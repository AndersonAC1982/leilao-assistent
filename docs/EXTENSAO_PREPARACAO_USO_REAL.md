# MultiLeilao - Preparação para Uso Real (Extensão)

Este documento fecha a etapa final de validação operacional da extensão.

## 1. Checklist de testes manuais

## 1.1 Pré-requisitos

- [ ] Docker Desktop ativo
- [ ] Chrome atualizado
- [ ] Extensão carregada em `chrome://extensions` (modo desenvolvedor)
- [ ] API disponível em `http://localhost:8080`

## 1.2 Smoke test (2-3 minutos)

- [ ] Abrir o side panel da extensão
- [ ] Ver nome visual `MultiLeilao` no cabeçalho
- [ ] Fazer login com usuário de teste
- [ ] Clicar em `Rodar agora`
- [ ] Ver oportunidades carregadas
- [ ] Clicar em `Abrir lote` e confirmar abertura da URL exata

## 1.3 Autenticação e sessão

- [ ] Login com credenciais válidas retorna sucesso
- [ ] Login com senha inválida mostra erro amigável
- [ ] `Sair` limpa sessão e bloqueia ações autenticadas
- [ ] Reabrir extensão mantém sessão quando token ainda válido

## 1.4 Filtros e execução coerente

- [ ] Selecionar categoria, fontes, termo, score, UF e preço máximo
- [ ] Clicar `Rodar agora`
- [ ] Confirmar que resultados refletem os filtros selecionados
- [ ] Confirmar que o estado de varredura muda para execução e conclusão
- [ ] Confirmar bloqueio quando nenhuma fonte está selecionada

## 1.5 Oportunidades e histórico

- [ ] O contador de oportunidades atualiza corretamente
- [ ] Card exibe fonte, score, categoria, título, local, valor e resumo
- [ ] Botão `Mapa` só aparece quando há localização
- [ ] Histórico mostra últimas execuções com data/hora e status

## 1.6 Regras críticas

- [ ] Nenhum item sem `lotUrl` válida é exibido
- [ ] `Abrir lote` nunca abre home/categoria genérica
- [ ] URL aberta corresponde ao lote específico

## 1.7 Conectividade e fallback

- [ ] Com API offline, UI mostra mensagem amigável
- [ ] Bloco `Ajustes de conexão (avançado)` abre ao detectar falha
- [ ] `Autodetectar` encontra endpoint local quando API está disponível
- [ ] `Testar conexão` funciona com endpoint manual

## 1.8 Plano e cotas

- [ ] Conta `Free`: limite de resultados e execuções do scanner aplicado
- [ ] Conta `Pro/Premium/Elite`: limites superiores conforme regras atuais

---

## 2. Instalação da extensão no Chrome

1. Acesse `chrome://extensions`
2. Ative `Modo do desenvolvedor`
3. Clique em `Carregar sem compactação`
4. Selecione a pasta `apps/extension`
5. Clique no ícone da extensão para abrir o side panel

Observação:
- Sempre recarregue a extensão após mudanças em `apps/extension/*`.

---

## 3. Como rodar API e conectar com a extensão (local)

## 3.1 Subir stack local com Docker

```bash
npm run local:up
```

Serviços esperados:
- API: `http://localhost:8080`
- Health: `http://localhost:8080/health`
- Swagger: `http://localhost:8080/swagger`

Logs:

```bash
npm run local:logs
```

Parar stack:

```bash
npm run local:down
```

## 3.2 Rodar API sem Docker (opcional)

```bash
npm run local:api
```

## 3.3 Conectar extensão à API

- Fluxo padrão: a extensão tenta autodetectar endpoints locais.
- Se necessário:
  1. Abra `Ajustes de conexão (avançado)`
  2. Informe o endpoint (ex: `http://localhost:8080/api`)
  3. Clique `Testar conexão`

---

## 4. Estratégia simples de ambientes (dev/stage/prod)

## 4.1 Extensão

Arquivos:
- `apps/extension/config/environment.dev.json`
- `apps/extension/config/environment.stage.json`
- `apps/extension/config/environment.prod.json`
- `apps/extension/config/environment.json` (ativo)

Troca rápida:

```bash
npm run extension:env:dev
npm run extension:env:stage
npm run extension:env:prod
```

A extensão lê `config/environment.json` para resolver candidatos de API.

## 4.2 API

Arquivos:
- `backend/LeilaoAuto.Api/appsettings.Development.json`
- `backend/LeilaoAuto.Api/appsettings.Staging.json`
- `backend/LeilaoAuto.Api/appsettings.Production.json`

Seleção do ambiente:
- `ASPNETCORE_ENVIRONMENT=Development|Staging|Production`

Recomendação:
- Segredos (`ConnectionStrings`, `Jwt:SecretKey`) via variáveis de ambiente/secret manager.

---

## 5. Pendências finais antes de empacotar/publicar

## 5.1 Técnicas

- [ ] Definir domínio real de `stage/prod` e revisar `apiBaseCandidates`
- [ ] Revisar `host_permissions` do `manifest.json` para domínios reais
- [ ] Definir `CORS AllowedOrigins` com `chrome-extension://<EXTENSION_ID>`
- [ ] Confirmar conectores reais mínimos para operação comercial
- [ ] Executar rodada final de testes manuais em conta Free e Pro

## 5.2 Publicação Chrome Web Store

- [ ] Ícones e screenshots finais
- [ ] Descrição curta/longa final em PT-BR
- [ ] Política de privacidade publicada
- [ ] Termos de uso e canal de suporte
- [ ] Vídeo/demo (opcional, recomendado)

## 5.3 Segurança e operação

- [ ] Remover segredos hardcoded de qualquer arquivo versionado
- [ ] Garantir HTTPS em stage/prod
- [ ] Definir monitoramento básico de disponibilidade API
- [ ] Definir procedimento de rollback da extensão

