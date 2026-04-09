# MVP Extensao - Diagnostico de Fechamento

Data: 2026-04-09

## Escopo executado

1. Correcoes do MVP da extensao Chrome
2. Persistencia real de settings em banco
3. Estabilizacao dos endpoints da extensao
4. Revisao do fluxo ponta a ponta da extensao
5. Diagnostico de conectores reais vs mock

## Endpoints revisados (facade extension)

- `GET /api/opportunities`
- `POST /api/scanner/run`
- `GET /api/history`
- `GET /api/settings`
- `PUT /api/settings`

### Validacao em execucao local

Fluxo testado com API local em `http://localhost:8081`:

- login com usuario seed
- leitura de `/api/me`
- leitura e atualizacao de `/api/settings`
- consulta `/api/opportunities`
- execucao de `/api/scanner/run`
- consulta `/api/history`

Resultado observado:

- autenticacao OK
- settings persistidos em banco (valor atualizado retornado na API)
- opportunities retornando lotes validos
- scanner executando e retornando `success=true`
- history retornando registros recentes

## Persistencia de settings

Implementado:

- entidade `UserSettings`
- mapeamento EF `user_settings`
- relacao 1:1 com `User`
- repositorio `IUserSettingsRepository` + `UserSettingsRepository`
- `ExperienceService` com get-or-create e update persistente
- seed inicial de settings para usuarios seed
- migration `Phase11ExtensionMvpStabilization`

## Historico por usuario

Melhoria aplicada:

- `ConnectorExecutionLog` agora possui `UserId` opcional
- execucoes manuais (`/api/scanner/run`) gravam `UserId`
- `/api/history` agora filtra por usuario autenticado, mantendo logs de sistema (`UserId` nulo)
- migration faz backfill do `UserId` para logs manuais legados via `payload_json`

## Status dos conectores

### Operacional (fim a fim com busca HTTP + fallback)

- `SuperbidConnector`

### Estruturados, ainda mockados (com TODO tecnico explicito)

- `SodreSantoroConnector`
- `VipLeiloesConnector`
- `FreitasConnector`
- `ZukermanConnector`
- `MegaLeiloesConnector`
- `PactoLeiloesConnector`
- `MilanLeiloesConnector`

## Itens criticos antes de monetizacao

1. Ter pelo menos 2-3 conectores reais adicionais alem do Superbid para aumentar cobertura de inventario.
2. Definir politicas de cota por plano nos endpoints da facade da extensao (limites de busca, scanner, historico).
3. Adicionar testes automatizados para os endpoints da facade (`/opportunities`, `/scanner/run`, `/history`, `/settings`).
4. Refinar observabilidade por usuario/plano (dashboards de falha por conector, tempo de varredura, taxa de descarte por URL invalida).
5. Endurecer contrato de URL exata por conector real (validacao por dominio e padrao de rota de lote).

Observacao: Stripe real e mobile continuam fora do escopo atual conforme priorizacao.
