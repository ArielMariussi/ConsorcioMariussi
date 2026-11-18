# Feature Specification: Registro de Consórcios (fundação da API)

**Feature Branch**: `001-registro-consorcios`

**Created**: 2026-11-16

**Status**: Draft

**Input**: User description: "a plataforma guarda registros de consorcios. O registro deve obedecer a todas as regras dentro do contrato-api.md. A API deve aceitar chamadas de um front-end web que roda em http://localhost:5200. A API deve poder ser executada com o comando docker compose up a partir da pasta raiz do app, junto com o banco de dados. Fora do escopo: endpoints. Eles vêm nas próximas features."

## Clarifications

### Session 2026-11-16

- Q: A unicidade de administradora diferencia maiúsculas de minúsculas? → A: Não. "Sicredi", "SICREDI" e "sicredi" são a mesma administradora para fins de unicidade.
- Q: Como é a autenticação? → A: Por e-mail e senha. A senha é simples: nenhum tipo de caractere é obrigatório (nem caractere especial, nem dígito, nem maiúscula), com no mínimo 3 e no máximo 30 caracteres.
- Q: O e-mail diferencia maiúsculas de minúsculas? → A: Não (decisão revista). Mantém-se o padrão do ASP.NET Core Identity: joel@exemplo.com e JOEL@exemplo.com são o mesmo e-mail.
- Q: Os endpoints de consórcio entram nesta feature? → A: Sim (revisão do escopo original). Os 5 endpoints de `docs/contrato-api.md` (listar, consultar, criar, editar e excluir) passam a fazer parte desta feature. Os endpoints de cadastro/login de usuários continuam fora, porque não estão no contrato.
- Q: Login entra no contrato e nesta feature? → A: Sim (contrato atualizado em 2026-11-16). Entram `POST /api/auth/register` (público) e `POST /api/auth/login` (público, devolve token JWT válido por 60 minutos).
- Q: Como os usuários são criados? → A: Por endpoint público de cadastro; qualquer pessoa cria a própria conta.
- Q: Quais endpoints de consórcio exigem login? → A: Todos (GET, POST, PUT e DELETE); sem token válido → 401.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Subir a plataforma com um único comando (Priority: P1)

Como desenvolvedor ou aluno, eu clono o repositório, executo `docker compose up` na pasta raiz do app e a API sobe junto com o banco de dados, já com a estrutura de armazenamento de consórcios criada, sem nenhum passo manual.

**Why this priority**: Sem o ambiente funcionando, nenhuma outra parte da plataforma (nem os endpoints, nem o front-end) pode ser usada ou testada. É a base de tudo.

**Independent Test**: Em uma máquina limpa (sem banco pré-existente), executar `docker compose up` na pasta raiz do app e verificar que a API responde no endereço base do contrato e que o armazenamento de consórcios existe no banco.

**Acceptance Scenarios**:

1. **Given** uma máquina limpa com o repositório clonado e sem dados prévios, **When** o desenvolvedor executa `docker compose up` na pasta raiz do app, **Then** o banco e a API sobem e a API passa a responder em `http://localhost:5080`, sem nenhum comando adicional.
2. **Given** a API subiu pela primeira vez, **When** o banco é inspecionado, **Then** a estrutura para guardar registros de consórcio existe com todos os campos do contrato.
3. **Given** a plataforma já foi iniciada antes e contém registros de consórcio, **When** ela é parada e iniciada novamente com `docker compose up`, **Then** os registros existentes continuam lá, inalterados, e a inicialização não falha nem duplica estruturas.
4. **Given** o banco ainda está inicializando, **When** a API é iniciada, **Then** a API só fica disponível depois que o banco estiver pronto, sem falhar por indisponibilidade temporária do banco.

---

### User Story 2 - Registros de consórcio sempre válidos (Priority: P2)

Como dono da plataforma, eu quero que todo registro de consórcio guardado obedeça às regras do contrato (campos obrigatórios, formatos, unicidade), para que os dados sejam confiáveis independentemente de qual endpoint futuro os grave.

**Why this priority**: A integridade dos dados é o valor central de "guardar registros". Os endpoints (US5) dependem de um armazenamento que já garanta as regras.

**Independent Test**: Tentar gravar diretamente no armazenamento registros válidos e inválidos (ex.: via teste automatizado contra o banco) e verificar que os válidos são aceitos com os valores preservados e os inválidos são rejeitados.

**Acceptance Scenarios**:

1. **Given** um registro com administradora "Sicredi", grupo "05014", cota "0361" e demais campos opcionais vazios, **When** ele é gravado, **Then** ele é aceito, recebe um identificador numérico e, ao ser lido, grupo e cota mantêm os zeros à esquerda ("05014", "0361").
2. **Given** um registro com administradora "  Sicredi  ", **When** ele é gravado, **Then** a administradora fica armazenada como "Sicredi" (sem espaços nas pontas).
3. **Given** já existe um consórcio Sicredi / 05014 / 0361, **When** outro registro com a mesma administradora, grupo e cota é gravado, **Then** a gravação é rejeitada por violar a unicidade.
3a. **Given** já existe um consórcio Sicredi / 05014 / 0361, **When** um registro SICREDI / 05014 / 0361 (ou "sicredi") é gravado, **Then** a gravação é rejeitada por violar a unicidade, pois a comparação de administradora ignora maiúsculas e minúsculas.
4. **Given** um registro sem administradora, sem grupo ou sem cota, **When** ele é gravado, **Then** a gravação é rejeitada.
5. **Given** um registro com grupo ou cota contendo caracteres que não são dígitos (ex.: "05A14", "03 61"), **When** ele é gravado, **Then** a gravação é rejeitada.
6. **Given** um registro com crédito negativo, **When** ele é gravado, **Then** a gravação é rejeitada; com crédito ausente ou zero, é aceita.
7. **Given** um registro com valores monetários (ex.: crédito 200000.00, valor pago 30000.00) e datas (ex.: data da compra 2026-07-07), **When** ele é gravado e lido de volta, **Then** os valores monetários voltam com exatamente 2 casas decimais e as datas voltam como datas de calendário, sem hora nem deslocamento de fuso.

---

### User Story 3 - Front-end web consegue chamar a API (Priority: P3)

Como front-end web rodando em `http://localhost:5200`, eu preciso que o navegador permita minhas chamadas à API em `http://localhost:5080`, para que as telas funcionem assim que os endpoints existirem.

**Why this priority**: É pré-requisito para a integração com o front-end, mas só gera valor visível junto com os endpoints (US5).

**Independent Test**: A partir de um navegador (ou requisição simulando um navegador) com origem `http://localhost:5200`, enviar uma requisição de verificação prévia (preflight) à API e verificar que ela é autorizada; repetir com outra origem e verificar que não é autorizada.

**Acceptance Scenarios**:

1. **Given** a API está em execução, **When** o navegador com origem `http://localhost:5200` faz uma verificação prévia para os métodos GET, POST, PUT ou DELETE com corpo JSON, **Then** a API autoriza a chamada.
2. **Given** a API está em execução, **When** uma requisição vem de uma origem diferente de `http://localhost:5200` (ex.: `http://localhost:3000`), **Then** a API não autoriza a chamada entre origens.
3. **Given** uma resposta da API contém o cabeçalho `Location`, **When** ela é recebida pelo front-end em `http://localhost:5200`, **Then** o front-end consegue ler esse cabeçalho.

---

### User Story 4 - Cadastro e login com e-mail e senha (Priority: P4)

Como pessoa usando o front-end, eu quero criar minha conta com e-mail e senha e entrar na plataforma, recebendo um token que me dá acesso aos consórcios.

**Why this priority**: Sem login, nenhum endpoint de consórcio (US5) pode ser usado, porque todos são protegidos.

**Independent Test**: Seção "US4" do [quickstart](quickstart.md): cadastrar pelo `POST /api/auth/register`, fazer login pelo `POST /api/auth/login` e conferir status e corpo de cada resposta, inclusive os casos inválidos.

**Acceptance Scenarios**:

1. **Given** nenhum usuário joel@exemplo.com, **When** é feito o cadastro com esse e-mail e a senha "abc", **Then** a resposta é 204 (senha de 3 caracteres, sem exigência de dígito, maiúscula ou caractere especial).
2. **Given** um e-mail novo e uma senha de 30 caracteres, **When** é feito o cadastro, **Then** a resposta é 204.
3. **Given** uma senha com 2 ou com 31 caracteres, **When** é feito o cadastro, **Then** a resposta é 400 com erro na chave `password`.
4. **Given** já existe joel@exemplo.com, **When** é feito o cadastro com JOEL@exemplo.com, **Then** a resposta é 409.
5. **Given** um e-mail ausente ou em formato inválido (ex.: "joel"), **When** é feito o cadastro, **Then** a resposta é 400 com erro na chave `email`.
6. **Given** o usuário joel@exemplo.com com senha "abc", **When** é feito login com JOEL@exemplo.com e "abc", **Then** a resposta é 200 com `accessToken` e `expiresIn: 3600`.
7. **Given** o usuário joel@exemplo.com, **When** é feito login com a senha errada, ou com um e-mail que não existe, **Then** a resposta é 401 com ProblemDetails, idêntica nos dois casos.
8. **Given** um login com e-mail ou senha vazios, **When** ele é enviado, **Then** a resposta é 400 com erro nas chaves `email`/`password`.
9. **Given** um usuário cadastrado, **When** o armazenamento é inspecionado ou qualquer resposta é lida, **Then** a senha não aparece em texto puro em nenhum lugar.

---

### User Story 5 - Gerenciar consórcios pela API (Priority: P5)

Como usuário do front-end, eu quero cadastrar, listar, consultar, editar e excluir consórcios pela API, exatamente como descrito em `docs/contrato-api.md`, para gerenciar minha carteira de cotas pela tela.

**Why this priority**: É o que torna a plataforma utilizável pelo front-end. Depende do armazenamento (US1/US2), do CORS (US3) e do login (US4) já prontos.

**Independent Test**: Com a API no ar e um token obtido no login, executar a sequência POST → GET lista → GET por id → PUT → DELETE do [quickstart](quickstart.md) e conferir status, corpo e cabeçalhos de cada resposta contra o contrato.

**Acceptance Scenarios** (todos com um token válido no header `Authorization`, exceto o 0):

0. **Given** nenhum token, um token inválido ou um token expirado, **When** qualquer um dos 5 endpoints de consórcio é chamado, **Then** a resposta é 401 com ProblemDetails e nada é lido nem gravado.

1. **Given** nenhum consórcio Sicredi / 05014 / 0361, **When** o front-end faz POST com administradora "  Sicredi  ", grupo "05014", cota "0361", crédito 200000.00 e data da compra "2026-07-07", **Then** a resposta é 201 com o consórcio criado no corpo (com `id`, administradora "Sicredi", grupo "05014", crédito `200000.00`) e o cabeçalho `Location: /api/consorcios/{id}`.
2. **Given** um POST com administradora vazia, grupo "05A14", cota ausente e crédito -1, **When** ele é enviado, **Then** a resposta é 400 com ValidationProblemDetails contendo erros nas chaves `administradora`, `grupo`, `cota` e `credito`, e nada é gravado.
3. **Given** já existe Sicredi / 05014 / 0361, **When** é feito POST com SICREDI / 05014 / 0361, **Then** a resposta é 409 Conflict com ProblemDetails.
4. **Given** existem consórcios com datas de compra 2026-01-10, 2026-07-07 e sem data, **When** é feito GET `/api/consorcios`, **Then** a resposta é 200 com a lista na ordem 2026-07-07, 2026-01-10 e, por último, o sem data.
5. **Given** um consórcio de id 1, **When** é feito GET `/api/consorcios/1`, **Then** a resposta é 200 com o consórcio; **When** é feito GET `/api/consorcios/999` (inexistente), **Then** a resposta é 404 com ProblemDetails.
6. **Given** um consórcio de id 1 com valor pago 30000.00, **When** é feito PUT `/api/consorcios/1` com todos os campos e sem `valorPago`, **Then** a resposta é 204 e o consórcio passa a ter exatamente os novos valores (valor pago vazio).
7. **Given** um consórcio de id 1, **When** é feito PUT mantendo a mesma administradora + grupo + cota dele, **Then** a resposta é 204 (não conflita consigo mesmo); **When** o PUT usa a combinação de outro consórcio, **Then** a resposta é 409; **When** o PUT é para um id inexistente com corpo válido, **Then** a resposta é 404.
8. **Given** um consórcio de id 1, **When** é feito DELETE `/api/consorcios/1`, **Then** a resposta é 204; **When** o mesmo DELETE é repetido, **Then** a resposta é 404.

---

### Edge Cases

- Administradora composta apenas de espaços: após remover os espaços das pontas fica vazia, portanto é tratada como ausente e rejeitada.
- Unicidade é avaliada após remover os espaços das pontas da administradora e sem diferenciar maiúsculas de minúsculas: "Sicredi", " Sicredi " e "SICREDI" são a mesma administradora para fins de unicidade. A grafia gravada é preservada como informada (apenas sem espaços nas pontas).
- Senha com espaços, acentos ou caracteres especiais: é aceita, desde que tenha entre 3 e 30 caracteres; caracteres especiais são permitidos, só não são obrigatórios.
- Grupo/cota "0" ou "0000" são válidos (só dígitos); grupo/cota vazios são rejeitados.
- Valores monetários com mais de 2 casas decimais: são armazenados com 2 casas decimais (ver Assumptions).
- Campos opcionais (valor pago, comissão, previsão de lucro, fundo de reserva, taxa de transferência, lucro e todas as datas) aceitam ausência de valor e a preservam como ausência (não viram zero nem data padrão).
- Primeira subida com o volume de dados vazio versus subidas seguintes com dados existentes: ambas devem funcionar sem intervenção.
- Requisição a uma rota que não está no contrato (ex.: `GET /`): a API responde "não encontrado" no formato padronizado de erro, sem expor detalhes internos.
- Id não numérico na rota (ex.: `/api/consorcios/abc`): tratado como rota inexistente, 404.
- Corpo JSON malformado ou data em formato diferente de `yyyy-MM-dd` (ex.: "07/07/2026"): 400 com ProblemDetails, nada é gravado.
- PUT com corpo inválido para um id inexistente: a validação vem primeiro e a resposta é 400.
- Dois POSTs simultâneos com a mesma administradora + grupo + cota: só um é gravado; o outro recebe 409.

## Requirements *(mandatory)*

### Functional Requirements

**Registro de consórcio**

- **FR-001**: A plataforma MUST guardar registros de consórcio com exatamente os campos definidos em `docs/contrato-api.md`: id, administradora, grupo, cota, credito, valorPago, dataCompra, vencimento, assembleia, comissao, previsaoLucro, fundoReserva, contemplacao, taxaTransferencia, dataLucro e lucro.
- **FR-002**: Cada registro MUST receber um identificador numérico inteiro único, gerado pela plataforma.
- **FR-003**: Administradora MUST ser obrigatória e MUST ser armazenada sem espaços nas pontas; um valor vazio após essa remoção MUST ser rejeitado.
- **FR-004**: Grupo e cota MUST ser obrigatórios, MUST conter somente dígitos (0–9) e MUST ser armazenados como texto, preservando zeros à esquerda.
- **FR-005**: Crédito MUST ser opcional e, quando informado, MUST NOT ser negativo.
- **FR-006**: Valor pago, comissão, previsão de lucro, fundo de reserva, taxa de transferência e lucro MUST ser opcionais e armazenados como valores monetários com 2 casas decimais.
- **FR-007**: Data da compra, vencimento, assembleia, contemplação e data do lucro MUST ser opcionais e armazenadas como datas de calendário (dia, mês, ano), sem componente de hora ou fuso.
- **FR-008**: A combinação administradora + grupo + cota MUST ser única entre todos os registros, com a administradora comparada sem diferenciar maiúsculas de minúsculas (e após remover os espaços das pontas); uma gravação que viole essa regra MUST ser rejeitada. A administradora MUST ser armazenada com a grafia informada.
- **FR-009**: As regras FR-003 a FR-008 MUST ser garantidas pelo próprio armazenamento, de modo que nenhum registro inválido possa ser persistido, mesmo por um caminho de gravação futuro que esqueça de validar.

**Execução**

- **FR-010**: A API e o banco de dados MUST subir juntos com um único `docker compose up` executado na pasta raiz do app, usando o arquivo de composição já existente, sem alterá-lo.
- **FR-011**: Na inicialização, a API MUST criar ou atualizar automaticamente a estrutura de armazenamento dos consórcios, sem passos manuais, e MUST ser segura para repetir a cada inicialização (idempotente).
- **FR-012**: Os registros MUST persistir entre reinicializações da plataforma.
- **FR-013**: A API MUST obter os dados de conexão com o banco a partir da configuração do ambiente, sem valores fixados no código.

**Acesso pelo front-end**

- **FR-014**: A API MUST permitir chamadas de navegador vindas da origem `http://localhost:5200`, para os métodos GET, POST, PUT e DELETE, com corpo JSON.
- **FR-015**: A API MUST NOT permitir chamadas de navegador vindas de outras origens.
- **FR-016**: A API MUST permitir que o front-end em `http://localhost:5200` leia o cabeçalho `Location` das respostas.

**Usuários e autenticação (base)**

- **FR-019**: A plataforma MUST guardar usuários identificados por e-mail e senha.
- **FR-020**: O e-mail MUST ser obrigatório, em formato válido e único entre usuários, sem diferenciar maiúsculas de minúsculas.
- **FR-021**: A senha MUST ter no mínimo 3 e no máximo 30 caracteres, e MUST NOT exigir nenhum tipo de caractere específico (caractere especial, dígito, maiúscula ou minúscula); qualquer caractere é permitido.
- **FR-022**: A senha MUST NOT ser armazenada em texto puro, registrada em log ou devolvida em qualquer resposta.
- **FR-023**: A autenticação MUST ser feita pedindo e-mail e senha, conforme `docs/contrato-api.md`.
- **FR-033**: `POST /api/auth/register` MUST ser público, receber `{ email, password }` e criar o usuário, retornando 204; 400 com ValidationProblemDetails (chaves `email`, `password`) se violar FR-020/FR-021; 409 se o e-mail já existir (sem diferenciar maiúsculas/minúsculas).
- **FR-034**: `POST /api/auth/login` MUST ser público, receber `{ email, password }` e, com credenciais corretas, retornar 200 com `{ accessToken, expiresIn }`, onde `expiresIn` = 3600 (segundos); 400 (chaves `email`, `password`) se algum estiver vazio; 401 com ProblemDetails se o e-mail não existir ou a senha estiver errada, com a mesma resposta nos dois casos.
- **FR-035**: O token emitido MUST valer 60 minutos. Não há refresh token nem logout.

**Comportamento geral**

- **FR-017**: Toda resposta de erro da API MUST seguir o formato padronizado de detalhes de problema (ProblemDetails), incluindo "não encontrado" para rotas inexistentes e erros inesperados, sem expor detalhes internos fora do ambiente de desenvolvimento.
- **FR-018**: A API MUST expor exatamente os 7 endpoints de `docs/contrato-api.md` (2 de autenticação e 5 de consórcio) e nenhum outro.

**Endpoints de consórcio** (todos conforme `docs/contrato-api.md`)

- **FR-024**: `GET /api/consorcios` MUST retornar 200 com todos os consórcios, ordenados pela data da compra do mais recente para o mais antigo; consórcios sem data da compra vêm por último.
- **FR-025**: `GET /api/consorcios/{id}` MUST retornar 200 com o consórcio, ou 404 se ele não existir.
- **FR-026**: `POST /api/consorcios` MUST receber todos os campos do consórcio exceto `id` e, se válido e único, gravar e retornar 201 com o consórcio criado no corpo e o cabeçalho `Location: /api/consorcios/{id}`.
- **FR-027**: `PUT /api/consorcios/{id}` MUST receber o mesmo corpo do POST, substituir todos os campos (campos ausentes ficam vazios) e retornar 204; 404 se o consórcio não existir.
- **FR-028**: `DELETE /api/consorcios/{id}` MUST excluir o consórcio e retornar 204, ou 404 se ele não existir.
- **FR-029**: POST e PUT MUST validar as regras de FR-003 a FR-005 e, se houver violação, retornar 400 com ValidationProblemDetails cujas chaves de erro são `administradora`, `grupo`, `cota` e `credito` (só as que tiverem erro), sem gravar nada.
- **FR-030**: POST e PUT MUST retornar 409 Conflict com ProblemDetails quando a combinação administradora + grupo + cota já pertencer a outro consórcio (comparação conforme FR-008), inclusive quando dois pedidos simultâneos competirem pela mesma combinação.
- **FR-031**: O JSON de entrada e saída MUST usar propriedades em camelCase, datas `yyyy-MM-dd`, valores monetários como número com 2 casas decimais, e grupo/cota como texto.
- **FR-032**: Os 5 endpoints de consórcio MUST exigir um token válido no header `Authorization: Bearer {accessToken}`; sem token, com token inválido ou expirado, a resposta MUST ser 401 com ProblemDetails.

### Key Entities

- **Consorcio**: uma cota de consórcio adquirida e acompanhada pela plataforma. Identificada por um id numérico e, de forma única no negócio, pela combinação administradora + grupo + cota. Guarda o valor do crédito, quanto foi pago, as datas relevantes do ciclo (compra, vencimento, assembleia, contemplação, lucro) e os valores financeiros associados (comissão, previsão de lucro, fundo de reserva, taxa de transferência, lucro).
- **Token de acesso**: credencial temporária (60 minutos) emitida no login, que identifica o usuário nas chamadas aos endpoints de consórcio.
- **Usuário**: pessoa que acessa a plataforma. Identificado de forma única pelo e-mail (sem diferenciar maiúsculas de minúsculas) e autenticado por senha, guardada de forma não legível.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em uma máquina limpa, a plataforma fica pronta para uso com 1 único comando (`docker compose up`) e 0 passos manuais adicionais.
- **SC-002**: Após a primeira inicialização, a API responde no endereço base do contrato em até 60 segundos depois que o banco estiver pronto.
- **SC-003**: 100% dos casos de teste de registros inválidos (administradora vazia/só espaços, grupo/cota ausentes ou com não-dígitos, crédito negativo, duplicidade administradora+grupo+cota, inclusive com administradora em grafia diferente de maiúsculas/minúsculas) são rejeitados pelo armazenamento.
- **SC-008**: 100% dos cenários de aceitação da US5 retornam exatamente o status, o corpo e os cabeçalhos definidos em `docs/contrato-api.md`.
- **SC-009**: Pelo front-end, um usuário novo consegue criar a conta, entrar, cadastrar um consórcio e vê-lo na lista em menos de 2 minutos.
- **SC-010**: 100% das chamadas aos endpoints de consórcio sem token válido recebem 401; 100% das tentativas de login com credenciais erradas recebem a mesma resposta 401, seja o e-mail inexistente ou a senha errada.
- **SC-007**: 100% das senhas de 3 a 30 caracteres sem caracteres especiais são aceitas; 100% das senhas com menos de 3 ou mais de 30 caracteres são rejeitadas; 0 senhas aparecem em texto puro no armazenamento.
- **SC-004**: 100% dos registros válidos gravados e lidos de volta preservam exatamente os valores do contrato: zeros à esquerda em grupo/cota, 2 casas decimais nos valores monetários, datas sem deslocamento de dia.
- **SC-005**: 100% dos registros existentes permanecem intactos após 3 reinicializações consecutivas da plataforma.
- **SC-006**: Chamadas de navegador a partir de `http://localhost:5200` são autorizadas em 100% dos testes; chamadas a partir de qualquer outra origem testada são recusadas em 100% dos testes.

## Assumptions

- O arquivo de composição usado é o `docker-compose.yml` já existente na pasta raiz do app (pasta acima de `api/`), que define os serviços `db`, `api` e `web`, expõe a API em `http://localhost:5080` e fornece a conexão com o banco por variável de ambiente; ele não será alterado.
- Valores monetários com mais de 2 casas decimais são arredondados para 2 casas ao serem armazenados, conforme o contrato ("número com 2 casas decimais").
- Os campos opcionais não têm regras adicionais além das do contrato (ex.: valores pagos ou de lucro negativos são aceitos, pois o contrato só restringe o crédito).
- Não há limite de tamanho definido no contrato para administradora, grupo e cota; o plano técnico definirá limites razoáveis que não restrinjam valores reais.
- Não há papéis (perfis) de usuário definidos; todos os usuários autenticados têm o mesmo nível de acesso e veem todos os consórcios (os consórcios não pertencem a um usuário).
- Não há regra de bloqueio por tentativas erradas nem de confirmação de e-mail nesta feature.
- O cadastro não faz login automático: depois do 204, o front-end chama o login.
- Na listagem, empates de data da compra são desempatados pelo id, do maior para o menor.
- Em PUT, a validação do corpo (400) é feita antes da verificação de existência (404).
- A origem do front-end é exatamente `http://localhost:5200`; outras origens (incluindo `http://127.0.0.1:5200`) não são autorizadas nesta feature.
