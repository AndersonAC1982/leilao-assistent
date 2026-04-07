# LEILAOAUTO - Fase 4

Fase 4 implementa normalizacao robusta de modelos, matching, agrupamento e analytics com endpoints dedicados e tela Angular funcional.

## Estrutura da solution

- `LeilaoAuto.Api`
- `LeilaoAuto.Application`
- `LeilaoAuto.Domain`
- `LeilaoAuto.Infrastructure`
- `LeilaoAuto.Workers`
- `LeilaoAuto.Tests`
- `leilaoauto-web`

## Backend - Normalizacao e matching

Servico robusto de normalizacao de modelo com regras:

- remove acentos
- ignora caixa
- remove caracteres especiais
- consolida espacos
- remove marca redundante do inicio
- gera versao comparavel para agrupamento e matching
- separa blocos letra/numero (`CG160` -> `CG 160`)
- remove ano isolado (`2022`) na versao comparavel

Exemplos equivalentes cobertos em testes:

- `Honda CG 160 Start`
- `CG160 START`
- `Honda CG 160 2022`

## Backend - Servicos de analytics

Novos servicos na camada Application:

- `IModelNormalizationService` / `ModelNormalizationService`
- `ILotAnalyticsComputationService` / `LotAnalyticsComputationService`
- `IAnalyticsService` / `AnalyticsService`

Capacidades:

- normalizar modelo
- comparar similaridade simples
- agrupar lotes por modelo normalizado/comparavel
- calcular media, menor preco, maior preco e quantidade

## Endpoints de analytics

- `GET /api/analytics/average-price`
  - filtro opcional: `?model=`
- `GET /api/analytics/opportunities`
  - filtro opcional: `?model=`
  - lista lotes ativos com preco atual abaixo da media historica
- `GET /api/analytics/risk-summary`
  - filtro opcional: `?model=`

## Frontend Angular - Tela Analytics

Tela `analytics` conectada aos novos endpoints com:

- cards de media por modelo
- faixa de preco (min-max)
- resumo de oportunidades
- resumo de risco
- filtro por modelo

## Testes unitarios

Cobertura adicionada para:

- normalizador
- regra de agrupamento
- calculo de media
- limite de 4 veiculos monitorados

## Execucao

Subir API + Postgres via Docker:

```bash
docker compose up --build
```

Rodar frontend:

```bash
cd leilaoauto-web
npm install
npm start
```

Endpoints uteis:

- API: `http://localhost:8080`
- Health: `http://localhost:8080/health`
- Swagger (Development): `http://localhost:8080/swagger`
- Frontend: `http://localhost:4200`

## Proximas fases

- conectores reais de leiloeiros
- ranking de oportunidade mais avancado
- alertas com regras configuraveis por usuario
