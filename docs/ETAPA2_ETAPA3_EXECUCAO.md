# ETAPA 2 e ETAPA 3 - EXECUCAO

## Escopo executado

- Etapa 2: reorganizacao inicial do monorepo
- Etapa 3: base funcional da extensao Chrome extension-first

## Entregas da Etapa 2

- estrutura `apps/`, `packages/`, `backend/`
- web em `apps/web`
- extensao em `apps/extension`
- backend preservado em `backend/*`
- `docker-compose.yml` ajustado para novos caminhos

## Entregas da Etapa 3

- `manifest.json` MV3
- `background.js` para comportamento do side panel
- `sidepanel.html` com layout simples
- `sidepanel.js` com logica principal
- servicos iniciais:
  - `auth.js`
  - `scanner.js`
  - `opportunities.js`
  - `history.js`
  - `api.js`
  - `storage.js`

## Integracao com backend

- facade endpoints usados pela extensao:
  - `GET /api/me`
  - `GET /api/opportunities`
  - `POST /api/scanner/run`
  - `GET /api/history`
  - `GET /api/settings`
  - `PUT /api/settings`

## Regras de dominio preservadas na extensao

- nunca renderizar item sem `lotUrl` valida
- abrir sempre URL exata do lote
- armazenamento local para token/filtros/historico curto

## Fora de escopo nesta entrega

- evolucao de mobile (mantido para etapa futura)
