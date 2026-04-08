# LEILAOAUTO

Refatoracao em andamento para estrategia **extension-first**.

Status atual desta entrega:

- Etapa 2: reorganizacao inicial do monorepo
- Etapa 3: base funcional da extensao Chrome (canal principal)

## Estrutura atual do monorepo

```text
/apps
  /extension
  /web
  /mobile   (reservado, sem evolucao nesta etapa)
/packages
  /shared-core
  /shared-ui
  /shared-services
  /shared-types
/backend
  /LeilaoAuto.Api
  /LeilaoAuto.Application
  /LeilaoAuto.Domain
  /LeilaoAuto.Infrastructure
  /LeilaoAuto.Workers
  /LeilaoAuto.Tests
  LeilaoAuto.sln
```

## Etapa 2 concluida (Monorepo)

- backend central mantido e reaproveitado
- web realocado para `apps/web`
- extensao criada em `apps/extension`
- pacotes compartilhados criados em `packages/*`
- docker-compose ajustado para a nova estrutura

## Etapa 3 concluida (Extensao Chrome)

Arquivos principais:

- `apps/extension/manifest.json` (Manifest V3)
- `apps/extension/background.js` (abre side panel)
- `apps/extension/sidepanel.html`
- `apps/extension/sidepanel.js`
- `apps/extension/content.js`
- `apps/extension/services/auth.js`
- `apps/extension/services/scanner.js`
- `apps/extension/services/opportunities.js`
- `apps/extension/services/history.js`
- `apps/extension/services/storage.js`
- `apps/extension/services/api.js`

Fluxo implementado no side panel:

- login/logout
- leitura de status da operacao
- filtros simples
- listar oportunidades
- listar historico
- acao **Rodar agora** para scanner
- persistencia local com `chrome.storage.local`

Regras atendidas no client da extensao:

- nao exibir item sem `lotUrl` valida
- botao principal abre sempre URL exata do lote

## Integracao backend reaproveitado

Facades para extensao/web:

- `GET /api/me`
- `GET /api/opportunities`
- `POST /api/scanner/run`
- `GET /api/history`
- `GET /api/settings`
- `PUT /api/settings`

Endpoints legados (auth/lots/analytics/monitoring/billing/connectors) continuam ativos.

## Web (coexistencia)

- mantido funcional em `apps/web`
- ajustado para coexistir com a arquitetura nova
- sem escopo de expansao adicional nesta etapa

## Mobile

- **nao avancado nesta entrega**
- pasta `apps/mobile` permanece apenas reservada para etapa futura

## Como rodar localmente

### Backend

```bash
dotnet build backend/LeilaoAuto.sln
dotnet run --project backend/LeilaoAuto.Api
dotnet run --project backend/LeilaoAuto.Workers
```

API: `http://localhost:8080`
Swagger: `http://localhost:8080/swagger`

### Web (coexistencia)

```bash
npm --prefix apps/web install
npm --prefix apps/web start
```

Web: `http://localhost:4200`

### Extensao Chrome

1. Abrir `chrome://extensions`
2. Ativar `Developer mode`
3. `Load unpacked`
4. Selecionar `apps/extension`
5. Clicar no icone da extensao para abrir o side panel

## Validacoes executadas

```bash
dotnet build backend/LeilaoAuto.sln
dotnet test backend/LeilaoAuto.Tests/LeilaoAuto.Tests.csproj
npm --prefix apps/web run build
```
