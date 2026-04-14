# Cobertura Real de Conectores (Extensão)

Data de referência: 14/04/2026.

## Conectores realmente operacionais
- Superbid: operacional com busca real + fallback mock.
- Sodré Santoro: operacional com busca real + fallback mock.
- VIP Leilões: operacional com busca real + fallback mock.
- Mega Leilões: operacional com busca real + fallback mock (acesso avançado por plano em oportunidades).

## Conectores ainda mockados
- Freitas: mock estruturado.
- Zukerman: mock estruturado.
- Pacto Leilões: mock estruturado.
- Milan Leilões: mock estruturado.

## Cobertura por categoria (estado atual)
- Veículos: cobertura real consolidada.
  Fontes reais principais: Superbid, Sodré Santoro, VIP Leilões, Mega Leilões.
- Imóveis: cobertura parcial (sem conector real dedicado ativo nesta base).
  Fonte hoje associada: Zukerman (mock).
- Máquinas e Equipamentos: cobertura parcial.
- Materiais / Estoque: cobertura parcial.
- Sucatas: cobertura parcial (depende de lotes veiculares específicos).
- Judicial/Extrajudicial/Diversos: cobertura parcial.

## Impacto no modo simples
- Modo simples expõe somente `Todos` e `Veículos`.
- Categorias com cobertura parcial permanecem no modo avançado com indicação explícita de cobertura parcial.
- A extensão mostra quantas fontes reais ativas suportam a categoria selecionada.
