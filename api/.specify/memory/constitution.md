<!--
Sync Impact Report
==================
Version change: (template, não ratificada) → 1.0.0
Princípios definidos (novos):
  - I. Simplicidade Acima de Tudo
  - II. Contrato Primeiro
  - III. Erros como ProblemDetails (RFC 9457)
  - IV. Execução em Container
  - V. Configuração por Ambiente
  - VI. Banco de Dados Autogerenciado
  - VII. Autenticação e Autorização com Identity + JWT
Seções adicionadas:
  - Restrições Técnicas
  - Fluxo de Desenvolvimento
  - Governança
Seções removidas: nenhuma (template tinha 5 slots de princípio; expandido para 7 a pedido).
Templates dependentes: lidos em tempo de execução; nenhuma alteração feita por este comando.
TODOs pendentes:
  - docs/contrato-api.md está vazio no momento da ratificação; precisa ser preenchido antes
    do primeiro /speckit-specify.
-->

# ConsorciosMariussi.Api Constitution

## Core Principles

### I. Simplicidade Acima de Tudo

ConsorciosMariussi.Api é um projeto didático. O código MUST ser o mais direto possível:

- MUST NOT usar camada de repositório (Repository/Unit of Work); o `DbContext` do EF Core é
  usado diretamente onde o dado é necessário.
- MUST NOT usar MediatR, AutoMapper ou bibliotecas equivalentes de mediação/mapeamento.
  Mapeamentos entre entidades e DTOs são escritos à mão.
- MUST NOT introduzir padrões, abstrações, interfaces ou projetos adicionais que não sejam
  estritamente necessários para atender a uma exigência concreta. Interfaces com uma única
  implementação são proibidas, salvo quando exigidas pelo framework.
- Toda complexidade adicional MUST ser justificada por escrito no `plan.md` da feature
  (seção de rastreamento de complexidade), explicando por que a alternativa simples não serve.

Racional: o objetivo é que qualquer aluno leia o código de ponta a ponta e entenda o fluxo de
uma requisição sem saltar por camadas indiretas.

### II. Contrato Primeiro

O arquivo `docs/contrato-api.md` é a fonte da verdade da API.

- Rotas, métodos HTTP, formatos JSON (nomes e tipos de propriedades), códigos de status e
  regras de validação MUST corresponder exatamente ao contrato.
- Endpoints, campos ou códigos de status que não estejam no contrato MUST NOT existir na
  implementação. Divergência entre código e contrato é um defeito.
- Mudanças de comportamento da API MUST começar pela alteração do contrato; o código segue
  o contrato, nunca o contrário.

Racional: o front-end (`../web`) é desenvolvido contra o mesmo contrato; qualquer desvio
quebra a integração.

### III. Erros como ProblemDetails (RFC 9457)

- Toda resposta de erro (4xx e 5xx) MUST ter corpo no formato ProblemDetails
  (`application/problem+json`), conforme a RFC 9457.
- Erros de validação MUST usar `ValidationProblemDetails`, com o dicionário `errors` por campo.
- Exceções não tratadas MUST ser convertidas em ProblemDetails com status 500, sem expor
  stack trace ou detalhes internos fora do ambiente de desenvolvimento.
- Respostas 401/403 do pipeline de autenticação/autorização MUST também seguir ProblemDetails.

Racional: um único formato de erro torna o tratamento no cliente previsível e testável.

### IV. Execução em Container

- A API MUST ser empacotada por um `Dockerfile` na raiz desta pasta (`api/`).
- A API é executada pelo `../docker-compose.yml`, no serviço `api`, que expõe a porta interna
  `8080`. A aplicação MUST escutar em `8080` dentro do container.
- Este projeto MUST NOT alterar o `../docker-compose.yml`. Qualquer necessidade que exija
  mudança no Compose é tratada fora deste projeto.
- `docker compose up` a partir da pasta acima MUST bastar para subir a API funcional.

Racional: o Compose é compartilhado entre API, banco e front-end; ele é o ambiente de
referência.

### V. Configuração por Ambiente

- A connection string MUST ser lida da configuração `ConnectionStrings:Default`. No container,
  ela é preenchida pela variável de ambiente `ConnectionStrings__Default`.
- O banco é o serviço `db` do Compose (PostgreSQL). Connection strings MUST NOT ser fixadas no
  código-fonte.
- Toda configuração MUST vir do sistema de configuração do ASP.NET Core (`appsettings*.json` +
  variáveis de ambiente), permitindo sobrescrita por ambiente sem recompilar.
- Como o Compose não pode ser alterado (Princípio IV), configurações que ele não fornece
  (ex.: parâmetros de JWT) MUST ter valores funcionais em `appsettings.json`, sobrescrevíveis
  por variável de ambiente.

Racional: o mesmo artefato roda localmente e no container mudando apenas configuração.

### VI. Banco de Dados Autogerenciado

- O banco MUST ser criado e atualizado automaticamente na inicialização da aplicação, aplicando
  as migrations do EF Core pendentes, sem nenhum passo manual.
- O esquema MUST ser versionado por migrations do EF Core incluídas no repositório.
- Dados iniciais obrigatórios (ex.: papéis do Identity) MUST ser semeados de forma idempotente
  na inicialização.

Racional: `docker compose up` em uma máquina limpa precisa resultar em uma API pronta para uso.

### VII. Autenticação e Autorização com Identity + JWT

- Usuários, senhas e papéis MUST ser gerenciados pelo ASP.NET Core Identity, persistidos no
  mesmo banco via EF Core.
- A autenticação da API MUST usar tokens JWT Bearer emitidos pela própria API.
- Endpoints protegidos MUST declarar sua exigência de autorização explicitamente (`[Authorize]`
  ou `RequireAuthorization`, com papel/política quando aplicável), conforme o contrato.
- Endpoints públicos MUST ser marcados explicitamente como anônimos.
- Senhas MUST NOT ser registradas em log nem retornadas em nenhuma resposta.

Racional: Identity + JWT é o caminho padrão e nativo do ASP.NET Core, alinhado ao Princípio I.

## Restrições Técnicas

- Plataforma: ASP.NET Core (C#) com EF Core e provedor Npgsql para PostgreSQL.
- Banco: PostgreSQL, fornecido pelo serviço `db` do `../docker-compose.yml`.
- Pacotes de terceiros MUST se limitar ao necessário (EF Core, Npgsql, Identity, JWT Bearer,
  e o mínimo para documentação/testes). Cada pacote novo MUST ser justificado no `plan.md`.
- Proibidos: MediatR, AutoMapper, bibliotecas de Repository genérico.

## Fluxo de Desenvolvimento

- Toda feature segue o fluxo Spec Kit: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks`
  → `/speckit-implement`.
- O `plan.md` de cada feature MUST conter uma verificação explícita contra os sete princípios
  (Constitution Check) antes da implementação.
- Antes de considerar uma feature concluída, MUST ser verificado que: as rotas e respostas batem
  com `docs/contrato-api.md`; os erros saem como ProblemDetails; e a API sobe com
  `docker compose up` aplicando migrations automaticamente.

## Governance

- Esta constituição prevalece sobre quaisquer outras práticas do projeto. Em conflito, ela vence.
- Emendas MUST ser feitas via `/speckit-constitution`, com Sync Impact Report e incremento de
  versão.
- Versionamento semântico: MAJOR para remoção ou redefinição incompatível de princípios; MINOR
  para novo princípio/seção ou expansão material; PATCH para esclarecimentos e redação.
- Toda revisão de código e todo `plan.md` MUST verificar conformidade com os princípios; desvios
  só são aceitos com justificativa registrada na seção de complexidade do plano.

**Version**: 1.0.0 | **Ratified**: 2026-11-16 | **Last Amended**: 2026-11-16
