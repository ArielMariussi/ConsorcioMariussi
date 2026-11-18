# Data Model: Front publicado em container

Fonte da verdade: `docs/contrato-api.md`. O front não persiste dados; estes são os modelos que
ele envia e recebe, mais o estado da sessão.

## Credenciais (envio para register e login)

| Campo | Tipo | Validação na tela (mensagem) |
|---|---|---|
| email | string | Obrigatório ("Informe o e-mail."); formato de e-mail ("Informe um e-mail válido.") |
| password | string | Obrigatório ("Informe a senha."); no Cadastro, 3 a 30 caracteres ("A senha deve ter entre 3 e 30 caracteres.") |

No Login só se exige que os campos não estejam vazios (o contrato valida apenas isso no login).

## RespostaLogin (recebida)

| Campo | Tipo | Uso |
|---|---|---|
| accessToken | string | Guardado em `sessionStorage`; enviado como `Bearer` |
| expiresIn | int (segundos) | Informativo; a expiração é detectada pelo 401 da API |

## Sessão (estado no navegador)

| Estado | Condição | Efeito |
|---|---|---|
| Sem sessão | não há token na aba | "/" redireciona para `/login` |
| Autenticada | há token | `/login` e `/cadastro` redirecionam para "/" |
| Expirada | API respondeu 401 | token apagado → `/login?expirou=1` |

Transições: Sem sessão → (login 200) → Autenticada → (Logout, fechar aba ou 401) → Sem sessão.

## Consorcio

| Campo | Tipo | Obrigatório | Validação na tela (mensagem) | Exibição |
|---|---|---|---|---|
| id | int | — (só leitura) | — | não exibido |
| administradora | string | sim | não vazio após `Trim` ("Informe a administradora.") | título do cartão |
| grupo | string | sim | não vazio ("Informe o grupo."); só dígitos ("O grupo deve conter apenas números.") | texto, mantém zeros à esquerda |
| cota | string | sim | não vazio ("Informe a cota."); só dígitos ("A cota deve conter apenas números.") | texto, mantém zeros à esquerda |
| credito | decimal? | não | ≥ 0 ("O crédito não pode ser negativo.") | moeda |
| valorPago | decimal? | não | — | moeda |
| dataCompra | DateOnly? | não | — | dd/mm/aaaa |
| vencimento | DateOnly? | não | — | dd/mm/aaaa |
| assembleia | DateOnly? | não | — | dd/mm/aaaa (também "Próx. assembleia") |
| comissao | decimal? | não | — | moeda |
| previsaoLucro | decimal? | não | — | moeda |
| fundoReserva | decimal? | não | — | moeda |
| contemplacao | DateOnly? | não | — | dd/mm/aaaa |
| taxaTransferencia | decimal? | não | — | moeda |
| dataLucro | DateOnly? | não | — | dd/mm/aaaa |
| lucro | decimal? | não | — | moeda (destaque `info--accent`) |

Regras:

- Unicidade administradora + grupo + cota: verificada só pela API (409).
- Campos vazios são enviados como `null`. Na exibição, `null` vira "—".
- **Situação (derivada, não enviada)**: `contemplacao != null` → "Contemplado"
  (`badge badge--success`); senão "Em andamento" (`badge`).

## Resumo (derivado da lista carregada)

| Indicador | Cálculo |
|---|---|
| Consórcios | quantidade de itens |
| Total investido | soma de `valorPago` (null = 0) |
| Previsão de lucro | soma de `previsaoLucro` (null = 0) |
| Total de lucro | soma de `lucro` (null = 0) |

## Filtros (apenas na tela)

- **Busca**: texto contido em administradora, grupo ou cota, sem diferenciar maiúsculas.
- **Situação**: Todos / Contemplados / Em andamento (pela situação derivada).
- O resumo considera todos os consórcios carregados, independentemente dos filtros
  (como na referência, que soma o total).
