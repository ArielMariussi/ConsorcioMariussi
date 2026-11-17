# Contrato HTTP de base (comportamento transversal)

A fonte da verdade da API é `docs/contrato-api.md`. Os endpoints estão em [auth-endpoints.md](auth-endpoints.md) e [consorcios-endpoints.md](consorcios-endpoints.md). Este arquivo descreve o comportamento HTTP transversal: rotas inexistentes, erros inesperados e CORS.

URL base vista pelo navegador: `http://localhost:5080` (porta do host → `8080` no container).

## 1. Rotas inexistentes → 404 ProblemDetails (FR-017)

Qualquer rota fora do contrato, por exemplo `GET /` ou `GET /api/consorcios/abc`:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404
}
```

## 2. Erro inesperado → 500 ProblemDetails (FR-017)

`Content-Type: application/problem+json`, `status: 500`. Fora de Development, sem stack trace nem detalhes da exceção.

## 3. CORS (FR-014, FR-015, FR-016)

Preflight da origem permitida:

```http
OPTIONS /api/consorcios
Origin: http://localhost:5200
Access-Control-Request-Method: POST
Access-Control-Request-Headers: content-type
```

Resposta esperada: `204` com `Access-Control-Allow-Origin: http://localhost:5200`, `Access-Control-Allow-Methods` contendo `POST` e `Access-Control-Allow-Headers` contendo `content-type`. O preflight não exige token; um preflight pedindo `authorization` também é autorizado.

Respostas reais para essa origem incluem `Access-Control-Expose-Headers: Location`.

Qualquer outra origem (ex.: `http://localhost:3000`, `http://127.0.0.1:5200`): a resposta **não** contém `Access-Control-Allow-Origin`.

## 4. Formato JSON

Para as features seguintes: camelCase, datas `yyyy-MM-dd`, enums como texto.
