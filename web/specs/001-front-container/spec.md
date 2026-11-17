# Feature Specification: Front publicado em container

**Feature Branch**: `001-front-container`

**Created**: 2026-11-17

**Status**: Draft

**Input**: User description: "O front deve poder ser executado com o comando docker compose up a
partir da pasta raiz do app e ficar disponível em http://localhost:5200. Fora do escopo: dados,
chamadas de API, formulários e tabelas." (escopo ampliado na sessão de clarificação de
2026-11-17: dados, formulários e chamadas à API passaram a fazer parte da feature)

## Clarifications

### Session 2026-11-17

- Q: Quais páginas o container deve servir e o que fica no escopo? → A: Dados, formulários e
  chamadas à API passam a fazer parte do escopo. O front serve as páginas de Cadastro, Login e
  a página principal, com dados reais vindos da API e formulários que enviam para a API,
  conforme `docs/contrato-api.md`. Tabelas não se aplicam: a referência usa cartões.
- Q: Se a pessoa fechar o navegador e abrir de novo antes de 60 minutos, continua logada? →
  A: Não. A sessão vale só enquanto a aba estiver aberta; ao fechar, precisa entrar de novo.
- Q: Os formulários conferem os campos na tela antes de enviar, ou só mostram os erros da API? →
  A: Validação completa na tela, com mensagens próprias em português por campo, seguindo as
  regras do contrato; os erros devolvidos pela API continuam sendo exibidos.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Subir o front com um único comando (Priority: P1)

Uma pessoa que acabou de baixar o projeto executa `docker compose up` na pasta raiz do app (a que
contém o arquivo de orquestração com os serviços `db`, `api` e `web`). Ao final da
inicialização, abre `http://localhost:5200` no navegador e vê o sistema ConsórcioMariussi.

**Why this priority**: sem o front rodando no ambiente padrão, nenhuma outra história pode ser
demonstrada ou testada.

**Independent Test**: executar `docker compose up` na pasta raiz do app e acessar
`http://localhost:5200`; uma página do sistema deve abrir (a de Login, já que ninguém está
autenticado).

**Acceptance Scenarios**:

1. **Given** o projeto baixado e o Docker em execução, **When** a pessoa executa
   `docker compose up` na pasta raiz do app, **Then** o serviço do front sobe sem erros.
2. **Given** o serviço do front em execução e nenhuma sessão ativa, **When** a pessoa acessa
   `http://localhost:5200`, **Then** é levada à página de Login.

---

### User Story 2 - Criar conta e entrar (Priority: P2)

Uma pessoa sem conta abre a página de Cadastro, informa e-mail e senha e cria a conta. Em
seguida é levada à página de Login, informa as mesmas credenciais e entra no sistema, chegando à
página principal. Pelo botão "Logout" ela sai e volta para a página de Login.

**Why this priority**: todas as informações de consórcios são protegidas; sem cadastro e login
nada mais pode ser usado.

**Independent Test**: criar uma conta nova pela página de Cadastro, entrar pela página de Login,
verificar que a página principal abre e que "Logout" leva de volta ao Login.

**Acceptance Scenarios**:

1. **Given** a página de Cadastro, **When** a pessoa informa um e-mail válido ainda não usado e
   uma senha de 3 a 30 caracteres, **Then** a conta é criada, uma mensagem de sucesso é exibida e
   ela é levada à página de Login.
2. **Given** a página de Cadastro, **When** a pessoa informa um e-mail já cadastrado, **Then** a
   mensagem "Já existe uma conta com esse e-mail." é exibida e ela continua no Cadastro.
3. **Given** a página de Cadastro, **When** a pessoa informa um e-mail inválido ou uma senha fora
   do limite de 3 a 30 caracteres, **Then** nada é enviado e a mensagem própria do campo
   (FR-018a) aparece junto a ele.
4. **Given** a página de Login, **When** a pessoa informa e-mail e senha corretos, **Then** ela é
   levada à página principal.
5. **Given** a página de Login, **When** o e-mail não existe ou a senha está errada, **Then** a
   mensagem "E-mail ou senha inválidos." é exibida.
6. **Given** uma pessoa autenticada na página principal, **When** clica em "Logout", **Then** a
   sessão é encerrada e ela é levada à página de Login.
7. **Given** uma pessoa não autenticada, **When** tenta abrir a página principal diretamente,
   **Then** é levada à página de Login.

---

### User Story 3 - Ver meus consórcios e o resumo (Priority: P3)

Na página principal, a pessoa autenticada vê o resumo (quantidade de consórcios, total
investido, previsão de lucro e total de lucro) e a lista de consórcios em cartões, como na
referência. Ela pode buscar por texto e filtrar por situação.

**Why this priority**: é o principal valor do sistema: acompanhar os consórcios.

**Independent Test**: com consórcios já cadastrados na API, entrar no sistema e conferir que os
cartões e o resumo mostram exatamente os dados da API.

**Acceptance Scenarios**:

1. **Given** uma pessoa autenticada, **When** a página principal abre, **Then** cada consórcio
   retornado pela API aparece em um cartão, na ordem recebida (compras mais recentes primeiro).
2. **Given** a lista carregada, **When** a pessoa olha o resumo, **Then** "Consórcios" mostra a
   quantidade, "Total investido" a soma dos valores pagos, "Previsão de lucro" a soma das
   previsões de lucro e "Total de lucro" a soma dos lucros (valores vazios contam como zero).
3. **Given** um consórcio com data de contemplação preenchida, **When** seu cartão é exibido,
   **Then** aparece a etiqueta "Contemplado"; sem essa data, aparece "Em andamento".
4. **Given** a lista carregada, **When** a pessoa digita na busca, **Then** só ficam visíveis os
   cartões cuja administradora, grupo ou cota contenham o texto (sem diferenciar maiúsculas).
5. **Given** a lista carregada, **When** a pessoa escolhe "Contemplados" ou "Em andamento" no
   filtro de situação, **Then** só ficam visíveis os cartões dessa situação; "Todos" mostra todos.
6. **Given** nenhum consórcio cadastrado, **When** a página principal abre, **Then** o resumo
   mostra zeros e uma mensagem informa que ainda não há consórcios.

---

### User Story 4 - Cadastrar um novo consórcio (Priority: P4)

A pessoa clica em "+ Novo consórcio", preenche o formulário (como o modal da referência) e
salva. O novo consórcio aparece na lista e o resumo é atualizado.

**Why this priority**: sem cadastro de consórcios a lista fica vazia em um ambiente novo.

**Independent Test**: criar um consórcio pelo formulário e verificar que ele aparece na lista e
nos totais.

**Acceptance Scenarios**:

1. **Given** o formulário de novo consórcio, **When** a pessoa preenche administradora, grupo e
   cota válidos e salva, **Then** o modal fecha, o consórcio aparece na lista e o resumo é
   recalculado.
2. **Given** o formulário, **When** já existe consórcio com a mesma administradora, grupo e cota,
   **Then** a mensagem "Já existe um consórcio dessa administradora com esse grupo e cota."
   aparece no topo do formulário e os dados digitados são mantidos.
3. **Given** o formulário, **When** algum campo viola as regras (administradora vazia, grupo
   ou cota com caracteres que não são dígitos, crédito negativo), **Then** nada é enviado, a
   mensagem própria do campo (FR-018a) aparece junto a ele e os dados digitados são mantidos.
4. **Given** o formulário válido na tela, **When** a API mesmo assim recusa algum campo,
   **Then** a mensagem devolvida pela API aparece junto ao campo correspondente.
5. **Given** o formulário aberto, **When** a pessoa clica em "Cancelar" ou fecha o modal,
   **Then** nada é salvo.

---

### User Story 5 - Editar um consórcio (Priority: P5)

A pessoa abre um cartão, clica em "Editar consórcio", vê o formulário preenchido com os dados
atuais, altera e salva.

**Why this priority**: os dados de um consórcio mudam ao longo do tempo (assembleia,
contemplação, lucro).

**Independent Test**: editar um campo de um consórcio existente e conferir que o cartão e o
resumo mostram o novo valor.

**Acceptance Scenarios**:

1. **Given** um consórcio na lista, **When** a pessoa clica em "Editar consórcio", **Then** o
   formulário abre preenchido com todos os dados atuais desse consórcio.
2. **Given** o formulário de edição, **When** a pessoa altera dados válidos e salva, **Then** o
   modal fecha e o cartão e o resumo exibem os novos valores.
3. **Given** o formulário de edição, **When** a combinação administradora + grupo + cota já
   pertence a outro consórcio, **Then** a mesma mensagem de duplicidade da criação é exibida.
4. **Given** o formulário de edição, **When** o consórcio foi excluído por outra sessão, **Then**
   a mensagem "Este consórcio não existe mais." é exibida e a lista é recarregada.

---

### User Story 6 - Excluir um consórcio (Priority: P6)

A pessoa clica em "Excluir" em um cartão, confirma na janela de confirmação (com administradora,
grupo e cota) e o consórcio sai da lista.

**Why this priority**: necessário para corrigir cadastros errados, mas menos frequente.

**Independent Test**: excluir um consórcio e verificar que ele sumiu da lista e dos totais.

**Acceptance Scenarios**:

1. **Given** um consórcio na lista, **When** a pessoa clica em "Excluir", **Then** a confirmação
   mostra administradora, grupo e cota desse consórcio.
2. **Given** a confirmação aberta, **When** a pessoa clica em "Sim, excluir", **Then** o
   consórcio sai da lista e o resumo é recalculado.
3. **Given** a confirmação aberta, **When** a pessoa clica em "Cancelar", **Then** nada é
   excluído.
4. **Given** a confirmação aberta, **When** o consórcio já tinha sido excluído por outra sessão,
   **Then** a mensagem "Este consórcio não existe mais." é exibida e a lista é recarregada.

---

### Edge Cases

- A porta 5200 já está ocupada: o `docker compose up` informa o conflito e o front não sobe.
- A API está fora do ar ou não responde: a página exibe "Não foi possível conectar ao servidor.
  Tente novamente." sem quebrar a tela; a pessoa pode tentar de novo.
- A sessão expira (60 minutos após o login) ou o token é recusado em qualquer operação: a pessoa
  é levada à página de Login com a mensagem "Sua sessão expirou. Entre novamente."
- Uma pessoa já autenticada abre a página de Login ou de Cadastro: é levada à página principal.
- A pessoa fecha a aba e abre o sistema de novo: precisa entrar novamente, mesmo que o token
  ainda não tenha expirado.
- Um valor opcional vem vazio da API: o cartão mostra "—" no lugar do valor.
- Busca e filtro não encontram nenhum cartão: aparece a mensagem "Nenhum consórcio encontrado."
- A pessoa clica duas vezes seguidas em salvar: apenas um envio é feito (o botão fica
  desabilitado enquanto o envio está em andamento).
- Grupo e cota com zeros à esquerda (ex.: "05014"): são exibidos e enviados sem perder os zeros.
- Endereço inexistente no front (ex.: `/qualquer-coisa`): o servidor responde "não encontrado".

## Requirements *(mandatory)*

### Functional Requirements

**Execução**

- **FR-001**: O front MUST poder ser iniciado com `docker compose up` na pasta raiz do app, pelo
  serviço `web` já definido no arquivo de orquestração, sem alterá-lo.
- **FR-002**: O front MUST ficar acessível em `http://localhost:5200`.
- **FR-003**: O endereço da API usado pelo navegador (`http://localhost:5080` no ambiente padrão)
  MUST vir de um arquivo de configuração, nunca fixo no código das páginas.

**Páginas e layout**

- **FR-004**: O front MUST ter três páginas: Cadastro (baseada em `docs/layout/cadastro.html`),
  Login (baseada em `docs/layout/login.html`) e principal (baseada em `docs/layout/index.html`),
  reutilizando as classes de estilo das referências em um único arquivo de estilos.
- **FR-005**: Todos os textos visíveis MUST estar em português do Brasil.
- **FR-006**: Valores monetários MUST ser exibidos no formato "R$ 1.234,56" e datas no formato
  "dd/mm/aaaa"; valores vazios MUST aparecer como "—".

**Autenticação**

- **FR-007**: A página de Cadastro MUST criar a conta com e-mail e senha conforme as regras do
  contrato (e-mail válido e único; senha de 3 a 30 caracteres) e, em caso de sucesso, levar a
  pessoa à página de Login.
- **FR-008**: A página de Login MUST trocar e-mail e senha por um token de acesso e levar a
  pessoa à página principal.
- **FR-009**: Todas as operações com consórcios MUST enviar o token de acesso conforme o
  contrato.
- **FR-010**: Quando não houver token, ou a API recusar o token, o front MUST levar a pessoa à
  página de Login.
- **FR-011**: O botão "Logout" MUST descartar o token e levar a pessoa à página de Login.
- **FR-011a**: O token MUST valer apenas enquanto a aba do navegador estiver aberta; ao fechar a
  aba ou o navegador, a pessoa MUST precisar entrar novamente, mesmo antes dos 60 minutos.

**Consórcios**

- **FR-012**: A página principal MUST listar todos os consórcios retornados pela API, na ordem
  recebida, em cartões com etiqueta de situação, administradora, grupo, cota, crédito, valor
  pago, próxima assembleia e os demais campos no corpo do cartão.
- **FR-013**: O resumo MUST mostrar quantidade, soma de valor pago, soma de previsão de lucro e
  soma de lucro dos consórcios carregados, tratando vazios como zero.
- **FR-014**: A busca e o filtro de situação MUST atuar sobre a lista já carregada, sem nova
  consulta à API.
- **FR-015**: O formulário de novo consórcio MUST enviar todos os campos do contrato
  (administradora, grupo e cota obrigatórios; demais opcionais, enviados como vazios quando não
  preenchidos).
- **FR-016**: O formulário de edição MUST abrir com os dados atuais do consórcio e substituir
  todos os campos ao salvar.
- **FR-017**: A exclusão MUST exigir confirmação mostrando administradora, grupo e cota.
- **FR-018**: Após criar, editar ou excluir com sucesso, a lista e o resumo MUST ser atualizados
  sem que a pessoa precise recarregar a página.

**Erros**

- **FR-018a**: Antes de enviar, cada formulário MUST validar na tela todas as regras do contrato
  e, se houver erro, MUST NOT enviar e MUST exibir junto a cada campo inválido uma mensagem
  própria em português:
  - E-mail vazio: "Informe o e-mail."; formato inválido: "Informe um e-mail válido."
  - Senha vazia: "Informe a senha."; no Cadastro, fora de 3 a 30 caracteres: "A senha deve ter
    entre 3 e 30 caracteres."
  - Administradora vazia (ou só espaços): "Informe a administradora."
  - Grupo vazio: "Informe o grupo."; com algo além de dígitos: "O grupo deve conter apenas
    números."
  - Cota vazia: "Informe a cota."; com algo além de dígitos: "A cota deve conter apenas números."
  - Crédito negativo: "O crédito não pode ser negativo."
  - A mensagem de um campo MUST sumir assim que o valor for corrigido.
- **FR-019**: Erros de validação devolvidos pela API (casos que escaparem da validação na tela)
  MUST ser exibidos junto ao campo correspondente; erros de duplicidade e de "não encontrado" MUST ser exibidos como alerta no
  topo do formulário ou da página, com as mensagens definidas nas histórias acima.
- **FR-020**: Falhas de conexão com a API MUST exibir mensagem amigável sem quebrar a página.
- **FR-021**: Durante um envio, o botão de envio MUST ficar desabilitado para evitar envio
  duplicado.
- **FR-022**: Nenhuma tela MUST depender de dado que não esteja no contrato da API.

### Key Entities

- **Usuário**: pessoa que acessa o sistema; identificada pelo e-mail (único, sem diferenciar
  maiúsculas) e protegida por senha. A senha nunca é exibida nem devolvida pela API.
- **Sessão**: o token de acesso obtido no login, válido por 60 minutos, sem renovação
  automática, e mantido apenas enquanto a aba do navegador estiver aberta.
- **Consórcio**: cota de consórcio acompanhada pela pessoa. Identificado pela combinação única
  administradora + grupo + cota. Tem valores (crédito, valor pago, comissão, fundo de reserva,
  taxa de transferência, previsão de lucro, lucro) e datas (compra, veto, assembleia,
  contemplação, data do lucro). Situação derivada: "Contemplado" quando há data de contemplação;
  caso contrário, "Em andamento".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma pessoa com Docker instalado chega à página de Login em
  `http://localhost:5200` executando um único comando, sem passos manuais adicionais.
- **SC-002**: Uma pessoa nova consegue criar a conta, entrar e ver a página principal em menos de
  1 minuto.
- **SC-003**: Uma pessoa consegue cadastrar um consórcio completo em menos de 2 minutos.
- **SC-004**: Em 100% dos casos, os valores do resumo coincidem com as somas dos dados da API.
- **SC-005**: Após criar, editar ou excluir, a lista atualizada aparece em menos de 2 segundos.
- **SC-006**: Todo erro previsto no contrato (validação, duplicidade, não encontrado, sessão
  inválida, falha de conexão) produz uma mensagem em português; nenhum erro deixa a tela em
  branco ou travada.
- **SC-007**: Em comparação visual com as referências de `docs/layout/`, as três páginas usam a
  mesma estrutura, cores, fontes e componentes.

## Assumptions

- "Pasta raiz do app" é a pasta que contém o arquivo de orquestração com os serviços `db`, `api`
  e `web` (a pasta acima da pasta do front); a porta 5200 já está mapeada para o serviço `web`.
- A API segue `docs/contrato-api.md` e roda junto, no mesmo `docker compose up`.
- Todos os usuários autenticados veem os mesmos consórcios: o contrato não associa consórcios a
  usuários.
- Onde a referência visual diverge do contrato, vale o contrato. Exemplo: `cadastro.html` pede
  senha com no mínimo 8 caracteres, mas o contrato aceita de 3 a 30; e o campo da referência se
  chama "senha", enquanto o contrato usa "password".
- Os dados de exemplo das referências (Sicredi, Porto Seguro etc.) não são exibidos; tudo vem da
  API.
- "Próx. assembleia" no cartão mostra o campo "assembleia" do consórcio.
- Os campos de valor do formulário aceitam números com até 2 casas decimais; datas são escolhidas
  em seletor de data.
- As fontes externas usadas pela referência são carregadas da internet; sem conexão, o navegador
  usa fontes alternativas.
