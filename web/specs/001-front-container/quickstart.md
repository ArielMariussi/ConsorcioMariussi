# Quickstart: validar o front

Roteiro manual (não há testes automatizados). Cada passo indica o resultado esperado e o
requisito que ele comprova.

## Pré-requisitos

- Docker com `docker compose`.
- Portas 5200, 5080 e 5433 livres.
- API do projeto com CORS liberado para `http://localhost:5200`
  ([research R10](research.md#r10-dependência-externa-cors-na-api)).

## Subir

Na pasta raiz do app (a que contém `docker-compose.yml`, acima de `web/`):

```bash
docker compose up --build
```

Esperado: serviços `db`, `api` e `web` sobem sem erro (FR-001).

## Roteiro

| # | Ação | Resultado esperado | Comprova |
|---|---|---|---|
| 1 | Abrir `http://localhost:5200` numa aba nova | Vai para `/login`, visual igual a `docs/layout/login.html` | FR-002, FR-010, SC-001 |
| 2 | "Criar conta" → enviar vazio | Nada é enviado; "Informe o e-mail." e "Informe a senha." | FR-018a |
| 3 | Cadastro com senha "ab" | "A senha deve ter entre 3 e 30 caracteres." | FR-018a |
| 4 | Cadastro com e-mail novo e senha "abc" | Sucesso e ida para `/login` | FR-007 |
| 5 | Repetir o cadastro com o mesmo e-mail | "Já existe uma conta com esse e-mail." | US2-2 |
| 6 | Login com senha errada | "E-mail ou senha inválidos." | US2-5 |
| 7 | Login correto | Página principal com topbar de `index.html` e resumo zerado + "ainda não há consórcios" | FR-008, US3-6 |
| 8 | "+ Novo consórcio" → grupo "12a" | "O grupo deve conter apenas números."; nada enviado | FR-018a |
| 9 | Criar Sicredi / 05014 / 0361, crédito 200000, valor pago 30000 | Modal fecha; cartão aparece com "05014", "R$ 200.000,00" e "Em andamento"; resumo atualizado | FR-015, FR-018, FR-006 |
| 10 | Criar de novo Sicredi / 05014 / 0361 | Alerta de duplicidade; dados mantidos | US4-2 |
| 11 | Editar o consórcio e preencher Contemplação | Etiqueta muda para "Contemplado" | FR-016, US3-3 |
| 12 | Buscar "sicr" e filtrar "Em andamento" | Cartão some só com o filtro de situação | FR-014 |
| 13 | Excluir → "Cancelar"; depois excluir → "Sim, excluir" | Primeiro nada muda; depois o cartão some e o resumo zera | FR-017, US6 |
| 14 | F5 na página principal | Continua logado | R2 |
| 15 | Fechar a aba, abrir `http://localhost:5200` de novo | Pede login novamente | FR-011a |
| 16 | Logout | Volta para `/login`; abrir `/` redireciona ao login | FR-011 |
| 17 | Com a API parada (`docker compose stop api`), tentar entrar | "Não foi possível conectar ao servidor. Tente novamente." | FR-020 |
| 18 | Abrir `http://localhost:5200/qualquer-coisa` | "Página não encontrada." | Edge case |
| 19 | No DevTools (aba Rede), conferir chamadas | Todas para `http://localhost:5080` (valor de `appsettings.json`); `.wasm` com `application/wasm`; `app.css` com `text/css` | FR-003, R9 |

## Encerrar

```bash
docker compose down
```
