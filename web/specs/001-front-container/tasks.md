---

description: "Lista de tarefas da feature Front publicado em container"
---

# Tasks: Front publicado em container

**Input**: Design documents from `specs/001-front-container/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: sem testes automatizados (decisão do usuário no plano). Cada história é validada
manualmente pelos passos indicados de [quickstart.md](quickstart.md).

**Organization**: tarefas agrupadas por história de usuário (US1–US6 da spec).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: história de usuário da tarefa (US1…US6)

## Path Conventions

- Raiz do repositório = esta pasta (`web/`).
- Projeto Blazor: `ConsorciosMariussi.Web/`; todos os caminhos abaixo são completos a partir da
  raiz.
- Regras gerais para todas as tarefas: textos visíveis em pt-BR; usar somente classes que
  existem em `docs/layout/styles.css` (mais o bloco de adições da T011); nenhum pacote NuGet além
  do template; nenhum arquivo `*.razor.css`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: criar o projeto e limpar o template

- [X] T001 Criar o projeto Blazor WebAssembly standalone executando, na raiz desta pasta, `dotnet new blazorwasm -n ConsorciosMariussi.Web -o ConsorciosMariussi.Web -f net10.0` (gera `ConsorciosMariussi.Web/ConsorciosMariussi.Web.csproj`); confirmar que `dotnet build ConsorciosMariussi.Web` compila
- [X] T002 Remover do template tudo que não será usado: `ConsorciosMariussi.Web/wwwroot/lib/` (Bootstrap), `ConsorciosMariussi.Web/Pages/Counter.razor`, `ConsorciosMariussi.Web/Pages/Weather.razor`, `ConsorciosMariussi.Web/Layout/NavMenu.razor`, `ConsorciosMariussi.Web/Layout/NavMenu.razor.css`, `ConsorciosMariussi.Web/Layout/MainLayout.razor.css`, `ConsorciosMariussi.Web/wwwroot/sample-data/`, `ConsorciosMariussi.Web/wwwroot/css/app.css` original e ícones/imagens de exemplo não usados; remover referências a eles no código
- [X] T003 Copiar `docs/layout/styles.css` sem alterações para `ConsorciosMariussi.Web/wwwroot/css/app.css` (único arquivo de estilos do projeto)
- [X] T004 [P] Criar `.dockerignore` na raiz desta pasta ignorando `bin`, `obj`, `**/bin`, `**/obj`, `.specify`, `.claude`, `specs` e `docs`
- [X] T005 [P] Garantir que `.gitignore` na raiz desta pasta ignora `bin/` e `obj/` (acrescentar as linhas se faltarem, sem remover as existentes)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: base comum a todas as histórias (configuração, sessão, cliente da API, layouts)

**⚠️ CRITICAL**: nenhuma história começa antes desta fase terminar

- [X] T006 Reescrever `ConsorciosMariussi.Web/wwwroot/index.html`: `<html lang="pt-BR">`, `<title>ConsórcioMariussi</title>`, `preconnect` + link das fontes Google Inter (400/500/600/700) e JetBrains Mono (500) exatamente como em `docs/layout/index.html`, `<link rel="stylesheet" href="css/app.css">` como único CSS, sem Bootstrap; texto de carregamento "Carregando…" e área `#blazor-error-ui` com "Ocorreu um erro inesperado." e link "Recarregar"
- [X] T007 [P] Criar `ConsorciosMariussi.Web/wwwroot/appsettings.json` com `{ "ApiBaseUrl": "http://localhost:5080" }` (research R1)
- [X] T008 [P] Criar `ConsorciosMariussi.Web/Services/Sessao.cs`: classe que guarda o token em memória e em `sessionStorage` (chave `cm.token`) chamando `IJSRuntime.InvokeVoidAsync("sessionStorage.setItem", ...)`, `InvokeAsync<string?>("sessionStorage.getItem", ...)` e `InvokeVoidAsync("sessionStorage.removeItem", ...)` (sem arquivo .js); métodos `Task<string?> ObterTokenAsync()`, `Task SalvarAsync(string token)`, `Task LimparAsync()` e `Task<bool> EstaAutenticadaAsync()` (research R2; FR-011a: nunca usar `localStorage`)
- [X] T009 Criar `ConsorciosMariussi.Web/Services/ApiClient.cs` com a base comum (sem métodos de endpoint ainda): recebe `HttpClient`, `Sessao` e `NavigationManager`; tipo de retorno `ResultadoApi` (ou `ResultadoApi<T>`) com `Sucesso`, `Dados`, `Mensagem` (alerta em pt-BR) e `ErrosPorCampo` (`Dictionary<string, string[]>`); método privado que envia a requisição adicionando `Authorization: Bearer {token}` quando `protegido = true`, e trata: 401 em rota protegida → `Sessao.LimparAsync()` + `NavigateTo("/login?expirou=1")`; 400 → lê o ProblemDetails e preenche `ErrosPorCampo` a partir de `errors` (chaves camelCase); `HttpRequestException` → `Mensagem = "Não foi possível conectar ao servidor. Tente novamente."` (research R8, contracts/api-consumida.md)
- [X] T010 Atualizar `ConsorciosMariussi.Web/Program.cs`: definir `CultureInfo.DefaultThreadCurrentCulture` e `DefaultThreadCurrentUICulture` como `new CultureInfo("pt-BR")`; registrar `HttpClient` com `BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!)`; registrar `Sessao` como singleton e `ApiClient` como scoped; garantir que o `.csproj` **não** tem `InvariantGlobalization=true` (research R7)
- [X] T011 Acrescentar ao fim de `ConsorciosMariussi.Web/wwwroot/css/app.css` um bloco comentado `/* ===== Adições do front (Blazor) ===== */` com somente: `.modal--open { display: flex; }`, `.validation-message { color: var(--danger); font-size: 13px; }` e `.input.invalid { border-color: var(--danger); }` (research R5; Complexity Tracking do plano)
- [X] T012 [P] Atualizar `ConsorciosMariussi.Web/_Imports.razor` com os `@using` de `System.Net.Http.Json`, `Microsoft.AspNetCore.Components.Forms`, `Microsoft.AspNetCore.Components.Routing`, `Microsoft.JSInterop`, `ConsorciosMariussi.Web`, `ConsorciosMariussi.Web.Layout`, `ConsorciosMariussi.Web.Components`, `ConsorciosMariussi.Web.Models` e `ConsorciosMariussi.Web.Services`
- [X] T013 Atualizar `ConsorciosMariussi.Web/App.razor`: `Router` com `DefaultLayout="typeof(MainLayout)"`, `FocusOnNavigate` e bloco `NotFound` exibindo "Página não encontrada." dentro de `<main class="container">` com link "Voltar ao início" para `/` (contracts/ui-rotas.md)
- [X] T014 [P] Criar `ConsorciosMariussi.Web/Layout/AuthLayout.razor`: herda `LayoutComponentBase` e renderiza `<div class="auth-simple">@Body</div>` (research R4)
- [X] T015 Reescrever `ConsorciosMariussi.Web/Layout/MainLayout.razor` reproduzindo o topo e o rodapé de `docs/layout/index.html` com as mesmas classes: `<header class="topbar"><div class="container topbar__inner">` contendo `<a href="/" class="brand"><span class="brand__mark">CM</span><span class="brand__name">Consórcio<strong>Mariussi</strong></span></a>`, `<a href="/?novo=1" class="btn btn--primary topbar__center">+ Novo consórcio</a>` e `<button type="button" class="btn btn--danger-solid topbar__logout">Logout</button>` (ainda sem ação); depois `@Body`; depois `<footer class="footer"><div class="container">ConsórcioMariussi · 2026</div></footer>`. Sem `NavLink`, sem menu e sem link "Dashboard" (research R3)

**Checkpoint**: `dotnet build ConsorciosMariussi.Web` sem erros; base pronta

---

## Phase 3: User Story 1 - Subir o front com um único comando (Priority: P1) 🎯 MVP

**Goal**: `docker compose up` na pasta raiz do app publica o front em `http://localhost:5200`,
que leva à página de Login.

**Independent Test**: quickstart passos 1, 18 e 19 (página de Login abre; rota inexistente mostra
"Página não encontrada."; `.wasm` servido como `application/wasm` e `app.css` como `text/css`).

- [X] T016 [P] [US1] Criar `Dockerfile` na raiz desta pasta, multi-stage: estágio `build` com `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build`, `WORKDIR /src`, copiar `ConsorciosMariussi.Web/ConsorciosMariussi.Web.csproj` para `ConsorciosMariussi.Web/`, `dotnet restore`, copiar o resto (`COPY . .`) e `dotnet publish ConsorciosMariussi.Web/ConsorciosMariussi.Web.csproj -c Release -o /app/publish`; estágio final `FROM nginx:alpine` copiando `nginx.conf` para `/etc/nginx/conf.d/default.conf` e `--from=build /app/publish/wwwroot` para `/usr/share/nginx/html`; `EXPOSE 80` (research R9)
- [X] T017 [P] [US1] Criar `nginx.conf` na raiz desta pasta com um bloco `server { listen 80; root /usr/share/nginx/html; index index.html; location / { try_files $uri $uri/ /index.html; } }` e um `location ~* \.wasm$ { types { } default_type application/wasm; try_files $uri =404; }` para servir `.wasm` como `application/wasm` sem apagar os tipos padrão do resto do site (research R9)
- [X] T018 [P] [US1] Criar `ConsorciosMariussi.Web/Pages/Home.razor` com `@page "/"`: em `OnInitializedAsync`, se `Sessao.EstaAutenticadaAsync()` for falso, `NavigateTo("/login")`; caso contrário renderiza `<main class="container">` com `<section class="toolbar"><h1 class="page-title">Meus consórcios</h1></section>` (conteúdo completo vem na US3)
- [X] T019 [P] [US1] Criar `ConsorciosMariussi.Web/Pages/Login.razor` com `@page "/login"` e `@layout AuthLayout`, reproduzindo a marcação estática de `docs/layout/login.html`: `<main class="auth-card">`, marca com `brand auth-card__brand` (link para `/`), `<h1 class="auth-card__title">Entrar</h1>`, formulário `auth__form` com campos `field`/`input` "E-mail" e "Senha", botão `btn btn--primary btn--block` "Entrar" e `<p class="auth__switch">Ainda não tem conta? <a href="/cadastro" class="link">Criar conta</a></p>`; sem envio ainda; `<PageTitle>Entrar · ConsórcioMariussi</PageTitle>`
- [ ] T020 [US1] Validar US1: na pasta acima desta, `docker compose up --build`; abrir `http://localhost:5200` (deve ir para `/login` com visual igual a `docs/layout/login.html`), abrir `/qualquer-coisa` ("Página não encontrada.") e conferir no DevTools os tipos de `.wasm` e `app.css` (quickstart passos 1, 18, 19)

**Checkpoint**: MVP — front publicado em container e acessível em `http://localhost:5200`

---

## Phase 4: User Story 2 - Criar conta e entrar (Priority: P2)

**Goal**: Cadastro → Login → página principal; Logout e proteção de rota.

**Independent Test**: quickstart passos 2–7, 14, 15 e 16.

- [X] T021 [P] [US2] Criar `ConsorciosMariussi.Web/Models/Credenciais.cs` com: classe `CadastroForm` com `Email` (`[Required(ErrorMessage = "Informe o e-mail.")]`, `[EmailAddress(ErrorMessage = "Informe um e-mail válido.")]`) e `Password` (`[Required(ErrorMessage = "Informe a senha.")]`, `[StringLength(30, MinimumLength = 3, ErrorMessage = "A senha deve ter entre 3 e 30 caracteres.")]`); classe `LoginForm` com `Email` e `Password` apenas `[Required]` com as mesmas mensagens "Informe o e-mail." / "Informe a senha."; record `RespostaLogin(string AccessToken, int ExpiresIn)`. Propriedades serializadas como `email` e `password` (camelCase do contrato; **não** usar "senha" da referência)
- [X] T022 [US2] Acrescentar a `ConsorciosMariussi.Web/Services/ApiClient.cs` os métodos públicos `RegistrarAsync(CadastroForm)` → `POST /api/auth/register` (204 sucesso; 400 erros por campo `email`/`password`; 409 `Mensagem = "Já existe uma conta com esse e-mail."`) e `EntrarAsync(LoginForm)` → `POST /api/auth/login` (200 → `Sessao.SalvarAsync(accessToken)`; 400 erros por campo; 401 `Mensagem = "E-mail ou senha inválidos."`); ambos sem token e **sem** redirecionar no 401 (contracts/api-consumida.md)
- [X] T023 [P] [US2] Criar `ConsorciosMariussi.Web/Pages/Cadastro.razor` com `@page "/cadastro"` e `@layout AuthLayout`, seguindo `docs/layout/cadastro.html` (`auth-card`, `auth-card__brand`, `<h1 class="auth-card__title">Criar conta</h1>`, `auth__form`, botão "Criar conta", `auth__switch` "Já tem conta? Entrar" → `/login`): `EditForm` com `DataAnnotationsValidator`, `InputText` (`type="email"` e `type="password"`, `class="input"`) e `ValidationMessage` sob cada campo; se já autenticada, `NavigateTo("/")`; ao enviar, desabilitar o botão enquanto aguarda; alerta `alert alert--error auth-card__alert` só quando houver `Mensagem`; erros 400 da API adicionados a um `ValidationMessageStore`; em sucesso, `NavigateTo("/login?cadastro=1")`; não usar `minlength="8"` da referência (vale o contrato: 3 a 30)
- [X] T024 [US2] Tornar `ConsorciosMariussi.Web/Pages/Login.razor` funcional: trocar o formulário estático por `EditForm` com `LoginForm`, `DataAnnotationsValidator`, `InputText` e `ValidationMessage`; se já autenticada, `NavigateTo("/")`; ler `[SupplyParameterFromQuery]` `expirou` e `cadastro`: com `expirou=1` mostrar alerta `alert alert--error auth-card__alert` "Sua sessão expirou. Entre novamente."; com `cadastro=1` mostrar alerta `alert auth-card__alert` "Conta criada. Entre com seu e-mail e senha."; ao enviar, desabilitar o botão, chamar `EntrarAsync` e em sucesso `NavigateTo("/")`, senão mostrar `Mensagem` ("E-mail ou senha inválidos.") ou erros por campo
- [X] T025 [US2] Ligar o botão "Logout" em `ConsorciosMariussi.Web/Layout/MainLayout.razor`: `@onclick` chama `Sessao.LimparAsync()` e `NavigateTo("/login")` (FR-011)
- [ ] T026 [US2] Validar US2 pelo quickstart passos 2–7, 14, 15 e 16 (cadastro, login, F5 mantém sessão, fechar a aba exige novo login, Logout)

**Checkpoint**: autenticação completa; US1 e US2 funcionam sozinhas

---

## Phase 5: User Story 3 - Ver meus consórcios e o resumo (Priority: P3)

**Goal**: página principal com resumo, filtros e cartões vindos da API.

**Independent Test**: com consórcios cadastrados direto na API, entrar e conferir cartões,
situação, resumo, busca e filtro (quickstart passos 7 e 12; US3 da spec).

- [X] T027 [P] [US3] Criar `ConsorciosMariussi.Web/Models/Consorcio.cs` com as propriedades do contrato: `int Id`, `string Administradora`, `string Grupo`, `string Cota`, `decimal? Credito`, `decimal? ValorPago`, `DateOnly? DataCompra`, `DateOnly? Veto`, `DateOnly? Assembleia`, `decimal? Comissao`, `decimal? PrevisaoLucro`, `decimal? FundoReserva`, `DateOnly? Contemplacao`, `decimal? TaxaTransferencia`, `DateOnly? DataLucro`, `decimal? Lucro`; propriedade calculada `bool Contemplado => Contemplacao != null` marcada com `[JsonIgnore]`; método `Consorcio Copiar()` (cópia rasa para o formulário de edição)
- [X] T028 [P] [US3] Criar `ConsorciosMariussi.Web/Models/Formato.cs`, classe estática com `Moeda(decimal? v)` → `v?.ToString("C") ?? "—"` (resultado "R$ 1.234,56" com cultura pt-BR) e `Data(DateOnly? d)` → `d?.ToString("dd/MM/yyyy") ?? "—"` (FR-006)
- [X] T029 [US3] Acrescentar a `ConsorciosMariussi.Web/Services/ApiClient.cs` o método `ListarAsync()` → `GET /api/consorcios` protegido, retornando `ResultadoApi<List<Consorcio>>` na ordem recebida
- [X] T030 [P] [US3] Criar `ConsorciosMariussi.Web/Components/ConsorcioCard.razor` reproduzindo um `<details class="card">` de `docs/layout/index.html`: `summary.card__head` com `card__title` (badge "Contemplado" com `badge badge--success` quando `Contemplado`, senão "Em andamento" com `badge`; `<h2>` administradora; `<span class="muted">Grupo <b class="mono">…</b> · Cota <b class="mono">…</b></span>`), `dl.card__kpis` (Crédito, Valor pago, Próx. assembleia = `Assembleia`) e `span.chevron`; `div.card__body` com `div.card__actions` contendo `<button type="button" class="btn btn--danger">Excluir</button>` e `<button type="button" class="btn btn--ghost">Editar consórcio</button>` (callbacks `EventCallback<Consorcio> AoExcluir` e `AoEditar`, ligados nas US5/US6) e `div.grid-info` com os 10 `div.info` na ordem da referência (Data da compra, Veto, Assembleia, Contemplação, Comissão, Fundo de reserva, Taxa de transferência, Previsão de lucro, Data do lucro, Lucro com `info info--accent`), formatados com `Formato`; parâmetro `Aberto` para o primeiro cartão abrir expandido
- [X] T031 [US3] Completar `ConsorciosMariussi.Web/Pages/Home.razor`: carregar a lista com `ListarAsync` ao abrir; `section.stats` com 4 `article.stat` (Consórcios = quantidade; Total investido = soma de `ValorPago`; Previsão de lucro = soma de `PrevisaoLucro`; Total de lucro = soma de `Lucro`, com `stat stat--accent`; nulos contam como zero; somas sobre **todos** os itens, não só os filtrados); `section.toolbar` com o `h1.page-title` e `div.toolbar__filters` contendo `<input type="search" class="input" placeholder="Buscar por administradora, grupo, cota…">` e `<select class="input">` com "Todos", "Contemplados", "Em andamento" (`@bind:event="oninput"` na busca); `section.list` com um `ConsorcioCard` por item filtrado (busca sem diferenciar maiúsculas em administradora, grupo ou cota; situação pelo `Contemplado`); mensagens `<p class="empty">`: "Ainda não há consórcios cadastrados." quando a lista vier vazia e "Nenhum consórcio encontrado." quando os filtros esconderem tudo; em falha de conexão, `<p class="alert alert--error">` com a `Mensagem` e botão `btn btn--ghost` "Tentar novamente" (FR-012, FR-013, FR-014, FR-020)
- [ ] T032 [US3] Validar US3: cadastrar 2 consórcios direto na API (um com `contemplacao`), entrar e conferir cartões, etiquetas, "—" nos vazios, zeros à esquerda de grupo/cota, resumo, busca e filtro; parar a API e conferir a mensagem de conexão

**Checkpoint**: listagem completa e independente das histórias de escrita

---

## Phase 6: User Story 4 - Cadastrar um novo consórcio (Priority: P4)

**Goal**: modal "Novo consórcio" que valida na tela e cria pela API.

**Independent Test**: quickstart passos 8, 9 e 10.

- [X] T033 [US4] Acrescentar validação a `ConsorciosMariussi.Web/Models/Consorcio.cs`: `Administradora` com `[Required(ErrorMessage = "Informe a administradora.")]` (o formulário aplica `Trim()` antes de validar e enviar, para que só espaços conte como vazio); `Grupo` com `[Required(ErrorMessage = "Informe o grupo.")]` e `[RegularExpression(@"^\d+$", ErrorMessage = "O grupo deve conter apenas números.")]`; `Cota` com `[Required(ErrorMessage = "Informe a cota.")]` e `[RegularExpression(@"^\d+$", ErrorMessage = "A cota deve conter apenas números.")]`; `Credito` com `[Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "O crédito não pode ser negativo.")]`; `Grupo` e `Cota` permanecem `string` (mantêm zeros à esquerda)
- [X] T034 [US4] Acrescentar a `ConsorciosMariussi.Web/Services/ApiClient.cs` o método `CriarAsync(Consorcio)` → `POST /api/consorcios` protegido, enviando todos os campos exceto `id` (vazios como `null`): 201 sucesso; 400 erros por campo (`administradora`, `grupo`, `cota`, `credito`); 409 `Mensagem = "Já existe um consórcio dessa administradora com esse grupo e cota."`
- [X] T035 [US4] Criar `ConsorciosMariussi.Web/Components/ConsorcioForm.razor` reproduzindo o modal "Novo consórcio" de `docs/layout/index.html`: `<div class="modal modal--open" role="dialog">` com `button`/elemento `modal__backdrop` e `modal__close` "×" que fecham; `header.modal__head` com `<h2>` = `Titulo`; `EditForm` com `class="form"`, `DataAnnotationsValidator`; alerta `alert alert--error form__alert` quando houver `Mensagem`; três `fieldset` com `legend` "Dados principais" (Administradora `field field--full` com `<em>*</em>`, Grupo `*`, Cota `*` com `inputmode="numeric"`, Crédito (R$), Valor pago (R$), Data da compra), "Datas" (Veto, Assembleia, Contemplação, Data do lucro) e "Valores" (Comissão (R$), Fundo de reserva (R$), Taxa de transferência (R$), Previsão de lucro (R$), Lucro (R$)), usando `InputText`/`InputNumber<decimal?>` (`step="0.01"`)/`InputDate<DateOnly?>` com `class="input"` e `ValidationMessage` sob cada campo; `footer.modal__foot` com botão `btn btn--ghost` "Cancelar" e submit `btn btn--primary` com texto `TextoBotao`, desabilitado enquanto `Enviando`; parâmetros: `Consorcio Modelo`, `string Titulo`, `string TextoBotao`, `Func<Consorcio, Task<ResultadoApi>> AoSalvar`, `EventCallback AoFechar`; no envio: `Trim` da administradora, chama `AoSalvar`, em sucesso chama `AoFechar`, senão mostra `Mensagem` e joga `ErrosPorCampo` num `ValidationMessageStore` mantendo os dados digitados (FR-015, FR-018a, FR-019, FR-021)
- [X] T036 [US4] Ligar a criação em `ConsorciosMariussi.Web/Pages/Home.razor`: ler `[SupplyParameterFromQuery] int? Novo`; quando `Novo == 1`, renderizar `ConsorcioForm` com `Modelo = new Consorcio()`, `Titulo = "Novo consórcio"`, `TextoBotao = "Criar consórcio"`, `AoSalvar = ApiClient.CriarAsync`; ao fechar (cancelar ou sucesso) `NavigateTo("/")` e, em sucesso, recarregar a lista para atualizar cartões e resumo (FR-018)
- [ ] T037 [US4] Validar US4 pelo quickstart passos 8, 9 e 10 (validação na tela, criação, duplicidade, cancelar não salva, duplo clique gera um só envio)

**Checkpoint**: criação funcionando; lista e resumo atualizam sozinhos

---

## Phase 7: User Story 5 - Editar um consórcio (Priority: P5)

**Goal**: "Editar consórcio" abre o formulário preenchido e salva pela API.

**Independent Test**: quickstart passo 11 e US5 da spec.

- [X] T038 [US5] Acrescentar a `ConsorciosMariussi.Web/Services/ApiClient.cs` o método `AtualizarAsync(Consorcio)` → `PUT /api/consorcios/{id}` protegido com o mesmo corpo do POST: 204 sucesso; 400 erros por campo; 404 `Mensagem = "Este consórcio não existe mais."` e marcar `NaoEncontrado = true` no resultado; 409 mesma mensagem de duplicidade da criação
- [X] T039 [US5] Ligar a edição em `ConsorciosMariussi.Web/Pages/Home.razor`: estado `Consorcio? emEdicao`; `ConsorcioCard.AoEditar` define `emEdicao = item.Copiar()`; renderizar `ConsorcioForm` com `Titulo = "Editar consórcio"`, `TextoBotao = "Salvar alterações"`, `AoSalvar = ApiClient.AtualizarAsync`; ao fechar, `emEdicao = null`; em sucesso recarregar a lista; se o resultado vier com `NaoEncontrado`, fechar o modal, exibir alerta `alert alert--error` "Este consórcio não existe mais." no topo da página e recarregar a lista (US5-4)
- [ ] T040 [US5] Validar US5: editar um campo e conferir cartão e resumo; preencher Contemplação e ver a etiqueta "Contemplado"; tentar duplicar a combinação de outro consórcio; editar um consórcio excluído em outra aba

**Checkpoint**: edição funcionando sem afetar criação e listagem

---

## Phase 8: User Story 6 - Excluir um consórcio (Priority: P6)

**Goal**: excluir com confirmação.

**Independent Test**: quickstart passo 13 e US6 da spec.

- [X] T041 [US6] Acrescentar a `ConsorciosMariussi.Web/Services/ApiClient.cs` o método `ExcluirAsync(int id)` → `DELETE /api/consorcios/{id}` protegido: 204 sucesso; 404 `Mensagem = "Este consórcio não existe mais."` com `NaoEncontrado = true`
- [X] T042 [P] [US6] Criar `ConsorciosMariussi.Web/Components/ExcluirConfirm.razor` reproduzindo o modal de exclusão de `docs/layout/index.html`: `<div class="modal modal--open" role="alertdialog">`, `modal__box modal__box--sm`, `<h2>Excluir consórcio</h2>`, `modal__close`, `div.confirm` com "Tem certeza que deseja excluir a cota abaixo? Essa ação não pode ser desfeita." e `div.confirm__item` (`<strong>` administradora; Grupo/Cota em `b.mono`), `footer.modal__foot` com "Cancelar" (`btn btn--ghost`) e "Sim, excluir" (`btn btn--danger-solid`, desabilitado enquanto envia); parâmetros `Consorcio Item`, `EventCallback AoConfirmar`, `EventCallback AoCancelar`
- [X] T043 [US6] Ligar a exclusão em `ConsorciosMariussi.Web/Pages/Home.razor`: estado `Consorcio? emExclusao`; `ConsorcioCard.AoExcluir` define o item; `ExcluirConfirm.AoConfirmar` chama `ExcluirAsync`, fecha e recarrega a lista; em `NaoEncontrado`, mostra o alerta "Este consórcio não existe mais." e recarrega; `AoCancelar` só fecha (FR-017, FR-018)
- [ ] T044 [US6] Validar US6 pelo quickstart passo 13 e excluindo um consórcio já removido em outra aba

**Checkpoint**: todas as histórias funcionando de forma independente

---

## Phase 9: Polish & Cross-Cutting Concerns

- [X] T045 [P] Revisar todos os `.razor` e `wwwroot/index.html` em `ConsorciosMariussi.Web/` garantindo textos 100% em pt-BR e nenhuma classe CSS fora de `wwwroot/css/app.css` (Princípios III, IV e VIII)
- [X] T046 [P] Conferir que não restou nenhuma referência ao template em `ConsorciosMariussi.Web/` (buscar por `bootstrap`, `Counter`, `Weather`, `NavMenu`, `sample-data`, `.razor.css`) e que o endereço da API só aparece em `ConsorciosMariussi.Web/wwwroot/appsettings.json` (Princípio VI)
- [X] T047 `dotnet build ConsorciosMariussi.Web -c Release` sem erros nem avisos novos
- [ ] T048 Executar o roteiro completo de [quickstart.md](quickstart.md) (passos 1–19) com `docker compose up --build` na pasta acima desta e registrar qualquer divergência

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências
- **Foundational (Phase 2)**: depende do Setup; bloqueia todas as histórias
- **US1 (Phase 3)**: depende da Foundational
- **US2 (Phase 4)**: depende da Foundational; usa `Login.razor` e `Home.razor` criados na US1
- **US3 (Phase 5)**: depende da US2 (precisa de login para chamar a API protegida)
- **US4, US5, US6 (Phases 6–8)**: dependem da US3 (cartões e `Home.razor`); US5 reutiliza o
  `ConsorcioForm` da US4; US6 é independente da US4/US5
- **Polish (Phase 9)**: depois das histórias desejadas

### Ordem das histórias

```text
Setup → Foundational → US1 → US2 → US3 ─┬→ US4 → US5
                                        └→ US6
```

### Within Each User Story

- Modelos → métodos do `ApiClient` → componentes → ligação na página → validação manual
- `ApiClient.cs` e `Home.razor` são editados por várias histórias: essas tarefas são sempre
  sequenciais (sem [P])

### Parallel Opportunities

- Setup: T004 e T005 juntos (após T001–T003)
- Foundational: T007, T008, T012 e T014 juntos; depois T009, T010, T011, T013, T015
- US1: T016, T017, T018 e T019 juntos
- US2: T021 e T023 podem começar juntos (T023 só compila depois de T021 e T022)
- US3: T027, T028 e T030 juntos; depois T029 e T031
- US6: T042 em paralelo com T041

---

## Parallel Example: User Story 1

```text
Task: "Criar Dockerfile na raiz desta pasta (multi-stage sdk:10.0 → nginx:alpine)"
Task: "Criar nginx.conf na raiz desta pasta (try_files + application/wasm mantendo mime.types)"
Task: "Criar ConsorciosMariussi.Web/Pages/Home.razor (redireciona para /login sem sessão)"
Task: "Criar ConsorciosMariussi.Web/Pages/Login.razor (marcação estática de login.html)"
```

## Parallel Example: User Story 3

```text
Task: "Criar ConsorciosMariussi.Web/Models/Consorcio.cs"
Task: "Criar ConsorciosMariussi.Web/Models/Formato.cs"
Task: "Criar ConsorciosMariussi.Web/Components/ConsorcioCard.razor"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 (Setup) → Phase 2 (Foundational)
2. Phase 3 (US1)
3. **Parar e validar**: `docker compose up --build` e `http://localhost:5200` mostrando o Login

### Incremental Delivery

1. Setup + Foundational → base pronta
2. US1 → front no container (MVP)
3. US2 → cadastro e login de verdade
4. US3 → consulta dos consórcios
5. US4 → criação; US5 → edição; US6 → exclusão
6. Polish → roteiro completo do quickstart

---

## Notes

- [P] = arquivos diferentes, sem dependência de tarefa incompleta
- Sem testes automatizados; cada fase termina com uma tarefa de validação manual
- Commit sugerido ao fim de cada fase
- Onde `docs/layout/` e `docs/contrato-api.md` divergirem, vale o contrato (senha 3–30, campo
  `password`)
