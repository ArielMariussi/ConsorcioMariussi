# Research: Registro de Consórcios (fundação da API)

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-11-16

Cada item registra: Decisão, Racional e Alternativas consideradas.

## R1. Tipo de `Grupo` e `Cota`

- **Decisão**: `string` obrigatório (não `int?`), coluna `varchar(20)` com check constraint "só dígitos".
- **Racional**: o contrato (`docs/contrato-api.md`) define `grupo` e `cota` como `string`, obrigatórios, "só dígitos (mantém zeros à esquerda)". Com `int?`, "05014" vira `5014` e o JSON passa a ser número, violando o contrato e o Princípio II (divergência código × contrato é defeito). O pedido original de usar `int?` foi substituído por esse motivo.
- **Alternativas**: `int?` (rejeitada: perde zeros à esquerda, permite nulo e muda o tipo JSON); `int` + formatação com zeros (rejeitada: o número de dígitos varia por administradora, não há largura fixa).

## R2. Nome do projeto e da DLL

- **Decisão**: `dotnet new web -n ConsorciosMariussi.Api -o ConsorciosMariussi`. Pasta `ConsorciosMariussi/`, arquivo `ConsorciosMariussi.Api.csproj`, namespace raiz `ConsorciosMariussi.Api`, saída `ConsorciosMariussi.Api.dll`.
- **Racional**: o `ENTRYPOINT` pedido é `ConsorciosMariussi.Api.dll`, e a constituição chama o projeto de "ConsorciosMariussi.Api". Um `dotnet new web -n ConsorciosMariussi` geraria `ConsorciosMariussi.dll`. Nomear o projeto com `.Api` deixa pasta, DLL e constituição coerentes sem configurar `AssemblyName`. O nome "ConsorsciosMariussi" no pedido foi tratado como erro de digitação.
- **Alternativas**: `-n ConsorciosMariussi` + `<AssemblyName>ConsorciosMariussi.Api</AssemblyName>` (funciona, mas cria divergência entre nome do csproj e da DLL); `ENTRYPOINT` com `ConsorciosMariussi.dll` (rejeitada: contraria o pedido).

## R3. Unicidade de administradora sem diferenciar maiúsculas/minúsculas

- **Decisão**: coluna `Administradora` do tipo PostgreSQL `citext` (extensão `citext` habilitada pela migration via `HasPostgresExtension("citext")`) e índice único em `(Administradora, Grupo, Cota)`.
- **Racional**: com `citext` o próprio índice único compara sem diferenciar maiúsculas/minúsculas, preservando a grafia gravada (FR-008). Não exige coluna extra nem código de normalização. A imagem `postgres:17` já traz a extensão, e o usuário `postgres` pode criá-la.
- **Alternativas**: coluna sombra `AdministradoraNormalizada` com `ToUpper()` (rejeitada: código e coluna a mais); índice de expressão `lower("Administradora")` (rejeitada: o EF Core não modela índice de expressão, exigiria SQL manual na migration); collation ICU não determinística (rejeitada: mais complexa e com restrições em `LIKE`).

## R4. Garantia das regras no armazenamento (FR-009)

- **Decisão**: check constraints no banco, declaradas no `OnModelCreating` com `ToTable(t => t.HasCheckConstraint(...))`:
  - `CK_Consorcios_Administradora`: `"Administradora" <> '' AND "Administradora" = btrim("Administradora")`
  - `CK_Consorcios_Grupo`: `"Grupo" ~ '^[0-9]+$'`
  - `CK_Consorcios_Cota`: `"Cota" ~ '^[0-9]+$'`
  - `CK_Consorcios_Credito`: `"Credito" IS NULL OR "Credito" >= 0`

  A remoção dos espaços das pontas é feita no setter da propriedade `Administradora` (`value?.Trim()`). Assim uma gravação com "  Sicredi  " é salva como "Sicredi", e a constraint protege contra qualquer caminho que contorne o setter.
- **Racional**: o banco garante as regras mesmo que um endpoint futuro esqueça de validar. A validação amigável (400 com ProblemDetails) fica para as features de endpoints.
- **Alternativas**: validar só no código C# (rejeitada: não atende FR-009); sobrescrever `SaveChanges` para aparar strings (rejeitada: mais código que o setter).

## R5. Valores monetários e datas

- **Decisão**: `decimal?` mapeado para `numeric(18,2)` (via `HasPrecision(18, 2)` em convenção para todas as propriedades decimais); `DateOnly?` mapeado para `date` (padrão do Npgsql).
- **Racional**: `numeric(18,2)` arredonda para 2 casas na gravação (assumption da spec) e comporta valores de crédito realistas. `DateOnly` + `date` não tem hora nem fuso (FR-007), e o System.Text.Json serializa `DateOnly` como `yyyy-MM-dd`, como pede o contrato.
- **Alternativas**: `double` (rejeitada: imprecisão monetária); `DateTime` (rejeitada: hora e fuso podem deslocar o dia).

## R6. Identity e regras de senha

- **Decisão**: `AppDbContext : IdentityDbContext<IdentityUser>`; `AddIdentityCore<IdentityUser>(...).AddEntityFrameworkStores<AppDbContext>()` com:
  - `User.RequireUniqueEmail = true` (o Identity compara pelo `NormalizedEmail`, sem diferenciar maiúsculas/minúsculas: FR-020);
  - `Password.RequiredLength = 3`, `RequireDigit/RequireLowercase/RequireUppercase/RequireNonAlphanumeric = false`, `RequiredUniqueChars = 1` (FR-021);
  - máximo de 30 caracteres via uma classe `MaxLengthPasswordValidator : IPasswordValidator<IdentityUser>` registrada com `.AddPasswordValidator<>()`, porque o Identity não tem opção de tamanho máximo.
  - O e-mail é usado também como `UserName`.
- **Racional**: é o caminho nativo (Princípio VII). `AddIdentityCore` evita registrar cookies e telas que a API não usa. A interface do validador é exigida pelo framework, então é permitida pelo Princípio I.
- **Alternativas**: `AddIdentity<,>` completo (rejeitada: registra autenticação por cookie desnecessária); validar o tamanho máximo só no endpoint futuro (rejeitada: FR-021 vale para a base de usuários).
- **Papéis**: nenhum (assumption da spec). Não há seed obrigatório nesta feature.

## R7. JWT Bearer

- **Decisão**: `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` validando emissor, audiência, tempo de vida e chave de assinatura lidos de `Jwt:Issuer`, `Jwt:Audience` e `Jwt:Key` em `appsettings.json` (sobrescrevíveis por `Jwt__Key` etc.), com `ClockSkew = TimeSpan.Zero` para que o token expire exatamente em 60 minutos. `AddAuthorization()`, `UseAuthentication()` e `UseAuthorization()` no pipeline.
- **Racional**: Princípios V e VII. O Compose não fornece essas variáveis, então `appsettings.json` precisa ter valores funcionais (chave HMAC com pelo menos 32 bytes). Sem `ClockSkew = 0`, o padrão de 5 minutos aceitaria o token até 65 minutos.

## R7a. Emissão do token (login)

- **Decisão**: método estático privado `CreateToken(IdentityUser user, IConfiguration config)` em `AuthEndpoints.cs`, usando `JsonWebTokenHandler` (pacote `Microsoft.IdentityModel.JsonWebTokens`, que já vem como dependência do JwtBearer) com `SecurityTokenDescriptor`: `Issuer`, `Audience`, `Expires = DateTime.UtcNow.AddMinutes(60)`, `SigningCredentials` HMAC-SHA256 com `Jwt:Key`, e claims `sub` = `user.Id` e `email` = `user.Email`. A validade é a constante `TokenMinutes = 60`, e o login responde `expiresIn = TokenMinutes * 60` (3600).
- **Racional**: o contrato fixa 60 minutos, então uma constante basta. `JsonWebTokenHandler` é o handler atual recomendado (mais rápido que `JwtSecurityTokenHandler`) e não exige pacote novo. Uma função dentro do arquivo de endpoints evita criar um serviço de token com interface (Princípio I).
- **Alternativas**: `JwtSecurityTokenHandler` (legado); um `TokenService` registrado no DI (rejeitada: abstração sem necessidade); validade em configuração (rejeitada: o contrato fixa 60 minutos).

## R7b. Cadastro (register)

- **Decisão**: validar à mão só a presença (`email` e `password` não vazios → 400). Em seguida, `userManager.CreateAsync(new IdentityUser { UserName = email, Email = email }, password)` e mapear os erros do `IdentityResult` pelo `Code`: `DuplicateEmail`/`DuplicateUserName` → 409 ProblemDetails ("Já existe um usuário com este e-mail."); códigos que começam com `Password` (incluindo o `PasswordTooLong` do `MaxLengthPasswordValidator`) → chave `password`; demais códigos (`InvalidEmail`, `InvalidUserName`) → chave `email`. Sucesso → 204.
- **Racional**: o Identity já valida o formato do e-mail (com `RequireUniqueEmail`), a unicidade sem diferenciar maiúsculas/minúsculas e as regras de senha. Repetir essas regras à mão duplicaria a lógica. As descrições dos erros do Identity saem em inglês; as mensagens dos erros mapeados podem ser reescritas em português no mapeamento.
- **Alternativas**: validar o formato do e-mail à mão (rejeitada: duplica o Identity).

## R7c. Login

- **Decisão**: validar presença (400, chaves `email`/`password`) → `userManager.FindByEmailAsync(email)` → `user is null || !await userManager.CheckPasswordAsync(user, password)` → `TypedResults.Problem(statusCode: 401, title: "Unauthorized", detail: "E-mail ou senha inválidos.")`; senão `TypedResults.Ok(new LoginResponse(token, 3600))`.
- **Racional**: a mesma resposta para e-mail inexistente e senha errada não revela quais e-mails estão cadastrados (FR-034). `FindByEmailAsync` usa o e-mail normalizado, então o login não diferencia maiúsculas/minúsculas.

## R7d. 401 dos endpoints protegidos

- **Decisão**: grupo `/api/consorcios` com `.RequireAuthorization()`; grupo `/api/auth` com `.AllowAnonymous()`. O desafio do JwtBearer devolve 401 sem corpo, e o `UseStatusCodePages()` o transforma em ProblemDetails.
- **Racional**: Princípios III e VII, sem middleware próprio. O CORS já aceita o header `Authorization` (`AllowAnyHeader`).

## R8. Erros como ProblemDetails (FR-017)

- **Decisão**: `AddProblemDetails()` + `UseExceptionHandler()` (500 sem stack trace fora de Development) + `UseStatusCodePages()` (404 de rota inexistente e 401/403 vazios viram ProblemDetails).
- **Racional**: sem `UseStatusCodePages`, um 404 de rota inexistente sai com corpo vazio, violando o Princípio III.
- **Alternativas**: middleware próprio (rejeitada: o framework já resolve).

## R9. CORS

- **Decisão**: policy nomeada `Frontend` com `WithOrigins("http://localhost:5200").AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Location")`, aplicada com `app.UseCors("Frontend")`.
- **Racional**: atende ao pedido (qualquer header e método só para essa origem) e ao FR-016. O navegador só deixa o JavaScript ler `Location` em chamadas entre origens se o cabeçalho for exposto. A origem fica fixa no código, como pedido; pode ser movida para configuração se surgir outro ambiente.
- **Alternativas**: `AllowAnyOrigin` (rejeitada: FR-015).

## R10. JSON

- **Decisão**: `ConfigureHttpJsonOptions` adicionando `JsonStringEnumConverter`; camelCase já é o padrão do Minimal API.
- **Racional**: pedido explícito. Não há enums hoje; o conversor só garante que enums futuros saiam como texto.

## R11. Migrations na inicialização

- **Decisão**: logo após `builder.Build()`, abrir `using var scope = app.Services.CreateScope()`, obter `AppDbContext` e chamar `db.Database.Migrate()`. A migration `InitialCreate` (com extensão `citext`, tabela `Consorcios`, tabelas do Identity, índice único e check constraints) é gerada com `dotnet ef migrations add InitialCreate` e versionada em `ConsorciosMariussi/Migrations/`.
- **Racional**: Princípio VI. O `depends_on: condition: service_healthy` do Compose garante que o banco está pronto antes da API, e `Migrate()` é idempotente.
- **Alternativas**: `EnsureCreated()` (rejeitada: não usa migrations).

## R12. Container

- **Decisão**: `api/Dockerfile` multi-stage com contexto `api/`:
  - build: `mcr.microsoft.com/dotnet/sdk:10.0`, `COPY ConsorciosMariussi/*.csproj` + `dotnet restore` (cache de camadas), `COPY . .`, `dotnet publish ConsorciosMariussi/ConsorciosMariussi.Api.csproj -c Release -o /app/publish /p:UseAppHost=false`;
  - final: `mcr.microsoft.com/dotnet/aspnet:10.0`, `COPY --from=build /app/publish .`, `EXPOSE 8080`, `ENTRYPOINT ["dotnet", "ConsorciosMariussi.Api.dll"]`.
  - `.dockerignore`: `**/bin`, `**/obj`, `.specify`, `.claude`, `specs`, `docs`, além de `.vs` e `.git`.
- **Racional**: a imagem `aspnet:10.0` já escuta em `8080` (`ASPNETCORE_HTTP_PORTS=8080`), como exige o Princípio IV. `.vs` e `.git` foram adicionados ao `.dockerignore` porque existem nesta pasta e só aumentam o contexto de build.

## R13. Connection string local

- **Decisão**: `ConnectionStrings:Default` em `appsettings.json` = `Host=localhost;Port=5433;Database=consorcioJoel;Username=postgres;Password=postgres`. No container, `ConnectionStrings__Default` do Compose a substitui.
- **Racional**: o `../docker-compose.yml` publica o banco no host em **5433** (`"5433:5432"`). Com essa porta, `dotnet run` fora do container conecta direto no banco do Compose. A porta foi trocada de 5432 para 5433 a pedido do usuário.

## R15. Organização dos endpoints

- **Decisão**: um arquivo `Endpoints/ConsorcioEndpoints.cs` com a extensão `MapConsorcioEndpoints(this IEndpointRouteBuilder app)` que cria `app.MapGroup("/api/consorcios").RequireAuthorization()` e mapeia os 5 endpoints como lambdas/métodos estáticos que recebem `AppDbContext` por injeção. A entrada é o record `Contracts/ConsorcioRequest.cs` (todos os campos exceto `id`, todos anuláveis); a saída é a própria entidade `Consorcio`, cujas propriedades batem 1:1 com o contrato. A cópia de request → entidade é um método estático escrito à mão (`ApplyRequest`).
- **Racional**: Minimal API + `DbContext` direto (Princípio I). Rotas com `{id:int}` fazem um id não numérico virar 404. `RequireAuthorization()` declara explicitamente que os endpoints exigem login (Princípio VII, FR-032).
- **Alternativas**: controllers (rejeitada: o pedido é Minimal API); DTO de resposta separado (rejeitada: seria idêntico à entidade); um arquivo por endpoint (rejeitada: 5 endpoints pequenos cabem em um arquivo legível).

## R16. Validação (400)

- **Decisão**: método estático `Validate(ConsorcioRequest r)` que monta um `Dictionary<string, string[]>` com as chaves do contrato: `administradora` (nula ou vazia após `Trim()`), `grupo` e `cota` (nulos, vazios, com caractere fora de `'0'..'9'` ou com mais de 20 caracteres) e `credito` (`< 0`). Havendo erros, retorna `TypedResults.ValidationProblem(errors)`. As mensagens são em português.
- **Racional**: o contrato fixa as chaves `administradora`, `grupo`, `cota` e `credito`. Validar à mão é mais simples que adicionar FluentValidation ou DataAnnotations com filtros. Comparar com `'0'..'9'` evita que `char.IsDigit` aceite dígitos Unicode, que o banco rejeitaria.
- **JSON inválido**: corpo malformado ou data fora de `yyyy-MM-dd` já gera 400 pelo próprio Minimal API (`BadHttpRequestException`), e o `AddProblemDetails` o formata como ProblemDetails.
- **Alternativas**: DataAnnotations (rejeitada: Minimal API não valida automaticamente no .NET 10 sem configuração extra, e as chaves precisam ser camelCase); FluentValidation (rejeitada: pacote proibido pela restrição "mínimo necessário").

## R17. Conflito (409)

- **Decisão**: não consultar antes de gravar. Chamar `SaveChangesAsync()` e capturar `DbUpdateException` cuja `InnerException` é `Npgsql.PostgresException` com `SqlState == PostgresErrorCodes.UniqueViolation` (`"23505"`), devolvendo `TypedResults.Problem(statusCode: 409, title: "Conflict", detail: "Já existe um consórcio com a mesma administradora, grupo e cota.")`.
- **Racional**: o índice único sobre `citext` já aplica a regra sem diferenciar maiúsculas/minúsculas. Capturar a violação resolve também a corrida entre dois pedidos simultâneos (edge case da spec), e um PUT que mantém a própria combinação não conflita, porque a linha é a mesma.
- **Alternativas**: `AnyAsync` antes de gravar (rejeitada: duplica a regra, tem corrida e a comparação `citext = text` vinda de parâmetro pode acabar diferenciando maiúsculas/minúsculas).

## R18. Valores monetários com 2 casas no JSON

- **Decisão**: em `ApplyRequest`, cada `decimal?` passa por `Round2(v) => v is null ? null : Math.Round(v.Value, 2, MidpointRounding.AwayFromZero) + 0.00m`.
- **Racional**: o contrato pede "número com 2 casas decimais". Somar `0.00m` força a escala do `decimal` para 2, então o System.Text.Json escreve `200000.00` (e não `200000`) na resposta 201 do POST, que vem da entidade em memória. Nas leituras, o Npgsql já devolve `numeric(18,2)` com escala 2. `AwayFromZero` repete o arredondamento do PostgreSQL.

## R19. Ordenação da listagem

- **Decisão**: `OrderByDescending(c => c.DataCompra.HasValue).ThenByDescending(c => c.DataCompra).ThenByDescending(c => c.Id)`.
- **Racional**: no PostgreSQL, `ORDER BY ... DESC` coloca `NULL` primeiro; o primeiro critério manda os consórcios sem data para o fim (assumption da spec), e o `Id` desempata de forma estável.

## R20. Respostas 201/204/404

- **Decisão**: `TypedResults.Created($"/api/consorcios/{c.Id}", c)`, `TypedResults.NoContent()` e `TypedResults.NotFound()`. O `UseStatusCodePages()` transforma o 404 sem corpo em ProblemDetails.
- **PUT**: validar → buscar com `FindAsync(id)` (404 se nulo) → `ApplyRequest` sobre a entidade (substitui todos os campos, ausentes viram `null`) → salvar (409 se violar a unicidade) → 204.

## R14. Testes

- **Decisão**: sem testes automatizados (pedido explícito). A validação é manual, seguindo o [quickstart.md](quickstart.md).
