# Implementation Plan: Registro de Consórcios (fundação da API)

**Branch**: `001-registro-consorcios` | **Date**: 2026-11-16 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-registro-consorcios/spec.md`

## Summary

Criar a API `ConsorciosMariussi.Api` (ASP.NET Core Minimal API, .NET 10) com os 7 endpoints de `docs/contrato-api.md`: cadastro e login públicos (token JWT de 60 minutos) e os 5 endpoints de consórcio (listar, consultar, criar, editar e excluir; validação 400 e conflito 409), todos protegidos por login. Ela persiste a entidade `Consorcio` no PostgreSQL via EF Core, com as regras do contrato garantidas pelo banco (citext + índice único + check constraints). Também prepara a base de usuários com ASP.NET Core Identity (e-mail + senha de 3 a 30 caracteres, sem exigência de tipo de caractere) e a autenticação JWT Bearer. A API libera CORS só para `http://localhost:5200`, devolve todo erro como ProblemDetails, aplica as migrations na inicialização e roda em container pelo `../docker-compose.yml` existente.

## Technical Context

**Language/Version**: C# / .NET 10 (SDK 10.0.401 instalado)

**Primary Dependencies**: ASP.NET Core Minimal API; `Npgsql.EntityFrameworkCore.PostgreSQL`; `Microsoft.EntityFrameworkCore.Design`; `Microsoft.AspNetCore.Identity.EntityFrameworkCore`; `Microsoft.AspNetCore.Authentication.JwtBearer` (todos na linha 10.x)

**Storage**: PostgreSQL 17 (serviço `db` do Compose), extensão `citext`

**Testing**: nenhum teste automatizado (decisão explícita); validação manual pelo [quickstart.md](quickstart.md)

**Target Platform**: container Linux `mcr.microsoft.com/dotnet/aspnet:10.0`, porta 8080

**Project Type**: web-service (API REST consumida pelo front-end `../web`)

**Performance Goals**: API disponível em até 60 s após o banco ficar pronto (SC-002); sem metas de vazão nesta feature

**Constraints**: não alterar `../docker-compose.yml`; nenhum endpoint fora do contrato; erros sempre em ProblemDetails; sem Repository, MediatR ou AutoMapper

**Scale/Scope**: projeto didático, um único projeto, poucos usuários e alguns milhares de registros

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Verificação | Status |
|---|---|---|
| I. Simplicidade | Um único projeto; `AppDbContext` usado direto nos endpoints; mapeamento request → entidade escrito à mão; validação manual sem bibliotecas; sem Repository, MediatR ou AutoMapper; única classe extra é `MaxLengthPasswordValidator` (interface exigida pelo Identity); a geração do token é uma função estática em `AuthEndpoints.cs`, sem serviço nem interface | ✅ |
| II. Contrato Primeiro | Somente os 7 endpoints do contrato, com as rotas, status, chaves de validação e formatos dele ([auth](contracts/auth-endpoints.md), [consórcios](contracts/consorcios-endpoints.md)). O login entrou primeiro no contrato e só depois na spec. `Grupo`/`Cota` como `string` (o pedido de `int?` violaria o contrato; ver research R1). Campos e nomes JSON batem com o contrato | ✅ (com correção do pedido) |
| III. ProblemDetails | `AddProblemDetails` + `UseExceptionHandler` + `UseStatusCodePages` | ✅ |
| IV. Container | `api/Dockerfile` multi-stage, porta 8080, `ENTRYPOINT ["dotnet","ConsorciosMariussi.Api.dll"]`; `../docker-compose.yml` intocado | ✅ |
| V. Configuração por ambiente | `ConnectionStrings:Default` e `Jwt:*` em `appsettings.json`, sobrescrevíveis por variável de ambiente; nada fixado no código | ✅ |
| VI. Banco autogerenciado | Migration `InitialCreate` versionada; `Database.Migrate()` em um scope na inicialização; sem seed obrigatório (não há papéis) | ✅ |
| VII. Identity + JWT | `IdentityDbContext<IdentityUser>` + `AddIdentityCore`; JWT Bearer emitido pela própria API no login; senha nunca logada nem retornada. Grupo `/api/auth` com `AllowAnonymous()` e grupo `/api/consorcios` com `RequireAuthorization()`, ambos explícitos; 401 em ProblemDetails | ✅ |
| Restrições técnicas | Somente os 4 pacotes listados (permitidos pela constituição) | ✅ |

**Pós-design (Phase 1)**: reavaliado após data-model e contracts. Nenhuma violação nova. `citext` e check constraints são recursos do banco, não abstrações de código.

## Project Structure

### Documentation (this feature)

```text
specs/001-registro-consorcios/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── http-baseline.md
│   ├── auth-endpoints.md
│   └── consorcios-endpoints.md
├── checklists/
│   └── requirements.md
└── tasks.md             # gerado por /speckit-tasks
```

### Source Code (pasta `api/`)

```text
api/
├── Dockerfile                         # multi-stage sdk:10.0 → aspnet:10.0
├── .dockerignore                      # bin, obj, .specify, .claude, specs, docs, .vs, .git
└── ConsorciosMariussi/
    ├── ConsorciosMariussi.Api.csproj  # dotnet new web -n ConsorciosMariussi.Api -o ConsorciosMariussi
    ├── Program.cs                     # DI, CORS, ProblemDetails, JSON, Identity, JWT, Migrate()
    ├── appsettings.json               # ConnectionStrings:Default (localhost:5433), Jwt:*
    ├── appsettings.Development.json
    ├── Data/
    │   └── AppDbContext.cs            # IdentityDbContext<IdentityUser>, DbSet<Consorcio>, mapeamentos
    ├── Models/
    │   └── Consorcio.cs
    ├── Contracts/
    │   ├── ConsorcioRequest.cs        # corpo de POST/PUT (sem id)
    │   ├── AuthRequest.cs             # { email, password }
    │   └── LoginResponse.cs           # { accessToken, expiresIn }
    ├── Endpoints/
    │   ├── AuthEndpoints.cs           # MapAuthEndpoints: register + login + CreateToken
    │   └── ConsorcioEndpoints.cs      # MapConsorcioEndpoints: 5 rotas + Validate + ApplyRequest
    ├── Identity/
    │   └── MaxLengthPasswordValidator.cs
    └── Migrations/                    # InitialCreate (gerada por dotnet ef)
```

**Structure Decision**: um único projeto web na pasta `api/ConsorciosMariussi/`. O `Dockerfile` e o `.dockerignore` ficam em `api/`, que é o contexto de build `./api` do Compose.

## Pontos de implementação (resumo; detalhes em research.md)

1. `Consorcio`: `Id`, `Administradora` (setter com `Trim()`), `Grupo`/`Cota` `string`, `decimal?` e `DateOnly?` conforme o [data-model](data-model.md).
2. `AppDbContext.OnModelCreating`: chamar `base.OnModelCreating` (Identity); `HasPostgresExtension("citext")`; `Administradora` → `citext`; `Grupo`/`Cota` → `HasMaxLength(20)`; decimais → `HasPrecision(18,2)`; índice único `(Administradora, Grupo, Cota)`; 4 check constraints.
3. `Program.cs`, nesta ordem: `AddDbContext` (Npgsql, `GetConnectionString("Default")`) → `AddIdentityCore` com opções de senha/e-mail + `AddPasswordValidator<MaxLengthPasswordValidator>` + `AddEntityFrameworkStores` → `AddAuthentication().AddJwtBearer` com `Jwt:*` → `AddAuthorization` → `AddCors("Frontend")` → `AddProblemDetails` → `ConfigureHttpJsonOptions` (`JsonStringEnumConverter`). Pipeline: `UseExceptionHandler` → `UseStatusCodePages` → `UseCors("Frontend")` → `UseAuthentication` → `UseAuthorization`. Antes de `app.Run()`, `Migrate()` em um scope, `app.MapAuthEndpoints()` e `app.MapConsorcioEndpoints()`. Remover o `MapGet("/")` do template (FR-018).
4. `AuthEndpoints`: grupo `/api/auth` anônimo; register via `UserManager.CreateAsync` com mapeamento dos erros do Identity (400/409); login via `FindByEmailAsync` + `CheckPasswordAsync` (401 único) e `CreateToken` com `JsonWebTokenHandler`, 60 minutos (research R7a–R7c).
5. `ConsorcioEndpoints`: grupo `/api/consorcios` com `RequireAuthorization()`; rotas com `{id:int}`; validação 400 com chaves `administradora`/`grupo`/`cota`/`credito`; 409 capturando a violação única `23505`; decimais com 2 casas; ordenação da lista (research R15–R20).
6. `dotnet ef migrations add InitialCreate` dentro de `ConsorciosMariussi/`.
7. `Dockerfile` e `.dockerignore` conforme research R12.

## Riscos e observações

- **Porta local**: o `appsettings.json` aponta para `localhost:5433`, a porta que o Compose publica para o banco; `dotnet run` fora do container funciona sem sobrescrever nada (ver [quickstart](quickstart.md)).
- **`citext`**: depende de a extensão poder ser criada pelo usuário `postgres` (pode, na imagem oficial). Em outro servidor sem permissão, a migration falha.
- **Cadastro aberto**: qualquer pessoa com acesso à porta 5080 pode criar uma conta e, com ela, ver e alterar todos os consórcios (não há papéis nem dono do consórcio). Isso é aceitável no ambiente didático/local, por decisão do usuário.
- **JWT Key** em `appsettings.json` é um valor de desenvolvimento; deve ser sobrescrita por `Jwt__Key` fora do ambiente didático.

## Complexity Tracking

Nenhuma violação a justificar. `MaxLengthPasswordValidator` implementa uma interface exigida pelo framework (exceção prevista no Princípio I), porque o Identity não tem opção de tamanho máximo de senha.
