# Endpoints de autenticação

**Fonte da verdade**: `docs/contrato-api.md` (seções Autenticação, Usuario, `POST /api/auth/register` e `POST /api/auth/login`). Este arquivo só registra as decisões de implementação que o contrato deixa em aberto.

Ambos são públicos (`AllowAnonymous`). Todo erro sai como `application/problem+json`.

| Método e rota | Sucesso | Erros | Detalhes |
|---|---|---|---|
| `POST /api/auth/register` | 204 sem corpo | 400 (`email`, `password`), 409 | Não faz login automático |
| `POST /api/auth/login` | 200 `{ accessToken, expiresIn: 3600 }` | 400 (`email`, `password`), 401 | 401 idêntico para e-mail inexistente e senha errada |

## 400 do register (exemplo)

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "email": ["O e-mail informado é inválido."],
    "password": ["A senha deve ter no mínimo 3 caracteres."]
  }
}
```

Mapeamento dos erros do Identity: códigos `Password*` → `password`; `InvalidEmail`/`InvalidUserName` → `email`; `DuplicateEmail`/`DuplicateUserName` → 409.

## 409 do register

```json
{ "title": "Conflict", "status": 409, "detail": "Já existe um usuário com este e-mail." }
```

## 401 do login

```json
{ "title": "Unauthorized", "status": 401, "detail": "E-mail ou senha inválidos." }
```

## Uso do token

`Authorization: Bearer {accessToken}` em todos os endpoints de [consorcios-endpoints.md](consorcios-endpoints.md). Sem token, ou com token inválido ou expirado: 401 ProblemDetails (gerado pelo `UseStatusCodePages` a partir do desafio do JwtBearer).
