# Research: Front publicado em container

Todas as decisões abaixo partem das escolhas técnicas do usuário e da constituição v1.2.0.
Nenhum item ficou como NEEDS CLARIFICATION.

## R1. Endereço da API em arquivo de configuração

- **Decision**: `wwwroot/appsettings.json` com `{ "ApiBaseUrl": "http://localhost:5080" }`.
  `Program.cs` lê `builder.Configuration["ApiBaseUrl"]` e registra um `HttpClient` com esse
  `BaseAddress`.
- **Rationale**: o Blazor WASM baixa e lê `appsettings.json` da pasta publicada sem código extra.
  Como é um arquivo estático servido pelo nginx, dá para trocar o endereço sem recompilar.
- **Alternatives considered**: constante no código (proibido pelo Princípio VI); variável de
  ambiente do container (o navegador não enxerga variáveis do container sem gerar arquivo na
  inicialização, o que é mais complexo).

## R2. Onde guardar o token

- **Decision**: `sessionStorage`, acessado por `IJSRuntime` chamando diretamente
  `sessionStorage.setItem/getItem/removeItem` (sem arquivo `.js` próprio). Serviço `Sessao`
  registrado como singleton guarda também uma cópia em memória.
- **Rationale**: atende ao FR-011a (sessão só enquanto a aba estiver aberta) e sobrevive a um
  recarregamento da página (F5) dentro da mesma aba.
- **Alternatives considered**: só em memória (perde a sessão no F5); `localStorage` (mantém após
  fechar o navegador, contrário à clarificação); `AuthenticationStateProvider` +
  `[Authorize]` (mais conceitos para um projeto didático com só uma página protegida).

## R3. Cabeçalho e navegação

- **Decision**: `MainLayout.razor` reproduz o `<header class="topbar">` de
  `docs/layout/index.html` com as classes reais: `topbar`, `container topbar__inner`,
  `brand`, `brand__mark`, `brand__name`, `btn btn--primary topbar__center`
  ("+ Novo consórcio") e `btn btn--danger-solid topbar__logout` ("Logout"). Sem `NavLink` e sem
  link "Dashboard".
- **Rationale**: as classes pedidas (`header`, `header__inner`, `logo`, `logo__mark`, `nav`,
  `nav__link`, `nav__link--active`) não existem em `docs/layout/styles.css` nem em
  `index.html`; usá-las deixaria o cabeçalho sem estilo e violaria o Princípio III (sem
  exceções). Há uma única página autenticada, então um menu com estado "ativo" não teria função.
- **Alternatives considered**: usar as classes pedidas e criar o CSS delas (viola o Princípio
  III); incluir `NavLink` para "/" com as classes `brand` (possível, mas sem ganho: a marca já é
  um link para "/").
- **Confirmado pelo usuário (2026-11-17)**: usar somente o cabeçalho da referência, sem menu e
  sem "Dashboard".

## R4. Layout das páginas de Login e Cadastro

- **Decision**: `AuthLayout.razor` envolve o conteúdo em `<div class="auth-simple">` e as
  páginas usam `main.auth-card`, `auth-card__brand`, `auth-card__title`, `auth-card__alert`,
  `auth__form`, `field`, `input`, `btn btn--primary btn--block`, `auth__switch`, como em
  `login.html` e `cadastro.html`.
- **Rationale**: na referência a classe `auth-simple` fica no `<body>`, que o Blazor não
  controla; as regras dela (centralização e altura mínima) funcionam igualmente num `div` que
  ocupa a página.
- **Alternatives considered**: alterar a classe do `<body>` via JS (código extra sem ganho).

## R5. Modais e mensagens de erro por campo

- **Decision**: os modais são renderizados condicionalmente pelo Blazor (variável de estado), com
  as mesmas classes da referência (`modal`, `modal__backdrop`, `modal__box`, `modal__head`,
  `modal__close`, `modal__foot`, `form`, `form__grid`, `field`, `alert alert--error
  form__alert`, `confirm`, `confirm__item`). No fim de `app.css` entra um bloco
  "Adições do front" com:
  - `.modal--open { display: flex; }` (equivale ao `.modal:target` da referência);
  - estilo da mensagem de erro por campo (`.validation-message`, em `var(--danger)`) e da borda
    de campo inválido (`.input.invalid`, `border-color: var(--danger)`).
- **Rationale**: a referência abre modais via `:target` (âncora na URL); no Blazor, controlar a
  abertura por estado é mais simples e permite fechar o modal após salvar. As classes
  `validation-message` e `invalid` são as que o próprio Blazor gera, então não há código para
  aplicá-las.
- **Alternatives considered**: manter `:target` com âncoras (exige um modal por consórcio e
  manipular a URL); estilos inline (espalha CSS fora do arquivo único).

## R6. Validação na tela (FR-018a)

- **Decision**: `EditForm` + `DataAnnotationsValidator` + `ValidationMessage` por campo, com
  atributos nos modelos (`[Required]`, `[EmailAddress]`, `[StringLength(30, MinimumLength = 3)]`,
  `[RegularExpression(@"^\d+$")]`, `[Range(0, ...)]`) e `ErrorMessage` em português exatamente
  como na spec. Erros 400 da API (ProblemDetails `errors`) são adicionados a um
  `ValidationMessageStore` para aparecerem no mesmo lugar. Administradora com só espaços é
  tratada com `Trim` antes de validar.
- **Rationale**: validação completa por campo, nativa do Blazor, sem biblioteca extra.
- **Alternatives considered**: FluentValidation (dependência extra, contraria o Princípio I);
  validação manual em cada página (repetitiva).

## R7. Formatação pt-BR e tipos

- **Decision**: em `Program.cs`, `CultureInfo.DefaultThreadCurrentCulture` e
  `DefaultThreadCurrentUICulture` = `pt-BR`. Moeda com `ToString("C")` ("R$ 1.234,56"), datas
  com `ToString("dd/MM/yyyy")`, nulos como "—". Modelo usa `decimal?` e `DateOnly?`; o
  `System.Text.Json` com padrões Web já serializa camelCase e `DateOnly` como `yyyy-MM-dd`.
- **Rationale**: atende ao FR-006 e ao formato do contrato sem conversores próprios.
- **Alternatives considered**: `InvariantGlobalization=true` (menor download, mas quebra a
  formatação pt-BR).

## R8. Tratamento de respostas da API

- **Decision**: `ApiClient` concentra as chamadas. Antes de cada chamada protegida, adiciona
  `Authorization: Bearer {token}`. Respostas:
  - 401 em rota protegida → `Sessao.Limpar()` e navega para `/login?expirou=1`
    (mensagem "Sua sessão expirou. Entre novamente.");
  - 400 → devolve o dicionário `errors` do ProblemDetails para a página;
  - 404 / 409 → devolve um resultado com a mensagem pt-BR da spec;
  - `HttpRequestException` (API fora do ar / CORS) → "Não foi possível conectar ao servidor.
    Tente novamente."
- **Rationale**: um único ponto de tratamento mantém as páginas curtas.
- **Alternatives considered**: `DelegatingHandler` para o token e o 401 (mais um conceito sem
  necessidade).

## R9. Container

- **Decision**:
  - `Dockerfile` multi-stage: estágio `build` em `mcr.microsoft.com/dotnet/sdk:10.0` copia o
    `.csproj`, faz `dotnet restore`, copia o resto e roda
    `dotnet publish ConsorciosMariussi.Web -c Release -o /app/publish`; estágio final
    `nginx:alpine` copia `nginx.conf` para `/etc/nginx/conf.d/default.conf` e
    `/app/publish/wwwroot` para `/usr/share/nginx/html`.
  - `nginx.conf`: `listen 80`, `root /usr/share/nginx/html`,
    `location / { try_files $uri $uri/ /index.html; }` e o tipo `application/wasm` para `.wasm`.
  - `.dockerignore`: `bin`, `obj`, `**/bin`, `**/obj`, `.specify`, `.claude`, `specs`, `docs`.
- **Atenção (nginx)**: um bloco `types { }` dentro de `server` **substitui** todos os tipos
  herdados, e o `.css`/`.js` passariam a ser servidos como `application/octet-stream`. Incluir
  `mime.types` dentro de outro `types` também não funciona (o arquivo já é um bloco `types`).
  Solução adotada na implementação: `location ~* \.wasm$ { types { } default_type
  application/wasm; }`, que afeta só os `.wasm` e preserva os tipos padrão no resto do site.
- **Consequência do `try_files`**: endereços inexistentes (ex.: `/qualquer-coisa`) recebem o
  `index.html`, e o Router do Blazor mostra a página "Página não encontrada." (atende ao caso de
  borda da spec pelo lado do usuário; o status HTTP será 200).
- **Rationale**: o compose já usa `build: ./web` e `5200:80`, então o contexto é esta pasta.

## R10. Dependência externa: CORS na API

- **Decision**: o front chama `http://localhost:5080` a partir de `http://localhost:5200`
  (origens diferentes), então **a API precisa liberar CORS para `http://localhost:5200`**,
  incluindo o header `Authorization` e os métodos GET, POST, PUT e DELETE.
- **Rationale**: sem isso, todas as chamadas falham no navegador com erro de conexão.
- **Alternatives considered**: proxy reverso no nginx do front (mudaria o endereço visto pelo
  navegador, contrário ao Princípio VI e ao contrato).
- **Status**: confirmado pelo usuário (2026-11-17) que a API já libera CORS para o front.
