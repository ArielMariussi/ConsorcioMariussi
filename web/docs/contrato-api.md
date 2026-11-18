# Contrato da API SiteConsorcio

Formato: JSON, propriedades em camelCase.
Datas no formato yyyy-MM-dd. Valores monetários como número com 2 casas decimais.
URL base, vista pelo navegador: http://localhost:5080

## Autenticação

Autenticação por token JWT Bearer, obtido em `POST /api/auth/login`.

- Endpoints marcados como **Protegido** exigem o header `Authorization: Bearer {accessToken}`.
- Sem token, com token inválido ou com token expirado: 401 Unauthorized com ProblemDetails.
- O token vale por 60 minutos. Não há refresh token: ao expirar, é preciso fazer login de novo.

### Usuario

| Campo | Tipo | Regra |
|---|---|---|
| email | string | Obrigatório, formato de e-mail válido, único (maiúsculas e minúsculas não diferenciam) |
| password | string | Obrigatório, mínimo 3 e máximo 30 caracteres; nenhum tipo de caractere é obrigatório (nem especial, nem dígito, nem maiúscula) |

A senha nunca é retornada em nenhuma resposta.

## Consorcio

| Campo | Tipo | Regra |
|---|---|---|
| id | int | Identificador |
| administradora | string | Obrigatório, sem espaços nas pontas |
| grupo | string | Obrigatório, só dígitos (mantém zeros à esquerda) |
| cota | string | Obrigatório, só dígitos (mantém zeros à esquerda) |
| credito | decimal \| null | Não pode ser negativo |
| valorPago | decimal \| null | Opcional |
| dataCompra | string (date) \| null | Opcional |
| vencimento | string (date) \| null | Opcional |
| assembleia | string (date) \| null | Opcional |
| comissao | decimal \| null | Opcional |
| previsaoLucro | decimal \| null | Opcional |
| fundoReserva | decimal \| null | Opcional |
| contemplacao | string (date) \| null | Opcional |
| taxaTransferencia | decimal \| null | Opcional |
| dataLucro | string (date) \| null | Opcional |
| lucro | decimal \| null | Opcional |

Cada consórcio pertence ao usuário autenticado que o criou: todos os endpoints de consórcio só listam, leem, alteram e excluem os consórcios desse usuário. Consórcio de outro usuário responde 404 Not Found.

A combinação administradora + grupo + cota é única dentro da conta de cada usuário (usuários diferentes podem cadastrar a mesma combinação).

## Endpoints

### POST /api/auth/register
Público. Cria um usuário.
Corpo:
```json
{
  "email": "joel@exemplo.com",
  "password": "abc"
}
```
Regras: as da tabela Usuario.
Respostas:
- 204 No Content
- 400 Bad Request com ProblemDetails de validação (chaves: email, password)
- 409 Conflict se já existir usuário com o mesmo email

### POST /api/auth/login
Público. Troca email e senha por um token.
Corpo:
```json
{
  "email": "joel@exemplo.com",
  "password": "abc"
}
```
Respostas:
- 200 OK
```json
{
  "accessToken": "eyJhbGciOi...",
  "expiresIn": 3600
}
```
`expiresIn` é a validade do token em segundos.
- 400 Bad Request com ProblemDetails de validação (chaves: email, password) se algum deles estiver vazio
- 401 Unauthorized com ProblemDetails se o email não existir ou a senha estiver errada (a mesma resposta nos dois casos)

### GET /api/consorcios
Protegido.
Retorna todos os consórcios, ordenados pela data da compra, dos mais recentes para os mais antigos.
Resposta 200:
```json
[
  {
    "id": 1,
    "administradora": "Sicredi",
    "grupo": "05014",
    "cota": "0361",
    "credito": 200000.00,
    "valorPago": 30000.00,
    "dataCompra": "2026-07-07",
    "vencimento": "2026-07-08",
    "assembleia": "2026-07-13",
    "comissao": null,
    "previsaoLucro": 14000.00,
    "fundoReserva": null,
    "contemplacao": null,
    "taxaTransferencia": null,
    "dataLucro": null,
    "lucro": 14000.00
  }
]
```

### GET /api/consorcios/{id}
Protegido.
Respostas:
- 200 OK com o Consorcio no corpo
- 404 Not Found se o consórcio não existir

### POST /api/consorcios
Protegido.
Corpo: todos os campos do Consorcio, exceto id.
```json
{
  "administradora": "Sicredi",
  "grupo": "05014",
  "cota": "0361",
  "credito": 200000.00,
  "valorPago": 30000.00,
  "dataCompra": "2026-07-07",
  "vencimento": "2026-07-08",
  "assembleia": "2026-07-13",
  "comissao": null,
  "previsaoLucro": 14000.00,
  "fundoReserva": null,
  "contemplacao": null,
  "taxaTransferencia": null,
  "dataLucro": null,
  "lucro": null
}
```
Regras: as da tabela acima. administradora é gravada sem espaços nas pontas.
Respostas:
- 201 Created com o Consorcio criado no corpo e o header Location /api/consorcios/{id}
- 400 Bad Request com ProblemDetails de validação (chaves: administradora, grupo, cota, credito)
- 409 Conflict se já existir consórcio com a mesma administradora + grupo + cota

### PUT /api/consorcios/{id}
Protegido.
Corpo: o mesmo do POST. Substitui todos os campos.
Regras: as mesmas do POST.
Respostas:
- 204 No Content
- 400 Bad Request com ProblemDetails de validação (chaves: administradora, grupo, cota, credito)
- 404 Not Found se o consórcio não existir
- 409 Conflict se outro consórcio já tiver a mesma administradora + grupo + cota

### DELETE /api/consorcios/{id}
Protegido.
Respostas:
- 204 No Content
- 404 Not Found se o consórcio não existir
