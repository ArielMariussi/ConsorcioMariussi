# Endpoints de consórcio

**Fonte da verdade**: `docs/contrato-api.md`. Este arquivo não repete os exemplos de JSON de lá; ele só registra as decisões de implementação que o contrato deixa em aberto. Se houver divergência, vale o contrato.

Todos os endpoints são **protegidos** (`RequireAuthorization`, FR-032): exigem `Authorization: Bearer {accessToken}`, obtido em [auth-endpoints.md](auth-endpoints.md). Sem token válido → 401 ProblemDetails, antes de qualquer outra verificação. Todo erro sai como `application/problem+json`.

| Método e rota | Sucesso | Erros | Detalhes |
|---|---|---|---|
| `GET /api/consorcios` | 200 + array | 401 | Ordem: `dataCompra` desc, sem data por último, desempate por `id` desc |
| `GET /api/consorcios/{id:int}` | 200 + objeto | 401, 404 | Id não numérico → 404 (rota não casa) |
| `POST /api/consorcios` | 201 + objeto + `Location: /api/consorcios/{id}` | 401, 400, 409 | Corpo = `ConsorcioRequest` (sem `id`) |
| `PUT /api/consorcios/{id:int}` | 204 | 401, 400, 404, 409 | Ordem das verificações: 401 → 400 → 404 → 409. Substitui todos os campos |
| `DELETE /api/consorcios/{id:int}` | 204 | 401, 404 | — |

## 400: ValidationProblemDetails

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "administradora": ["A administradora é obrigatória."],
    "grupo": ["O grupo deve conter somente dígitos."],
    "cota": ["A cota é obrigatória."],
    "credito": ["O crédito não pode ser negativo."]
  }
}
```

Só aparecem as chaves com erro. As únicas chaves possíveis são `administradora`, `grupo`, `cota` e `credito`. JSON malformado ou data fora de `yyyy-MM-dd` também resultam em 400 ProblemDetails, mas sem essas chaves.

## 409: ProblemDetails

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Já existe um consórcio com a mesma administradora, grupo e cota."
}
```

## 404: ProblemDetails

Igual ao de [http-baseline.md](http-baseline.md).

## Formato dos valores

- `grupo`/`cota`: string, zeros à esquerda preservados (`"05014"`).
- Monetários: número com 2 casas (`200000.00`), inclusive na resposta do POST.
- Datas: `"yyyy-MM-dd"`; ausentes → `null`.
