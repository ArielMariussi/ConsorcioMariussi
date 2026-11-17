# Implementation Plan: Front publicado em container

**Branch**: `001-front-container` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-front-container/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Front do ConsórcioMariussi com três páginas (Cadastro, Login e principal) que consomem a API
descrita em `docs/contrato-api.md`: autenticação por token, listagem com resumo e filtros, e
criação, edição e exclusão de consórcios. Implementado como Blazor WebAssembly standalone
(.NET 10), publicado como arquivos estáticos e servido por nginx em um container construído pelo
`Dockerfile` desta pasta e executado pelo serviço `web` do `../docker-compose.yml`
(porta 5200 → 80). O visual reutiliza `docs/layout/styles.css`, copiado como único CSS.

## Technical Context

**Language/Version**: C# / .NET 10 (SDK 10.0.401 instalado), Blazor WebAssembly standalone

**Primary Dependencies**: apenas o que vem no template `blazorwasm` (sem Bootstrap, sem
bibliotecas extras); nginx:alpine para servir os estáticos

**Storage**: nenhum no front; token de acesso em `sessionStorage` do navegador (vale só
enquanto a aba estiver aberta — FR-011a)

**Testing**: sem testes automatizados (decisão do usuário); validação manual pelo
[quickstart.md](quickstart.md)

**Target Platform**: navegadores modernos com suporte a WebAssembly; container Linux (nginx)

**Project Type**: aplicação web de front-end (SPA estática) que consome API REST externa

**Performance Goals**: página abre em < 2 s após o ambiente subir; lista atualizada em < 2 s
após criar/editar/excluir (SC-005)

**Constraints**: CSS em arquivo único, sem frameworks CSS; endereço da API em arquivo de
configuração; não alterar `../docker-compose.yml`; não ler arquivos fora desta pasta (exceto o
compose, para consulta); textos em pt-BR

**Scale/Scope**: 3 páginas, 1 entidade (Consórcio), 7 endpoints do contrato, uso por poucas
pessoas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Princípio | Situação | Observação |
|---|---|---|---|
| I | Simplicidade | ✅ | Blazor WASM é a tecnologia definida na constituição (v1.2.0). Dentro dele: sem camadas extras, um serviço de API e um de sessão, validação nativa do Blazor (DataAnnotations), nenhum pacote além do template. |
| II | Contrato primeiro | ✅ | Modelos e chamadas espelham `docs/contrato-api.md` ([contracts/api-consumida.md](contracts/api-consumida.md)). Onde `cadastro.html` diverge (senha mín. 8, campo "senha"), vale o contrato. |
| III | Fidelidade ao layout | ✅ | Cabeçalho com as classes reais de `index.html` (`topbar`, `topbar__inner`, `brand`, `brand__mark`, `brand__name`), sem menu de navegação nem link "Dashboard" (confirmado pelo usuário). Ver research R3. |
| IV | CSS puro, arquivo único | ✅ | `docs/layout/styles.css` → `wwwroot/css/app.css`, único CSS. Bootstrap e `*.razor.css` do template removidos. Pequeno bloco de adições no fim do mesmo arquivo (research R5). |
| V | Roda em container | ✅ | `Dockerfile` multi-stage + `nginx.conf` na raiz desta pasta; compose intocado. |
| VI | API configurável | ✅ | `wwwroot/appsettings.json` com `ApiBaseUrl: http://localhost:5080`, lido pelo Blazor na inicialização. |
| VII | Autenticação | ✅ | Cadastro → Login → página principal; 401 leva ao Login. |
| VIII | pt-BR | ✅ | Textos e formatação de moeda/data em pt-BR (cultura `pt-BR`). |
| — | Restrições Técnicas (Blazor WASM .NET 10, CSS único) | ✅ | Constituição emendada para v1.2.0. |

**Resultado do gate**: aprovado. Único item em Complexity Tracking: o bloco de adições no
`app.css`.

**Re-check pós-design (Phase 1)**: mantido. O design não introduziu novas dependências nem
camadas.

## Project Structure

### Documentation (this feature)

```text
specs/001-front-container/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── api-consumida.md # Endpoints do contrato que o front usa e como trata cada resposta
│   └── ui-rotas.md      # Rotas, configuração e container expostos pelo front
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root = esta pasta `web/`)

```text
web/
├── Dockerfile                     # build (sdk:10.0) → final (nginx:alpine)
├── nginx.conf                     # → /etc/nginx/conf.d/default.conf
├── .dockerignore                  # bin, obj, .specify, .claude, specs, docs
└── ConsorciosMariussi.Web/        # dotnet new blazorwasm
    ├── ConsorciosMariussi.Web.csproj
    ├── Program.cs                 # HttpClient com ApiBaseUrl, cultura pt-BR, serviços
    ├── App.razor                  # Router; NotFound em pt-BR
    ├── _Imports.razor
    ├── Layout/
    │   ├── MainLayout.razor       # topbar de index.html (marca, + Novo consórcio, Logout) + footer
    │   └── AuthLayout.razor       # <div class="auth-simple"> para Login/Cadastro
    ├── Pages/
    │   ├── Home.razor             # "/"  resumo, filtros, cartões, modais
    │   ├── Login.razor            # "/login"
    │   └── Cadastro.razor         # "/cadastro"
    ├── Components/
    │   ├── ConsorcioCard.razor    # um <details class="card">
    │   ├── ConsorcioForm.razor    # modal de criar/editar (mesmo formulário)
    │   └── ExcluirConfirm.razor   # modal de confirmação
    ├── Models/
    │   ├── Consorcio.cs           # espelha o contrato + DataAnnotations com mensagens pt-BR
    │   └── Credenciais.cs         # email + password (+ resposta do login)
    ├── Services/
    │   ├── Sessao.cs              # token em sessionStorage (via IJSRuntime, sem arquivo JS)
    │   └── ApiClient.cs           # chamadas do contrato, Bearer, 401 → Login, ProblemDetails
    └── wwwroot/
        ├── index.html             # sem Bootstrap; fontes Inter/JetBrains Mono; css/app.css
        ├── appsettings.json       # { "ApiBaseUrl": "http://localhost:5080" }
        └── css/app.css            # cópia de docs/layout/styles.css (+ bloco de adições)
```

Removidos do template: `wwwroot/lib/bootstrap`, `Pages/Counter.razor`, `Pages/Weather.razor`,
`Layout/NavMenu.razor(.css)`, `Layout/MainLayout.razor.css`, `wwwroot/sample-data`,
`wwwroot/css/app.css` original e o ícone/estilos de exemplo.

**Structure Decision**: projeto único Blazor WASM em `ConsorciosMariussi.Web/`, com os
arquivos de container na raiz desta pasta (contexto de build do serviço `web`). Pastas internas
mínimas: `Layout`, `Pages`, `Components`, `Models`, `Services`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Bloco de adições no fim de `app.css` (classes para modal aberto e mensagens de erro por campo) | A referência abre modais via `:target` (âncora na URL) e não tem estilo para mensagem de erro por campo, exigida pelo FR-018a | Abrir modais por `:target` no Blazor obriga a manipular a âncora da URL a cada abertura/fechamento e a gerar um modal por consórcio; uma classe a mais é mais simples |
