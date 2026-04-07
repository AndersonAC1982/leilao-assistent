# LEILAOAUTO - Fase 2

Fase 2 consolida o dominio central, persistencia e regras basicas do produto.

## Estrutura da solution

- `LeilaoAuto.Api`
- `LeilaoAuto.Application`
- `LeilaoAuto.Domain`
- `LeilaoAuto.Infrastructure`
- `LeilaoAuto.Workers`
- `LeilaoAuto.Tests`
- `leilaoauto-web`

## Dominio implementado

Entidades:

- `User`
- `Subscription`
- `MonitoredVehicle`
- `Lot`
- `LotAnalytics`
- `ConnectorExecutionLog`

Enums:

- `UserRole`
- `PlanType`
- `LotStatus`
- `SubscriptionStatus`

## Regras basicas implementadas

- Um usuario pode monitorar no maximo 4 veiculos.
- Um lote com status `Confirmed` exige `LotUrl` valida.
- Repositorio de lotes separa listagem de ativos e encerrados.
- `Lot.NormalizedTitle` e gerado automaticamente para comparacoes futuras.

## Persistencia EF Core

- Mapeamentos completos no projeto `LeilaoAuto.Infrastructure/Persistence/Configurations`.
- Indices e constraints aplicados para chaves unicas, performance e validacao basica.
- Migration inicial gerada:
  - `InitialPhase2`
  - pasta: `LeilaoAuto.Infrastructure/Persistence/Migrations`

## Seed inicial

Seed automatico contem:

- 1 usuario admin
- 1 usuario comum
- 4 veiculos monitorados de exemplo (2 por usuario)
- lotes ativos/encerrados/confirmado de exemplo
- analytics basicos por modelo
- log inicial de execucao de conector

Credenciais seed:

- Admin: `admin@leilaoauto.local` / `Admin1234`
- User: `demo@leilaoauto.local` / `Demo1234`

## Repositorios base

Interfaces adicionadas em `LeilaoAuto.Application/Abstractions/Persistence`:

- `IBaseRepository<TEntity>`
- `IUserEntityRepository`
- `ISubscriptionRepository`
- `IMonitoredVehicleRepository`
- `ILotRepository`
- `ILotAnalyticsRepository`
- `IConnectorExecutionLogRepository`

Implementacoes em `LeilaoAuto.Infrastructure/Persistence/Repositories`.

## Execucao

Subir API + Postgres via Docker:

```bash
docker compose up --build
```

Endpoints uteis:

- API: `http://localhost:8080`
- Health: `http://localhost:8080/health`
- Swagger (Development): `http://localhost:8080/swagger`

## Observacao desta fase

- Conectores reais de importacao de leiloes ainda nao foram implementados.
- Base de dominio/persistencia foi preparada para evolucao nas proximas fases.
