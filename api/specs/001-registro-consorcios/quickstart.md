# Quickstart: validação manual da feature 001

Não há testes automatizados (decisão do plano). Este roteiro prova cada história da [spec](spec.md). Os detalhes de esquema estão em [data-model.md](data-model.md) e o comportamento HTTP em [contracts/http-baseline.md](contracts/http-baseline.md), [contracts/auth-endpoints.md](contracts/auth-endpoints.md) e [contracts/consorcios-endpoints.md](contracts/consorcios-endpoints.md).

## Pré-requisitos

- Docker Desktop com Compose.
- Portas livres no host: 5080 (API), 5433 (banco), 5200 (web).
- Para gerar a migration: .NET SDK 10 e `dotnet-ef` (`dotnet tool install --global dotnet-ef`).

## US1: subir tudo com um comando

Na pasta raiz do app (`ConsorcioMariussi/`, acima de `api/`):

```sh
docker compose down -v      # máquina "limpa": apaga o volume do banco
docker compose up --build -d db api
docker compose logs api     # deve mostrar as migrations aplicadas e "Now listening on: http://[::]:8080"
curl -i http://localhost:5080/
```

Esperado: `404` com `Content-Type: application/problem+json` (`/` não está no contrato).

Estrutura criada:

```sh
docker compose exec db psql -U postgres -d consorcioJoel -c '\d "Consorcios"'
docker compose exec db psql -U postgres -d consorcioJoel -c '\dt'
```

Esperado: a tabela `Consorcios` com todos os campos e restrições do data-model, as tabelas `AspNet*` e `__EFMigrationsHistory`.

Persistência e idempotência:

```sh
docker compose restart api && docker compose logs --tail 20 api
```

Esperado: sobe sem erro e os registros inseridos na US2 continuam lá.

## US2: regras do registro no banco

```sh
docker compose exec db psql -U postgres -d consorcioJoel
```

```sql
-- válido; o setter da aplicação apara espaços, mas aqui o insert é direto no banco
INSERT INTO "Consorcios" ("Administradora","Grupo","Cota","Credito","DataCompra")
VALUES ('Sicredi','05014','0361',200000.005,'2026-07-07');
SELECT "Grupo","Cota","Credito","DataCompra" FROM "Consorcios";  -- 05014 | 0361 | 200000.01 | 2026-07-07

-- cada um abaixo DEVE falhar
INSERT INTO "Consorcios" ("Administradora","Grupo","Cota") VALUES ('SICREDI','05014','0361');   -- único (sem diferenciar maiúsculas)
INSERT INTO "Consorcios" ("Administradora","Grupo","Cota") VALUES ('  Sicredi ','1','1');       -- espaços nas pontas
INSERT INTO "Consorcios" ("Administradora","Grupo","Cota") VALUES ('','1','1');                -- vazia
INSERT INTO "Consorcios" ("Administradora","Grupo","Cota") VALUES ('X','05A14','1');           -- não-dígito
INSERT INTO "Consorcios" ("Administradora","Grupo","Cota") VALUES ('X','1','03 61');           -- não-dígito
INSERT INTO "Consorcios" ("Administradora","Grupo","Cota") VALUES ('X',NULL,'1');              -- obrigatório
INSERT INTO "Consorcios" ("Administradora","Grupo","Cota","Credito") VALUES ('X','1','1',-1);  -- crédito negativo
```

O `Trim()` da aplicação (administradora "  Sicredi  " → "Sicredi") é validado pelo POST na seção US5.

Antes de seguir para a US5, limpe a tabela: `DELETE FROM "Consorcios";`

## US3: CORS

```sh
curl -i -X OPTIONS http://localhost:5080/api/consorcios \
  -H "Origin: http://localhost:5200" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type"
```

Esperado: `Access-Control-Allow-Origin: http://localhost:5200`.

```sh
curl -i -X OPTIONS http://localhost:5080/api/consorcios \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: POST"
```

Esperado: sem `Access-Control-Allow-Origin`.

```sh
curl -i http://localhost:5080/ -H "Origin: http://localhost:5200"
```

Esperado: `Access-Control-Expose-Headers: Location`.

## US4: cadastro e login

Referência: [contracts/auth-endpoints.md](contracts/auth-endpoints.md).

```sh
A=http://localhost:5080/api/auth
J='Content-Type: application/json'

# 1. Cadastro com senha de 3 caracteres → 204
curl -i -X POST $A/register -H "$J" -d '{"email":"joel@exemplo.com","password":"abc"}'

# 2. Senha de 30 caracteres → 204; de 2 e de 31 → 400 com errors.password
curl -i -X POST $A/register -H "$J" -d '{"email":"trinta@exemplo.com","password":"123456789012345678901234567890"}'
curl -i -X POST $A/register -H "$J" -d '{"email":"curta@exemplo.com","password":"ab"}'
curl -i -X POST $A/register -H "$J" -d '{"email":"longa@exemplo.com","password":"1234567890123456789012345678901"}'

# 3. E-mail duplicado em outra caixa → 409; e-mail inválido ou ausente → 400 com errors.email
curl -i -X POST $A/register -H "$J" -d '{"email":"JOEL@exemplo.com","password":"abc"}'
curl -i -X POST $A/register -H "$J" -d '{"email":"joel","password":"abc"}'
curl -i -X POST $A/register -H "$J" -d '{"password":"abc"}'

# 4. Login (outra caixa no e-mail) → 200 com accessToken e expiresIn 3600
curl -i -X POST $A/login -H "$J" -d '{"email":"JOEL@exemplo.com","password":"abc"}'

# 5. Senha errada e e-mail inexistente → 401 com corpos idênticos; vazio → 400
curl -i -X POST $A/login -H "$J" -d '{"email":"joel@exemplo.com","password":"errada"}'
curl -i -X POST $A/login -H "$J" -d '{"email":"ninguem@exemplo.com","password":"abc"}'
curl -i -X POST $A/login -H "$J" -d '{"email":"","password":""}'
```

Banco: `\d "AspNetUsers"` mostra `NormalizedEmail` e `PasswordHash`, e `SELECT "Email","PasswordHash" FROM "AspNetUsers";` mostra só o hash, nunca "abc".

## US5: endpoints de consórcio

Referência: [contracts/consorcios-endpoints.md](contracts/consorcios-endpoints.md) e `docs/contrato-api.md`. Use `-i` para ver status e cabeçalhos.

Todos os comandos abaixo, exceto o passo 0, usam o token do usuário criado na US4.

```sh
B=http://localhost:5080/api/consorcios
J='Content-Type: application/json'

# 0. Sem token ou com token inválido → 401 ProblemDetails
curl -i $B
curl -i $B -H "Authorization: Bearer token-invalido"

# Obter o token e preparar o header (requer jq; sem jq, copie o accessToken à mão)
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login -H "$J" -d '{"email":"joel@exemplo.com","password":"abc"}' | jq -r .accessToken)
alias curl='curl -H "Authorization: Bearer $TOKEN"'

# 1. POST válido → 201, Location /api/consorcios/{id}, administradora "Sicredi", credito 200000.00
curl -i -X POST $B -H "$J" -d '{"administradora":"  Sicredi  ","grupo":"05014","cota":"0361","credito":200000,"valorPago":30000,"dataCompra":"2026-07-07"}'

# 2. POST inválido → 400 com errors.administradora, errors.grupo, errors.cota, errors.credito
curl -i -X POST $B -H "$J" -d '{"administradora":" ","grupo":"05A14","credito":-1}'

# 3. POST duplicado com outra caixa → 409
curl -i -X POST $B -H "$J" -d '{"administradora":"SICREDI","grupo":"05014","cota":"0361"}'

# 4. Mais dois para testar a ordenação
curl -s -X POST $B -H "$J" -d '{"administradora":"Porto","grupo":"1","cota":"1","dataCompra":"2026-01-10"}'
curl -s -X POST $B -H "$J" -d '{"administradora":"Porto","grupo":"1","cota":"2"}'

# 5. GET lista → 200, ordem: 2026-07-07, 2026-01-10, sem data
curl -i $B

# 6. GET por id (troque 1 pelo id do passo 1) → 200; inexistente → 404 ProblemDetails; não numérico → 404
curl -i $B/1
curl -i $B/999
curl -i $B/abc

# 7. PUT sem valorPago → 204; depois o GET mostra "valorPago": null
curl -i -X PUT $B/1 -H "$J" -d '{"administradora":"Sicredi","grupo":"05014","cota":"0361","credito":250000}'
curl -s $B/1

# 8. PUT para a combinação de outro → 409; id inexistente → 404; corpo inválido → 400
curl -i -X PUT $B/1 -H "$J" -d '{"administradora":"porto","grupo":"1","cota":"1"}'
curl -i -X PUT $B/999 -H "$J" -d '{"administradora":"X","grupo":"9","cota":"9"}'
curl -i -X PUT $B/1 -H "$J" -d '{"grupo":"x"}'

# 9. DELETE → 204; repetido → 404
curl -i -X DELETE $B/1
curl -i -X DELETE $B/1

# 10. JSON malformado e data em formato errado → 400 ProblemDetails
curl -i -X POST $B -H "$J" -d '{"administradora":'
curl -i -X POST $B -H "$J" -d '{"administradora":"X","grupo":"1","cota":"3","dataCompra":"07/07/2026"}'
```

No PowerShell, use `curl.exe`, aspas simples em volta do JSON e acrescente `-H "Authorization: Bearer $TOKEN"` em cada comando (o `alias` acima é só do bash). O token vale 60 minutos; se começar a vir 401, faça login de novo.

## Rodar fora do container (opcional)

Com o banco do Compose no ar (`docker compose up -d db`):

```sh
cd api/ConsorciosMariussi
dotnet run
```

A connection string `Default` do `appsettings.json` já aponta para `Host=localhost;Port=5433;Database=consorcioJoel;Username=postgres;Password=postgres`, a porta que o Compose publica no host. Não é preciso sobrescrever nada.
