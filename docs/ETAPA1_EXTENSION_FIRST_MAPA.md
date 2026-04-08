# LEILAOAUTO - Etapa 1 (Extension-First)

## 1) Mapa do que reaproveitar

### Backend (.NET 8)
- `backend/LeilaoAuto.Domain`
  - Entidades centrais: `User`, `Subscription`, `MonitoredVehicle`, `Lot`, `LotAnalytics`, `ConnectorExecutionLog`, `AuctionLot`.
  - Regras críticas já prontas:
    - limite de 4 veículos monitorados;
    - validação de `lotUrl`;
    - normalização de modelo/título;
    - scoring de oportunidade e risco.
- `backend/LeilaoAuto.Application`
  - Serviços de negócio já maduros:
    - `AuthService`, `MonitoringService`, `LotService`, `AnalyticsService`;
    - `OpportunityScoringService`, `RiskScoringService`;
    - `BillingService`.
  - Contratos/DTOs aproveitáveis para canal extensão/web/mobile.
- `backend/LeilaoAuto.Infrastructure`
  - EF Core + PostgreSQL + repositórios.
  - Conectores por domínio (`Superbid`, `SodreSantoro`, `VipLeiloes`, `Freitas`, `Zukerman`, `MegaLeiloes`, `PactoLeiloes`, `MilanLeiloes`).
  - `HttpClientFactory` + Polly + seed.
- `backend/LeilaoAuto.Api`
  - JWT, Controllers, FluentValidation, CORS, Swagger, ProblemDetails, políticas de plano.
- `backend/LeilaoAuto.Workers`
  - Processamento em background, logs de execução por conector.

### Frontend Angular atual
- Base de autenticação, interceptor JWT, guard, serviços de API.
- Páginas e componentes que já implementam domínio de:
  - monitoramento;
  - lotes ativos/encerrados;
  - analytics;
  - billing;
  - settings.

## 2) Mapa do que refatorar

- Estratégia de entrada do usuário:
  - sair de `web-first` para `extension-first`.
- UX:
  - reduzir navegação em múltiplas telas pesadas;
  - priorizar experiência operacional rápida em side panel.
- Camada frontend:
  - separar front em `apps/extension`, `apps/web`, `apps/mobile`.
- Contratos compartilhados:
  - centralizar tipos, utilitários de score/URL e cliente API em `packages/*`.
- Backend facade:
  - manter endpoints atuais e adicionar endpoints simplificados para extensão:
    - `/api/me`
    - `/api/opportunities`
    - `/api/scanner/run`
    - `/api/history`
    - `/api/settings` (GET/PUT)

## 3) Mapa do que mover para shared packages

- `packages/shared-types`
  - tipos de autenticação, oportunidades, histórico, settings, billing plan.
- `packages/shared-core`
  - utilitários de score label, validação de URL de lote, helpers de formatação.
- `packages/shared-services`
  - cliente HTTP base e serviços de domínio para consumo em extensão/web/mobile.
- `packages/shared-ui`
  - tokens visuais e componentes leves:
    - `AppHeader`
    - `PrimaryActionButton`
    - `StatusChips`
    - `OpportunityCard`
    - `HistoryList`
    - `FilterBar`
    - `EmptyState`
    - `LoadingState`
    - `PlanBadge`

## 4) Estrutura alvo de monorepo

```text
/apps
  /extension
  /web
  /mobile

/packages
  /shared-ui
  /shared-core
  /shared-services
  /shared-types

/backend
  /LeilaoAuto.Api
  /LeilaoAuto.Application
  /LeilaoAuto.Domain
  /LeilaoAuto.Infrastructure
  /LeilaoAuto.Workers
  /LeilaoAuto.Tests
  /LeilaoAuto.sln
```

## 5) Estratégia de transição

1. Reorganizar pastas para monorepo sem quebrar build.
2. Introduzir pacotes compartilhados (`packages/*`) com foco em tipos, serviços e tokens.
3. Entregar extensão Chrome (Manifest V3 + side panel) como canal principal.
4. Adaptar web para mesma linguagem operacional da extensão.
5. Criar base mobile com Capacitor reaproveitando web/shared.
6. Adicionar facades no backend para fluxo direto da extensão.

