# Contrato: o que o front expõe

## Rotas

| Rota | Página | Referência | Acesso |
|---|---|---|---|
| `/` | Principal (resumo, filtros, cartões, modais) | `docs/layout/index.html` | Autenticada; sem sessão → `/login` |
| `/login` | Entrar | `docs/layout/login.html` | Pública; com sessão → `/` |
| `/login?expirou=1` | Entrar, com alerta "Sua sessão expirou. Entre novamente." | idem | Pública |
| `/cadastro` | Criar conta | `docs/layout/cadastro.html` | Pública; com sessão → `/` |
| qualquer outra | "Página não encontrada." | — | — |

Os links das referências (`index.html`, `login.html`, `cadastro.html`) viram `/`, `/login` e
`/cadastro`.

## Configuração

| Arquivo | Chave | Padrão | Uso |
|---|---|---|---|
| `wwwroot/appsettings.json` (publicado em `/usr/share/nginx/html/appsettings.json`) | `ApiBaseUrl` | `http://localhost:5080` | Endereço da API visto pelo navegador |

## Container

| Item | Valor |
|---|---|
| Contexto de build | esta pasta (`web/`), via `build: ./web` do compose |
| Dockerfile | `web/Dockerfile` (multi-stage: `dotnet/sdk:10.0` → `nginx:alpine`) |
| Porta interna | 80 (`nginx.conf`) |
| Porta no computador | 5200 (definida no `../docker-compose.yml`, não alterado) |
| Comando | `docker compose up` na pasta raiz do app (a pasta acima de `web/`) |
