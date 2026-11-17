<!--
Sync Impact Report
- Versão: 1.1.0 → 1.2.0 (MINOR: restrição técnica redefinida)
- Princípios modificados: nenhum
- Seção modificada: Restrições Técnicas
  - "HTML, CSS puro e JavaScript do navegador, sem etapa de build" →
    "Blazor WebAssembly standalone (.NET 10), publicado como arquivos estáticos; CSS puro em
    arquivo único"
  - Regra de dependências passa a citar pacotes NuGet além do template padrão do Blazor
- Histórico: 1.1.0 removeu a exceção de Login/Cadastro no Princípio III
- Seções adicionadas: nenhuma
- Seções removidas: nenhuma
- Templates: nenhum template alterado (leem a constituição em tempo de execução)
- TODOs pendentes: nenhum
-->

# ConsorcioMariussi Web Constitution

## Core Principles

### I. Simplicidade Acima de Tudo

Este é um projeto didático. O código MUST ser o mais direto possível:

- Proibido introduzir camadas, padrões de projeto, abstrações ou bibliotecas que não sejam
  estritamente necessários para a funcionalidade pedida.
- Toda complexidade adicional MUST ser justificada por escrito no plano da feature
  (seção de rastreamento de complexidade); sem justificativa, a solução mais simples vence.

**Justificativa**: o objetivo é que qualquer pessoa estudando o projeto entenda o código lendo-o
de cima a baixo, sem precisar conhecer frameworks ou arquiteturas.

### II. Contrato Primeiro

O arquivo `docs/contrato-api.md` é a única fonte da verdade para formatos de dados, endpoints,
autenticação e erros.

- Nenhuma tela MUST depender de um dado, campo ou endpoint que não esteja no contrato.
- Divergências entre o código e o contrato são defeitos do código; mudanças de dados exigem
  primeiro a atualização do contrato.
- Este projeto MUST NOT ler nenhum arquivo fora da sua própria pasta, com uma única exceção:
  `../docker-compose.yml`, apenas para consulta.

**Justificativa**: front e API evoluem separadamente; o contrato evita suposições implícitas.

### III. Fidelidade ao Layout

- Todas as páginas do frontend MUST ser feitas com base nas páginas de referência da pasta
  `docs/layout/`, reutilizando as mesmas classes CSS (de `styles.css`) e a mesma estrutura de
  marcação. Não há exceções.
- Correspondência obrigatória: a página principal segue `index.html`, a página de Cadastro
  segue `cadastro.html` e a página de Login segue `login.html`.
- Uma página sem referência correspondente em `docs/layout/` MUST NOT ser criada antes que a
  referência seja adicionada à pasta.

**Justificativa**: o layout de referência é o resultado esperado; desviar dele gera retrabalho.

### IV. CSS Puro

- Proibido Bootstrap, Tailwind ou qualquer framework CSS ou biblioteca de componentes.
- O projeto MUST ter um único arquivo de estilos.

**Justificativa**: mantém o projeto didático e alinhado ao `styles.css` de referência.

### V. Roda em Container

- O front MUST ser publicado como arquivos estáticos servidos por nginx.
- O empacotamento MUST ser feito por um `Dockerfile` na raiz desta pasta.
- A execução acontece pelo serviço `web` do `../docker-compose.yml`. Este projeto MUST NOT
  alterar o `docker-compose.yml`.

**Justificativa**: o ambiente de execução é compartilhado e definido fora deste projeto.

### VI. Endereço da API Configurável

- O front roda no navegador do usuário, portanto MUST chamar a API pelo endereço visto pelo
  navegador (`http://localhost:5080` no ambiente padrão).
- Esse endereço MUST vir de um arquivo de configuração e MUST NOT ficar fixo no código das
  telas.

**Justificativa**: o endereço interno da rede Docker não é acessível pelo navegador, e o
endereço pode mudar entre ambientes.

### VII. Autenticação e Autorização

- O fluxo MUST ser: primeiro o cadastro na página de Cadastro, depois o login na página de
  Login, conforme os endpoints de autenticação definidos no contrato.
- Telas que consomem endpoints protegidos MUST enviar o token conforme o contrato e MUST
  redirecionar para o Login quando não houver token ou quando a API responder 401.

**Justificativa**: os dados do sistema só podem ser acessados por usuários autenticados.

### VIII. Interface em Português do Brasil

Todos os textos visíveis ao usuário (rótulos, botões, mensagens de erro e de sucesso, títulos)
MUST estar em português do Brasil.

**Justificativa**: o público-alvo do sistema é brasileiro.

## Restrições Técnicas

- Tecnologias: Blazor WebAssembly standalone (.NET 10), publicado como arquivos estáticos;
  CSS puro em arquivo único.
- Dependências além do template padrão do Blazor (pacotes NuGet, bibliotecas JS, frameworks
  CSS) só podem ser adicionadas com justificativa aprovada, conforme o Princípio I.
- Fontes de verdade: `docs/contrato-api.md` (dados e API) e `docs/layout/` (visual).

## Fluxo de Desenvolvimento

- Cada feature segue o fluxo Spec Kit: especificação → plano → tarefas → implementação.
- O plano de cada feature MUST passar pelo "Constitution Check", verificando os oito
  princípios antes da implementação.
- Uma feature só é considerada pronta quando roda via `docker compose` no serviço `web` e
  conversa com a API pelo endereço configurado.

## Governance

- Esta constituição prevalece sobre qualquer outra prática ou preferência do projeto.
- Emendas MUST ser feitas pelo comando de constituição, com registro no Sync Impact Report e
  atualização da versão.
- Versionamento semântico: MAJOR para remoção ou redefinição incompatível de princípios; MINOR
  para novo princípio ou seção, ou expansão relevante; PATCH para esclarecimentos e redação.
- Toda revisão de especificação, plano ou código MUST verificar a conformidade com estes
  princípios; violações precisam de justificativa explícita ou correção.

**Version**: 1.2.0 | **Ratified**: 2026-11-17 | **Last Amended**: 2026-11-17
