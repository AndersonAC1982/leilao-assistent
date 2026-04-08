# LEILAOAUTO

Buscador inteligente de oportunidades em leiloes automotivos, com backend .NET 8 e frontend Angular, focado em lotes validos, comparacao historica e apoio a decisao.

## Visao do sistema

O LEILAOAUTO integra autenticacao, monitoramento de veiculos, busca de lotes, analytics e scoring para apoiar usuarios na avaliacao de oportunidades em leiloes.

Principios centrais preservados:

- monitoramento de ate 4 veiculos por usuario
- filtros por marca, modelo, ano, tipo, UF e estado
- separacao clara entre lotes ativos e encerrados
- normalizacao/matching de modelo
- score de oportunidade e score de risco
- URL exata do lote obrigatoria para itens confirmados/listados
- estrutura preparada para FIPE, billing, alertas e evolucao de conectores

## Arquitetura

### Backend (.NET 8)

Projetos:

- `LeilaoAuto.Api` (ASP.NET Core Web API com Controllers)
- `LeilaoAuto.Application` (servicos, contratos, validacoes)
- `LeilaoAuto.Domain` (entidades, enums, regras)
- `LeilaoAuto.Infrastructure` (EF Core, PostgreSQL, repositorios, autenticacao, conectores)
- `LeilaoAuto.Workers` (BackgroundService para sync em lote)
- `LeilaoAuto.Tests` (testes unitarios)

Stack:

- ASP.NET Core Web API (Controllers)
- EF Core + PostgreSQL
- JWT
- Swagger/OpenAPI
- Serilog
- FluentValidation
- HttpClientFactory + Polly

### Frontend (Angular)

Projeto:

- `leilaoauto-web`

Estrutura por feature:

- `core`
- `shared`
- `features/auth`
- `features/dashboard`
- `features/monitoring`
- `features/lots`
- `features/analytics`
- `features/billing`
- `features/settings`

## Funcionalidades principais

- Registro, login e endpoint `/api/auth/me`
- CRUD de monitoramento por usuario autenticado
- Busca/listagem de lotes (`/api/lots/*`)
- Analytics (`/api/analytics/*`) com media e oportunidades
- Conectores por dominio com factory/registry
- Worker para sincronizacao automatica de lotes
- Billing por plano com endpoints:
  - `GET /api/billing/plan`
  - `POST /api/billing/checkout`
  - `POST /api/billing/webhook`

## Planos e politicas de acesso

Planos:

- Free
- Pro
- Premium
- Elite

Policies configuradas:

- `Plan.ProOrHigher` para analytics
- `Plan.PremiumOrHigher` para integracoes avancadas e refresh manual de lotes
- `Plan.EliteOnly` para endpoints de conectores

## Como rodar localmente (sem Docker)

Pre-requisitos:

- .NET SDK 8
- Node.js 22+
- PostgreSQL 16+

### 1) Banco

Crie banco `leilaoauto` e ajuste connection string em:

- `LeilaoAuto.Api/appsettings.Development.json`
- `LeilaoAuto.Workers/appsettings.Development.json`

### 2) Aplicar migrations

```bash
dotnet ef database update --project LeilaoAuto.Infrastructure --startup-project LeilaoAuto.Api
```

### 3) Subir API

```bash
dotnet run --project LeilaoAuto.Api
```

### 4) Subir Worker

```bash
dotnet run --project LeilaoAuto.Workers
```

### 5) Subir Angular

```bash
cd leilaoauto-web
npm ci
npm start
```

Frontend: `http://localhost:4200`  
API: `http://localhost:8080`

## Como rodar com Docker

### API + Worker + Postgres

```bash
docker compose up --build
```

### Com frontend containerizado (opcional)

```bash
docker compose --profile frontend up --build
```

Servicos principais:

- API: `http://localhost:8080`
- Frontend (profile `frontend`): `http://localhost:4200`
- PostgreSQL: `localhost:5432`

## Swagger

Com API em Development:

- `http://localhost:8080/swagger`

## Testes

Backend:

```bash
dotnet test LeilaoAuto.Tests/LeilaoAuto.Tests.csproj
```

Frontend build:

```bash
cd leilaoauto-web
npm run build
```

## Seed inicial (realista)

Seed inclui:

- usuarios com perfis Free/Pro/Premium/Elite
- monitoramentos por usuario
- subscriptions com status variados
- lotes ativos e encerrados
- analytics por modelo normalizado
- logs de execucao de conectores
- dados de compatibilidade para `AuctionLot`

Credenciais seed (desenvolvimento):

- `admin@leilaoauto.local` / `Admin1234`
- `pro@leilaoauto.local` / `Pro1234`
- `premium@leilaoauto.local` / `Premium1234`
- `free@leilaoauto.local` / `Free1234`

## Limitacoes atuais dos conectores

- Apenas parte dos conectores esta funcional fim-a-fim.
- Alguns conectores estao em modo mock estruturado (com TODO tecnico explicito).
- O provider de billing Stripe esta preparado como stub; provider fake esta ativo para validar fluxo nesta etapa.

## Roadmap tecnico

1. Implementar conectores reais restantes com parser por dominio e testes de regressao HTML/JSON.
2. Integrar Stripe real (checkout, assinatura recorrente, webhook assinado).
3. Evoluir observabilidade (metricas por conector, tracing e dashboards).
4. Fortalecer suite de testes (integracao API + banco + worker).
5. Endurecer seguranca (rate limiting, rotacao de segredo JWT, politicas mais granulares).
6. Expor API publica controlada para clientes Elite (roadmap de produto).
