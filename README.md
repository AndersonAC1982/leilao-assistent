# LEILAOAUTO - Fase 7

Fase 7 implementa arquitetura de conectores por dominio para leiloeiros, com factory, registry, contrato unificado e endpoint de teste.

## Arquitetura de conectores

Implementados:

- `ILotConnector`
- `BaseLotConnector`
- `ConnectorResult`
- `ConnectorFactory`
- `ConnectorRegistry`

Contrato de cada conector:

- `Name`
- `SupportedDomains`
- `SearchAsync(filters)`
- `ParseAsync(raw)`
- `ValidateLotUrl(url)`

## Conectores criados

Classes separadas:

- `SodreSantoroConnector`
- `SuperbidConnector`
- `VipLeiloesConnector`
- `FreitasConnector`
- `ZukermanConnector`
- `MegaLeiloesConnector`
- `PactoLeiloesConnector`
- `MilanLeiloesConnector`

## Estado funcional

- `SuperbidConnector` implementado ponta a ponta (search + parse + validate, com fallback mock estruturado).
- Demais conectores iniciados como mocks estruturados com TODO tecnico claro em cada classe.

## Regras de negocio preservadas

- Sem `lotUrl` exata, item nao e confirmado.
- Itens sem `lotUrl` valida sao descartados no pipeline.
- Parsing permanece isolado por dominio (cada classe com `ParseAsync` proprio).

## Infra preparada (HttpClientFactory + Polly)

- Cliente nomeado `lot-connectors` configurado com `HttpClientFactory`.
- Politicas `Polly` (retry/circuit-breaker) aplicadas.
- `AuctionProviderClient` agora orquestra todos os conectores registrados.

## Endpoints de conectores

- `GET /api/connectors`
- `POST /api/connectors/test/{name}`

Endpoint de teste retorna `ConnectorResult` com:

- total bruto (`raw`)
- total parseado
- descartados
- lotes validos
- observacoes

## Validacao da fase

- `dotnet build LeilaoAuto.sln` OK
- `dotnet test LeilaoAuto.Tests/LeilaoAuto.Tests.csproj` OK
- `npm run build` em `leilaoauto-web` OK
