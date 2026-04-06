# LEILAOAUTO - Fase 1

Scaffolding inicial do produto LEILAOAUTO com backend em .NET 8 e frontend em Angular standalone.

## Estrutura do repositorio

### Backend (.NET 8)

- LeilaoAuto.Api
- LeilaoAuto.Application
- LeilaoAuto.Domain
- LeilaoAuto.Infrastructure
- LeilaoAuto.Workers
- LeilaoAuto.Tests

### Frontend (Angular)

- leilaoauto-web

## Backend - Fase 1

Entregue nesta fase:

- ASP.NET Core Web API com Controllers
- Swagger/OpenAPI
- Serilog
- PostgreSQL via appsettings + environment variables
- EF Core DbContext inicial
- Health check basico em `/health`
- JWT configurado
- CORS configurado
- ProblemDetails e exception handler global
- Integracao FIPE inicial (BrasilAPI) com fallback tecnico

### Observacoes de FIPE

- Configuracao em `Fipe` no `appsettings`.
- Resolucao atual usa mapeamento `ModelCodeMappings` por modelo normalizado.
- Quando nao houver mapeamento, entra fallback estimado.
- TODO tecnico: substituir mapeamento estatico por catalogo persistente de codigos FIPE.

## Frontend - Fase 1

Entregue nesta fase:

- Angular CLI
- Standalone components
- Angular Router
- Reactive Forms
- Estrutura por features:
  - core
  - shared
  - features/auth
  - features/dashboard
  - features/monitoring
  - features/lots
  - features/analytics
  - features/billing
  - features/settings
- Layout principal com topo + menu lateral + area central de rotas

## Como rodar com Docker (Fase 1)

Este compose sobe apenas API + PostgreSQL:

```bash
docker compose up --build
```

Servicos:

- API: `http://localhost:8080`
- Health: `http://localhost:8080/health`
- Swagger: `http://localhost:8080/swagger` (Development)
- PostgreSQL: `localhost:5432`

## Como rodar frontend local

```bash
cd leilaoauto-web
npm install
npm start
```

Frontend local: `http://localhost:4200`

## Usuario seed (desenvolvimento)

- Email: `demo@leilaoauto.local`
- Senha: `Demo1234`

## Proximos TODOs tecnicos

- Evoluir fluxo completo de autenticacao/autorizacao
- Substituir `EnsureCreated` por migrations
- Integrar catalogo FIPE oficial para matching robusto
- Implementar regras densas de dominio nas proximas fases
- Adicionar observabilidade (metrics/traces)
