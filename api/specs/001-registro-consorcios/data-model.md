# Data Model: Registro de Consórcios (fundação da API)

**Feature**: [spec.md](spec.md) | **Research**: [research.md](research.md)

Banco: PostgreSQL (serviço `db`), com o esquema criado pela migration `InitialCreate`. Os nomes de tabelas e colunas seguem a convenção padrão do EF Core/Npgsql (PascalCase, entre aspas no SQL).

## Consorcio → tabela `Consorcios`

| Propriedade C# | Tipo C# | Coluna / tipo PG | Nulo | Regra (spec/contrato) |
|---|---|---|---|---|
| Id | `int` | `Id integer` identity, PK | não | FR-002 |
| Administradora | `string` | `citext` | não | Obrigatória; setter aplica `Trim()`; FR-003 |
| Grupo | `string` | `varchar(20)` | não | Só dígitos, zeros à esquerda preservados; FR-004 |
| Cota | `string` | `varchar(20)` | não | Só dígitos, zeros à esquerda preservados; FR-004 |
| Credito | `decimal?` | `numeric(18,2)` | sim | `>= 0` quando informado; FR-005 |
| ValorPago | `decimal?` | `numeric(18,2)` | sim | FR-006 |
| DataCompra | `DateOnly?` | `date` | sim | FR-007 |
| Vencimento | `DateOnly?` | `date` | sim | FR-007 |
| Assembleia | `DateOnly?` | `date` | sim | FR-007 |
| Comissao | `decimal?` | `numeric(18,2)` | sim | FR-006 |
| PrevisaoLucro | `decimal?` | `numeric(18,2)` | sim | FR-006 |
| FundoReserva | `decimal?` | `numeric(18,2)` | sim | FR-006 |
| Contemplacao | `DateOnly?` | `date` | sim | FR-007 |
| TaxaTransferencia | `decimal?` | `numeric(18,2)` | sim | FR-006 |
| DataLucro | `DateOnly?` | `date` | sim | FR-007 |
| Lucro | `decimal?` | `numeric(18,2)` | sim | FR-006 |

> Diferença em relação ao pedido do plano: `Grupo` e `Cota` são `string` obrigatórias, não `int?`. Ver [research.md R1](research.md#r1-tipo-de-grupo-e-cota). `Id` foi adicionado porque o contrato o exige.

### Restrições no banco (FR-008, FR-009)

| Nome | Tipo | Definição |
|---|---|---|
| `PK_Consorcios` | PK | `Id` |
| `IX_Consorcios_Administradora_Grupo_Cota` | índice único | `(Administradora, Grupo, Cota)`; o `citext` faz a comparação sem diferenciar maiúsculas/minúsculas |
| `CK_Consorcios_Administradora` | check | `"Administradora" <> '' AND "Administradora" = btrim("Administradora")` |
| `CK_Consorcios_Grupo` | check | `"Grupo" ~ '^[0-9]+$'` |
| `CK_Consorcios_Cota` | check | `"Cota" ~ '^[0-9]+$'` |
| `CK_Consorcios_Credito` | check | `"Credito" IS NULL OR "Credito" >= 0` |

Extensão exigida: `citext` (criada pela migration).

### Mapeamento para o contrato (JSON)

As propriedades saem em camelCase pelo padrão do System.Text.Json (`id`, `administradora`, `grupo`, `cota`, `credito`, `valorPago`, `dataCompra`, ...). `DateOnly` sai como `"yyyy-MM-dd"`. A entidade `Consorcio` é usada diretamente como corpo das respostas (os campos batem 1:1 com o contrato).

## ConsorcioRequest (entrada de POST e PUT)

Record em `Contracts/ConsorcioRequest.cs`: todos os campos do contrato **exceto `id`**, todos anuláveis, para que um campo ausente gere erro de validação 400 em vez de falha de desserialização.

| Propriedade | Tipo | Validação (chave do erro 400) |
|---|---|---|
| Administradora | `string?` | `administradora`: obrigatória; não pode ficar vazia após `Trim()` |
| Grupo | `string?` | `grupo`: obrigatório; só `'0'..'9'`; até 20 caracteres |
| Cota | `string?` | `cota`: obrigatória; só `'0'..'9'`; até 20 caracteres |
| Credito | `decimal?` | `credito`: não pode ser negativo |
| ValorPago, Comissao, PrevisaoLucro, FundoReserva, TaxaTransferencia, Lucro | `decimal?` | sem regra |
| DataCompra, Vencimento, Assembleia, Contemplacao, DataLucro | `DateOnly?` | sem regra (formato inválido → 400 da desserialização) |

Mapeamento manual para a entidade (`ApplyRequest`): copia todos os campos; `Administradora` é aparada pelo setter; decimais são arredondados para 2 casas com escala fixa (research R18). Em PUT, o mesmo método sobrescreve **todos** os campos da entidade existente.

## Usuário → tabelas do ASP.NET Core Identity

Entidade `IdentityUser` padrão (sem classe derivada), com as tabelas `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` e `AspNetRoleClaims`, criadas pela mesma migration.

| Campo relevante | Regra |
|---|---|
| Email / UserName | Obrigatório, formato válido; `UserName` = e-mail |
| NormalizedEmail | Único (via `RequireUniqueEmail`); comparação sem diferenciar maiúsculas/minúsculas (FR-020) |
| PasswordHash | Hash do Identity (PBKDF2); a senha nunca é guardada em texto puro (FR-022) |

Regras de senha (FR-021), aplicadas pelo `UserManager` ao criar usuário ou trocar a senha:

| Regra | Valor |
|---|---|
| Tamanho mínimo | 3 (`Password.RequiredLength`) |
| Tamanho máximo | 30 (`MaxLengthPasswordValidator`) |
| Exige dígito / minúscula / maiúscula / não alfanumérico | não |
| Caracteres únicos exigidos | 1 |

Papéis: nenhum nesta feature.

## AuthRequest (entrada de register e login)

Record em `Contracts/AuthRequest.cs`: `AuthRequest(string? Email, string? Password)`. Os campos são anuláveis para que um campo ausente gere 400 de validação.

| Propriedade | Validação (chave do erro 400) |
|---|---|
| Email | `email`: obrigatório; no register, também o formato (via Identity) |
| Password | `password`: obrigatória; no register, também 3 a 30 caracteres (via Identity + `MaxLengthPasswordValidator`) |

## LoginResponse (saída do login)

Record em `Contracts/LoginResponse.cs`: `LoginResponse(string AccessToken, int ExpiresIn)`, serializado como `{ "accessToken": "...", "expiresIn": 3600 }`.

## Token JWT (não persistido)

| Item | Valor |
|---|---|
| Algoritmo | HMAC-SHA256 com `Jwt:Key` |
| `iss` / `aud` | `Jwt:Issuer` / `Jwt:Audience` |
| Claims | `sub` = `IdentityUser.Id`, `email` = `IdentityUser.Email` |
| Validade | 60 minutos (`expiresIn` = 3600), sem tolerância de relógio |

## Relacionamentos

Nenhum entre `Consorcio` e `Usuário` nesta feature. Os consórcios não pertencem a um usuário até que o contrato defina isso.
