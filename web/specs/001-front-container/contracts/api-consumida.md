# Contrato: API consumida pelo front

Resumo de **como o front usa** cada endpoint de `docs/contrato-api.md`. Formatos de corpo e
regras não são repetidos aqui: o contrato é a fonte da verdade.

URL base: valor de `ApiBaseUrl` em `wwwroot/appsettings.json` (padrão `http://localhost:5080`).

| Endpoint | Tela / ação | Resposta → comportamento do front |
|---|---|---|
| `POST /api/auth/register` | Cadastro → "Criar conta" | 204 → mensagem de sucesso e ir para `/login` · 400 → erros por campo (`email`, `password`) · 409 → alerta "Já existe uma conta com esse e-mail." |
| `POST /api/auth/login` | Login → "Entrar" | 200 → guarda `accessToken` e vai para "/" · 400 → erros por campo · 401 → alerta "E-mail ou senha inválidos." |
| `GET /api/consorcios` | Abrir "/" e após cada alteração | 200 → cartões + resumo · 401 → `/login?expirou=1` |
| `GET /api/consorcios/{id}` | não usado | Os dados para editar vêm da lista já carregada (evita uma chamada a mais). |
| `POST /api/consorcios` | Modal "Novo consórcio" → "Criar consórcio" | 201 → fecha modal e recarrega a lista · 400 → erros por campo (`administradora`, `grupo`, `cota`, `credito`) · 409 → alerta de duplicidade · 401 → Login |
| `PUT /api/consorcios/{id}` | Modal "Editar consórcio" → salvar | 204 → fecha modal e recarrega · 400 → erros por campo · 404 → alerta "Este consórcio não existe mais." e recarrega · 409 → alerta de duplicidade · 401 → Login |
| `DELETE /api/consorcios/{id}` | Modal "Excluir consórcio" → "Sim, excluir" | 204 → fecha e recarrega · 404 → alerta "Este consórcio não existe mais." e recarrega · 401 → Login |

Em qualquer chamada: falha de rede ou CORS → "Não foi possível conectar ao servidor. Tente
novamente."

Header em todas as chamadas protegidas: `Authorization: Bearer {accessToken}`.

**Dependência**: a API precisa permitir CORS para a origem `http://localhost:5200`
(ver research R10).
