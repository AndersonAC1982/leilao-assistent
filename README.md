# LEILAOAUTO - Fase 5

Fase 5 implementa score de oportunidade e score de risco, incorporados ao retorno de lotes e analytics.

## Backend - scoring

### Score de oportunidade

Servico: `OpportunityScoringService`

Compara:

- `currentPrice` (ou `finalPrice` quando necessario)
- media historica do modelo

Saida:

- `opportunityScore` (0 a 100)
- `opportunityLabel`:
  - `OPORTUNIDADE`
  - `BOM_PRECO`
  - `ACIMA_DA_MEDIA`

### Score de risco

Servico: `RiskScoringService`

Analisa titulo e descricao do lote com palavras-chave criticas:

- sinistro
- recuperavel / recuperavel
- sucata
- enchente
- sem motor
- grande monta
- media monta
- pequena monta

Saida:

- `riskScore` (0 a 100)
- `damageLevel`
- `decision`:
  - `COMPRA_SEGURA`
  - `OPORTUNIDADE_COM_RISCO`
  - `ALTO_RISCO`

## Backend - endpoints ajustados

`/api/lots/*` e `/api/analytics/*` retornam scores com labels/decisao.

Novos campos relevantes de lotes:

- `title`
- `source`
- `referenceAveragePrice`
- `opportunityScore`
- `opportunityLabel`
- `riskScore`
- `damageLevel`
- `riskDecision`

## Frontend Angular

### Results (Lots)

Tela de resultados agora mostra cards com:

- titulo
- preco
- status
- fonte
- score de oportunidade
- score de risco
- botao **Abrir lote**

Regra aplicada:

- botao so renderiza com `lotUrl` valida.

### Dashboard

Cards de lotes ativos exibem os mesmos indicadores de score e o botao **Abrir lote** quando `lotUrl` e valida.

## Testes unitarios

Cobertura de Fase 5 adicionada para:

- score de oportunidade
- score de risco
- deteccao de palavras-chave criticas

Arquivo principal:

- `LeilaoAuto.Tests/ScoringPhase5Tests.cs`

Tambem permanece coberto o limite de 4 veiculos monitorados.

## Execucao

Subir API + Postgres:

```bash
docker compose up --build
```

Rodar frontend:

```bash
cd leilaoauto-web
npm install
npm start
```

## Validacao da fase

- `dotnet build LeilaoAuto.sln` OK
- `dotnet test LeilaoAuto.Tests/LeilaoAuto.Tests.csproj` OK
- `npm run build` em `leilaoauto-web` OK
