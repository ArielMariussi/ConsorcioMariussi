---

description: "Task list for feature 001-registro-consorcios"
---

# Tasks: Registro de Consórcios (fundação da API)

**Input**: Design documents from `specs/001-registro-consorcios/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: nenhum teste automatizado (decisão do plano). Cada história termina com uma tarefa de validação manual pelo [quickstart.md](quickstart.md).

**Organization**: tarefas agrupadas por user story. Como o plano exige **uma única** migration `InitialCreate`, todo o esquema (Consorcio + Identity + restrições) fica na Phase 2. As histórias acrescentam comportamento sobre esse esquema.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1–US5, conforme a spec
- Caminhos relativos à pasta `api/`. O projeto fica em `ConsorciosMariussi/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: criar o projeto e a configuração básica

- [X] T001 Criar o projeto com `dotnet new web -n ConsorciosMariussi.Api -o ConsorciosMariussi` a partir de `api/`, gerando `ConsorciosMariussi/ConsorciosMariussi.Api.csproj` (TargetFramework `net10.0`, namespace raiz `ConsorciosMariussi.Api`, saída `ConsorciosMariussi.Api.dll`)
- [X] T002 Adicionar os pacotes da linha 10.x em `ConsorciosMariussi/ConsorciosMariussi.Api.csproj`, executando em `ConsorciosMariussi/`: `dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL`, `dotnet add package Microsoft.EntityFrameworkCore.Design`, `dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer`. Não adicionar nenhum outro pacote
- [X] T003 [P] Substituir o conteúdo de `ConsorciosMariussi/appsettings.json` por: `Logging` (padrão do template), `AllowedHosts: "*"`, `ConnectionStrings.Default = "Host=localhost;Port=5433;Database=consorcioJoel;Username=postgres;Password=postgres"` e a seção `Jwt` com `Issuer = "ConsorciosMariussi.Api"`, `Audience = "ConsorciosMariussi.Web"` e `Key` = uma string de desenvolvimento com pelo menos 32 caracteres (ex.: `"chave-de-desenvolvimento-consorcios-mariussi-2026"`)
- [X] T004 [P] Garantir que o `.gitignore` em `api/.gitignore` ignore `bin/` e `obj/` (o arquivo já existe; só acrescentar se faltar)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: esquema completo do banco, a migration `InitialCreate` e o pipeline base com ProblemDetails

**⚠️ CRITICAL**: nenhuma história pode começar antes desta fase

- [X] T005 [P] Criar `ConsorciosMariussi/Models/Consorcio.cs` (namespace `ConsorciosMariussi.Api.Models`) com as propriedades: `int Id`; `string Administradora` (obrigatória, inicializada com `""`); `string Grupo` e `string Cota` (obrigatórias, inicializadas com `""`; **string, não int**, para manter zeros à esquerda: "só dígitos (mantém zeros à esquerda)"); `decimal? Credito`; `decimal? ValorPago`; `DateOnly? DataCompra`; `DateOnly? Vencimento`; `DateOnly? Assembleia`; `decimal? Comissao`; `decimal? PrevisaoLucro`; `decimal? FundoReserva`; `DateOnly? Contemplacao`; `decimal? TaxaTransferencia`; `DateOnly? DataLucro`; `decimal? Lucro`
- [X] T006 Criar `ConsorciosMariussi/Data/AppDbContext.cs` (namespace `ConsorciosMariussi.Api.Data`): classe `AppDbContext : IdentityDbContext<IdentityUser>` com construtor recebendo `DbContextOptions<AppDbContext>`, `DbSet<Consorcio> Consorcios` e `OnModelCreating` que: (1) chama `base.OnModelCreating(modelBuilder)` primeiro; (2) `modelBuilder.HasPostgresExtension("citext")`; (3) na entidade `Consorcio`: tabela `Consorcios`; `Administradora` `IsRequired()` com `HasColumnType("citext")`; `Grupo` e `Cota` `IsRequired()` com `HasMaxLength(20)`; todas as propriedades `decimal?` com `HasPrecision(18, 2)`; índice único `HasIndex(c => new { c.Administradora, c.Grupo, c.Cota }).IsUnique()`; e, via `ToTable("Consorcios", t => ...)`, as check constraints exatamente como no data-model: `CK_Consorcios_Administradora` = `"Administradora" <> '' AND "Administradora" = btrim("Administradora")`, `CK_Consorcios_Grupo` = `"Grupo" ~ '^[0-9]+$'`, `CK_Consorcios_Cota` = `"Cota" ~ '^[0-9]+$'`, `CK_Consorcios_Credito` = `"Credito" IS NULL OR "Credito" >= 0` (depende de T005)
- [X] T007 Reescrever `ConsorciosMariussi/Program.cs` com a base: `builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Default")))`; `builder.Services.AddProblemDetails()`; `builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()))`; após `Build()`: `app.UseExceptionHandler()` e `app.UseStatusCodePages()`; **remover** o `app.MapGet("/", ...)` do template (só endpoints do contrato, FR-018); terminar com `app.Run()` (depende de T006)
- [X] T008 Instalar a ferramenta, se faltar (`dotnet tool install --global dotnet-ef`), e gerar a migration em `ConsorciosMariussi/` com `dotnet ef migrations add InitialCreate`, criando `ConsorciosMariussi/Migrations/`. Conferir no arquivo gerado: a extensão `citext` (`AlterDatabase().Annotation("Npgsql:PostgresExtension:citext", ...)`), a tabela `Consorcios` com `citext`, `varchar(20)`, `numeric(18,2)` e `date`, as 4 check constraints, o índice único `IX_Consorcios_Administradora_Grupo_Cota` e as tabelas `AspNet*` (depende de T007)
- [X] T009 Executar `dotnet build` em `ConsorciosMariussi/` e corrigir os erros até compilar sem erros

**Checkpoint**: o projeto compila, a migration existe e ainda não há endpoints (eles chegam na US4 e na US5)

---

## Phase 3: User Story 1 - Subir a plataforma com um único comando (Priority: P1) 🎯 MVP

**Goal**: `docker compose up` na pasta raiz do app sobe banco + API, com o esquema criado automaticamente e os dados persistentes

**Independent Test**: seção "US1" do [quickstart.md](quickstart.md): `docker compose down -v`, `docker compose up --build -d db api`, `curl -i http://localhost:5080/` responde 404 `application/problem+json`, e `\d "Consorcios"` mostra a tabela

### Implementation for User Story 1

- [X] T010 [US1] Em `ConsorciosMariussi/Program.cs`, logo após `var app = builder.Build();`, aplicar as migrations: `using (var scope = app.Services.CreateScope()) { scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate(); }`
- [X] T011 [P] [US1] Criar `api/.dockerignore` com: `**/bin`, `**/obj`, `.specify`, `.claude`, `specs`, `docs`, `.vs`, `.git`
- [X] T012 [P] [US1] Criar `api/Dockerfile` multi-stage (contexto de build = `api/`): estágio `build` em `mcr.microsoft.com/dotnet/sdk:10.0` com `WORKDIR /src`, `COPY ConsorciosMariussi/ConsorciosMariussi.Api.csproj ConsorciosMariussi/`, `RUN dotnet restore ConsorciosMariussi/ConsorciosMariussi.Api.csproj`, `COPY . .`, `RUN dotnet publish ConsorciosMariussi/ConsorciosMariussi.Api.csproj -c Release -o /app/publish /p:UseAppHost=false`; estágio final em `mcr.microsoft.com/dotnet/aspnet:10.0` com `WORKDIR /app`, `COPY --from=build /app/publish .`, `EXPOSE 8080`, `ENTRYPOINT ["dotnet", "ConsorciosMariussi.Api.dll"]`. Não alterar `../docker-compose.yml`
- [X] T013 [US1] Validar pelo quickstart (seção US1): na pasta raiz do app, `docker compose down -v` + `docker compose up --build -d db api`; conferir nos logs as migrations aplicadas e `Now listening on: http://[::]:8080`; `curl -i http://localhost:5080/` → 404 ProblemDetails; `\d "Consorcios"` e `\dt` mostram a tabela, as `AspNet*` e `__EFMigrationsHistory`; `docker compose restart api` sobe sem erro (depende de T010–T012)

**Checkpoint**: MVP: a plataforma sobe com um comando e o armazenamento existe

---

## Phase 4: User Story 2 - Registros de consórcio sempre válidos (Priority: P2)

**Goal**: o armazenamento rejeita registros inválidos e preserva os valores (zeros à esquerda, 2 casas decimais, datas sem hora)

**Independent Test**: seção "US2" do [quickstart.md](quickstart.md): o insert válido é aceito e cada insert inválido falha

### Implementation for User Story 2

- [X] T014 [US2] Em `ConsorciosMariussi/Models/Consorcio.cs`, transformar `Administradora` em propriedade com campo privado e setter que aplica `Trim()`: `private string _administradora = ""; public string Administradora { get => _administradora; set => _administradora = value?.Trim() ?? ""; }` ("Obrigatório, sem espaços nas pontas"; FR-003). Não muda o esquema; não gerar migration nova
- [X] T015 [US2] Validar pelo quickstart (seção US2), com a API no ar: o insert `('Sicredi','05014','0361',200000.005,'2026-07-07')` é aceito e o SELECT devolve `05014 | 0361 | 200000.01 | 2026-07-07`; cada um dos 7 inserts inválidos (SICREDI duplicado, espaços nas pontas, vazia, `05A14`, `03 61`, grupo NULL, crédito -1) falha com violação de unique, check ou not-null; depois de `docker compose restart api`, o registro válido continua lá (SC-005); ao final, `DELETE FROM "Consorcios";`

**Checkpoint**: US1 e US2 funcionam

---

## Phase 5: User Story 3 - Front-end web consegue chamar a API (Priority: P3)

**Goal**: CORS liberado só para `http://localhost:5200`, com o cabeçalho `Location` exposto

**Independent Test**: seção "US3" do [quickstart.md](quickstart.md): preflight de `http://localhost:5200` autorizado, de `http://localhost:3000` não, e `Access-Control-Expose-Headers: Location` presente

### Implementation for User Story 3

- [X] T016 [US3] Em `ConsorciosMariussi/Program.cs`, registrar `builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p.WithOrigins("http://localhost:5200").AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Location")))` e, no pipeline, `app.UseCors("Frontend")` logo após `app.UseStatusCodePages()` (FR-014, FR-015, FR-016)
- [X] T017 [US3] Rebuild (`docker compose up --build -d api`) e validar os 3 `curl` da seção US3 do quickstart, incluindo que a origem `http://127.0.0.1:5200` também não é autorizada (depende de T016)

**Checkpoint**: US1, US2 e US3 funcionam

---

## Phase 6: User Story 4 - Cadastro e login com e-mail e senha (Priority: P4)

**Goal**: `POST /api/auth/register` e `POST /api/auth/login` públicos, com e-mail único (sem diferenciar maiúsculas/minúsculas), senha de 3 a 30 caracteres sem exigência de tipo de caractere, e token JWT de 60 minutos

**Independent Test**: seção "US4" do [quickstart.md](quickstart.md): os 5 blocos de `curl` de cadastro/login retornam os status de [contracts/auth-endpoints.md](contracts/auth-endpoints.md), e `AspNetUsers` guarda só o hash da senha

### Implementation for User Story 4

- [X] T018 [P] [US4] Criar `ConsorciosMariussi/Identity/MaxLengthPasswordValidator.cs` (namespace `ConsorciosMariussi.Api.Identity`): `public class MaxLengthPasswordValidator : IPasswordValidator<IdentityUser>` com `public const int MaxLength = 30;` e `ValidateAsync` que retorna `IdentityResult.Failed(new IdentityError { Code = "PasswordTooLong", Description = "A senha deve ter no máximo 30 caracteres." })` quando `password.Length > 30` e `IdentityResult.Success` caso contrário. Nunca registrar a senha em log
- [X] T019 [P] [US4] Criar `ConsorciosMariussi/Contracts/AuthRequest.cs` com `public record AuthRequest(string? Email, string? Password);` e `ConsorciosMariussi/Contracts/LoginResponse.cs` com `public record LoginResponse(string AccessToken, int ExpiresIn);` (namespace `ConsorciosMariussi.Api.Contracts`; JSON `{ "accessToken", "expiresIn" }`)
- [X] T020 [US4] Em `ConsorciosMariussi/Program.cs`, registrar o Identity: `builder.Services.AddIdentityCore<IdentityUser>(o => { o.User.RequireUniqueEmail = true; o.Password.RequiredLength = 3; o.Password.RequireDigit = false; o.Password.RequireLowercase = false; o.Password.RequireUppercase = false; o.Password.RequireNonAlphanumeric = false; o.Password.RequiredUniqueChars = 1; }).AddPasswordValidator<MaxLengthPasswordValidator>().AddEntityFrameworkStores<AppDbContext>();` ("mínimo 3 e máximo 30 caracteres; nenhum tipo de caractere é obrigatório"; FR-020, FR-021) (depende de T018)
- [X] T021 [US4] Em `ConsorciosMariussi/Program.cs`, configurar o JWT: `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"], ValidateLifetime = true, ClockSkew = TimeSpan.Zero, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)) });` + `builder.Services.AddAuthorization();`; no pipeline, `app.UseAuthentication(); app.UseAuthorization();` depois de `app.UseCors("Frontend")` (depende de T020)
- [X] T022 [US4] Criar `ConsorciosMariussi/Endpoints/AuthEndpoints.cs` (namespace `ConsorciosMariussi.Api.Endpoints`), `public static class AuthEndpoints`, com: `private const int TokenMinutes = 60;`; helper `ValidatePresence(AuthRequest r)` → `Dictionary<string, string[]>` com `"email"` ("O e-mail é obrigatório.") se `string.IsNullOrWhiteSpace(r.Email)` e `"password"` ("A senha é obrigatória.") se `string.IsNullOrEmpty(r.Password)`; helper `CreateToken(IdentityUser user, IConfiguration config)` usando `new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor { Issuer = config["Jwt:Issuer"], Audience = config["Jwt:Audience"], Expires = DateTime.UtcNow.AddMinutes(TokenMinutes), Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, user.Id), new Claim(JwtRegisteredClaimNames.Email, user.Email!)]), SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256) })`; e `public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)` com `var group = app.MapGroup("/api/auth").AllowAnonymous();` (depende de T019, T021)
- [X] T023 [US4] No mesmo arquivo, `group.MapPost("/register", async (AuthRequest request, UserManager<IdentityUser> users) => ...)`: `ValidatePresence` → 400 `TypedResults.ValidationProblem`; `users.CreateAsync(new IdentityUser { UserName = request.Email, Email = request.Email }, request.Password!)`; se falhar: algum erro com `Code` `DuplicateEmail` ou `DuplicateUserName` → `TypedResults.Problem(statusCode: 409, title: "Conflict", detail: "Já existe um usuário com este e-mail.")`; senão, montar o dicionário de erros com códigos que começam com `"Password"` na chave `"password"` e os demais (`InvalidEmail`, `InvalidUserName`) na chave `"email"`, retornando `TypedResults.ValidationProblem`; sucesso → `TypedResults.NoContent()` (204) (FR-033) (depende de T022)
- [X] T024 [US4] No mesmo arquivo, `group.MapPost("/login", async (AuthRequest request, UserManager<IdentityUser> users, IConfiguration config) => ...)`: `ValidatePresence` → 400; `var user = await users.FindByEmailAsync(request.Email!);` se `user is null || !await users.CheckPasswordAsync(user, request.Password!)` → `TypedResults.Problem(statusCode: 401, title: "Unauthorized", detail: "E-mail ou senha inválidos.")` (mesma resposta nos dois casos); senão `TypedResults.Ok(new LoginResponse(CreateToken(user, config), TokenMinutes * 60))`; ao final de `MapAuthEndpoints`, `return app;` (FR-034, FR-035) (depende de T023)
- [X] T025 [US4] Em `ConsorciosMariussi/Program.cs`, chamar `app.MapAuthEndpoints();` depois de `app.UseAuthorization()` e antes de `app.Run()` (depende de T024)
- [X] T026 [US4] Rebuild (`docker compose up --build -d api`) e executar a seção US4 do quickstart: 204 para "abc" e para a senha de 30 caracteres; 400 `password` para 2 e 31; 409 para `JOEL@exemplo.com`; 400 `email` para "joel" e para e-mail ausente; login 200 com `accessToken` e `expiresIn: 3600`; 401 idênticos para senha errada e e-mail inexistente; 400 para vazios; `AspNetUsers` só com `PasswordHash` (depende de T025)

**Checkpoint**: US1 a US4 funcionam; é possível obter um token

---

## Phase 7: User Story 5 - Gerenciar consórcios pela API (Priority: P5)

**Goal**: os 5 endpoints de consórcio de `docs/contrato-api.md`, **protegidos** (401 sem token válido), com 400/404/409 em ProblemDetails

**Independent Test**: seção "US5" do [quickstart.md](quickstart.md): sem token → 401; com token, a sequência de `curl` retorna exatamente os status, corpos e cabeçalhos de [contracts/consorcios-endpoints.md](contracts/consorcios-endpoints.md)

### Implementation for User Story 5

- [X] T027 [P] [US5] Criar `ConsorciosMariussi/Contracts/ConsorcioRequest.cs` (namespace `ConsorciosMariussi.Api.Contracts`): `public record ConsorcioRequest(string? Administradora, string? Grupo, string? Cota, decimal? Credito, decimal? ValorPago, DateOnly? DataCompra, DateOnly? Vencimento, DateOnly? Assembleia, decimal? Comissao, decimal? PrevisaoLucro, decimal? FundoReserva, DateOnly? Contemplacao, decimal? TaxaTransferencia, DateOnly? DataLucro, decimal? Lucro);` ("todos os campos do Consorcio, exceto id"; todos anuláveis para que campo ausente gere 400 de validação)
- [X] T028 [US5] Criar `ConsorciosMariussi/Endpoints/ConsorcioEndpoints.cs` (namespace `ConsorciosMariussi.Api.Endpoints`), `public static class ConsorcioEndpoints`, com os helpers privados estáticos: (a) `Validate(ConsorcioRequest r)` → `Dictionary<string, string[]>` com as chaves exatas do contrato: `"administradora"` se nula ou vazia após `Trim()` ("A administradora é obrigatória."); `"grupo"` se nulo/vazio ("O grupo é obrigatório."), com caractere fora de `'0'..'9'` ("O grupo deve conter somente dígitos.") ou com mais de 20 caracteres; `"cota"` com as mesmas regras e mensagens de cota; `"credito"` se `< 0` ("O crédito não pode ser negativo."). Não usar `char.IsDigit`; (b) `Round2(decimal? v)` → `v is null ? null : Math.Round(v.Value, 2, MidpointRounding.AwayFromZero) + 0.00m`; (c) `ApplyRequest(Consorcio c, ConsorcioRequest r)` que copia **todos** os 15 campos (`Administradora = r.Administradora!`, que o setter apara; `Grupo = r.Grupo!`; `Cota = r.Cota!`; cada `decimal?` via `Round2`; cada `DateOnly?` direto); (d) `IsUniqueViolation(DbUpdateException e)` → `e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }`; (e) `Conflict()` → `TypedResults.Problem(statusCode: 409, title: "Conflict", detail: "Já existe um consórcio com a mesma administradora, grupo e cota.")` (depende de T027, T014)
- [X] T029 [US5] Em `ConsorciosMariussi/Endpoints/ConsorcioEndpoints.cs`, criar `public static IEndpointRouteBuilder MapConsorcioEndpoints(this IEndpointRouteBuilder app)` com `var group = app.MapGroup("/api/consorcios").RequireAuthorization();` ("Protegido": sem token válido → 401) e o endpoint `group.MapGet("/", ...)` que retorna `TypedResults.Ok(await db.Consorcios.AsNoTracking().OrderByDescending(c => c.DataCompra.HasValue).ThenByDescending(c => c.DataCompra).ThenByDescending(c => c.Id).ToListAsync())` ("ordenados pela data da compra, dos mais recentes para os mais antigos"; sem data por último) (depende de T028)
- [X] T030 [US5] No mesmo arquivo, `group.MapGet("/{id:int}", ...)`: `FindAsync(id)`; `null` → `TypedResults.NotFound()`; senão `TypedResults.Ok(consorcio)` (depende de T029)
- [X] T031 [US5] No mesmo arquivo, `group.MapPost("/", async (ConsorcioRequest request, AppDbContext db) => ...)`: `Validate` → se houver erros, `TypedResults.ValidationProblem(errors)`; criar `new Consorcio()`, `ApplyRequest`, `db.Consorcios.Add`, `SaveChangesAsync` dentro de `try/catch (DbUpdateException e) when (IsUniqueViolation(e))` → `Conflict()`; sucesso → `TypedResults.Created($"/api/consorcios/{c.Id}", c)` (201 + Location) (depende de T030)
- [X] T032 [US5] No mesmo arquivo, `group.MapPut("/{id:int}", ...)`, nesta ordem: `Validate` (400) → `FindAsync(id)` (`null` → 404) → `ApplyRequest` sobre a entidade encontrada (substitui todos os campos; ausentes viram `null`) → `SaveChangesAsync` com o mesmo `catch` de unicidade (409) → `TypedResults.NoContent()` (204) (depende de T031)
- [X] T033 [US5] No mesmo arquivo, `group.MapDelete("/{id:int}", ...)`: `FindAsync(id)` (`null` → 404) → `Remove` → `SaveChangesAsync` → `TypedResults.NoContent()`; ao final do método, `return app;` (depende de T032)
- [X] T034 [US5] Em `ConsorciosMariussi/Program.cs`, chamar `app.MapConsorcioEndpoints();` logo após `app.MapAuthEndpoints();` (depende de T033, T025)
- [X] T035 [US5] Rebuild (`docker compose up --build -d api`) e executar a seção US5 do quickstart: passo 0 (401 sem token e com token inválido); obter o token; passos 1 a 10 conferindo cada status, o `Location` do 201, `administradora` aparada, `credito` como `200000.00`, a ordem da lista, `valorPago: null` após o PUT e as chaves do 400 (depende de T034)

**Checkpoint**: todas as histórias funcionam; o front-end consegue criar conta, entrar e gerenciar consórcios

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T036 Revisar `ConsorciosMariussi/Program.cs` na ordem final: `Migrate()` → `UseExceptionHandler()` → `UseStatusCodePages()` → `UseCors("Frontend")` → `UseAuthentication()` → `UseAuthorization()` → `MapAuthEndpoints()` → `MapConsorcioEndpoints()` → `Run()`; confirmar que as únicas rotas são as 7 do contrato (FR-018) e remover `using`s não usados
- [X] T037 Conferir a constituição: nenhum Repository, MediatR ou AutoMapper; apenas os 4 pacotes de T002 no `.csproj`; `/api/auth` com `AllowAnonymous()` e `/api/consorcios` com `RequireAuthorization()`, ambos explícitos; nenhum log contém senha; `../docker-compose.yml` sem alterações (`git -C .. status` ou comparação manual)
- [X] T038 Executar o [quickstart.md](quickstart.md) inteiro em sequência a partir de `docker compose down -v` (US1 → US5) e, opcionalmente, a seção "Rodar fora do container" com `dotnet run` contra `localhost:5433`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências
- **Foundational (Phase 2)**: depende da Setup; bloqueia todas as histórias
- **US1 (Phase 3)**: depende da Foundational
- **US2 (Phase 4)**: o código (T014) depende só da Foundational; a validação (T015) precisa do ambiente da US1 no ar
- **US3 (Phase 5)**: o código (T016) depende só da Foundational; a validação (T017) precisa do ambiente da US1
- **US4 (Phase 6)**: o código depende da Foundational e de T016 (a ordem `UseCors` → `UseAuthentication` no `Program.cs`); a validação (T026) precisa do ambiente da US1
- **US5 (Phase 7)**: depende de US2 (T014, o `Trim()` no setter) e de US4 (token e `MapAuthEndpoints`); a validação (T035) precisa do ambiente da US1
- **Polish (Phase 8)**: depois de todas as histórias

### Within Each Story

- T010, T016, T020, T021, T025, T034 e T036 editam `Program.cs`, então devem ser feitas uma de cada vez
- T005 → T006 → T007 → T008 → T009 em sequência
- T022 → T023 → T024 editam `AuthEndpoints.cs` em sequência
- T028 → T029 → T030 → T031 → T032 → T033 editam `ConsorcioEndpoints.cs` em sequência

### Parallel Opportunities

- Phase 1: T003 ∥ T004
- Phase 2: T005 pode ser escrita junto com T003/T004
- US1: T011 ∥ T012 (arquivos diferentes), ambas em paralelo com T010
- US4: T018 ∥ T019, e ambas em paralelo com qualquer tarefa de US2/US3
- US5: T027 em paralelo com qualquer tarefa depois da Foundational

## Parallel Example: User Story 4

```text
Task: "T018 [US4] Criar Identity/MaxLengthPasswordValidator.cs"
Task: "T019 [US4] Criar Contracts/AuthRequest.cs e Contracts/LoginResponse.cs"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 + Phase 2
2. Phase 3 (US1)
3. **Parar e validar**: `docker compose up` sobe tudo e cria o esquema

### Incremental Delivery

1. Setup + Foundational → esquema e migration prontos
2. US1 → plataforma sobe com um comando (MVP)
3. US2 → `Trim()` + validação das regras no banco
4. US3 → CORS para o front-end
5. US4 → cadastro e login com token
6. US5 → endpoints de consórcio protegidos (o front-end passa a funcionar)
7. Polish → revisão final e quickstart completo

## Notes

- Não gerar migrations além da `InitialCreate`: nenhuma tarefa posterior à T008 muda o esquema
- Se a `InitialCreate` precisar ser refeita antes da entrega: `dotnet ef migrations remove`, `dotnet ef migrations add InitialCreate` e `docker compose down -v`
- Fora do escopo: refresh token, logout, papéis, confirmação de e-mail e bloqueio por tentativas
