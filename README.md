# LEILAOAUTO - Fase 6

Fase 6 implementa fluxo completo de listagem de lotes com separacao entre ativos e encerrados, incluindo busca por filtros e tela de resultados com 3 areas.

## Backend - Endpoints de lotes

- `GET /api/lots/search`
- `GET /api/lots/active`
- `GET /api/lots/closed`
- `GET /api/lots/{id}`
- `POST /api/lots/refresh`

## Regras aplicadas

- Busca por filtros de marca, modelo, ano, tipo, UF e estado do veiculo.
- `active` retorna apenas lotes em andamento (`Active` e `Confirmed` com URL valida).
- `closed` retorna apenas lotes encerrados.
- Item confirmado sem `lotUrl` valida nunca e retornado.
- Separacao clara entre ativos e encerrados no backend e no frontend.

## Backend - Entregas

- `LotsController` completo para Fase 6.
- `LotService` reestruturado para:
  - busca consolidada (`search`) com ativos, encerrados e medias/faixas
  - consulta separada de ativos e encerrados
  - consulta por id
  - refresh de lotes
- Repositorio de lotes ajustado com:
  - filtros por ano exato
  - busca de encerrados por filtro
  - consulta por id
  - filtro de URL valida em retornos

## Frontend Angular - Tela Resultados

Tela de `Lots` com 3 areas:

- **Buscar**: formulario com marca, modelo, ano, tipo, UF e estado
- **Em andamento**: lista de lotes ativos encontrados
- **Encerrados / media**:
  - historico encerrado
  - medias e faixa por modelo

Cada card de lote exibe:

- titulo
- preco
- status
- fonte
- score
- botao **Abrir lote** (somente com URL valida)

## Navegacao

Fluxo funcional entre:

- Dashboard
- Monitoring
- Lots
- Analytics

## Validacao da fase

- `dotnet build LeilaoAuto.sln` OK
- `dotnet test LeilaoAuto.Tests/LeilaoAuto.Tests.csproj` OK
- `npm run build` em `leilaoauto-web` OK
