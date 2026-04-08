const opportunityLabels: Record<string, string> = {
  OPORTUNIDADE: 'Oportunidade',
  BOM_PRECO: 'Bom preço',
  ACIMA_DA_MEDIA: 'Acima da média'
};

const riskDecisionLabels: Record<string, string> = {
  COMPRA_SEGURA: 'Compra segura',
  OPORTUNIDADE_COM_RISCO: 'Oportunidade com risco',
  ALTO_RISCO: 'Alto risco'
};

const damageLevelLabels: Record<string, string> = {
  UNKNOWN: 'Desconhecido',
  DESCONHECIDO: 'Desconhecido',
  LOW: 'Baixo',
  BAIXO: 'Baixo',
  MEDIUM: 'Médio',
  MEDIO: 'Médio',
  HIGH: 'Alto',
  ALTO: 'Alto',
  CRITICAL: 'Crítico',
  CRITICO: 'Crítico',
  LEVE: 'Leve',
  MODERADO: 'Moderado',
  GRAVE: 'Grave',
  SEM_INDICIOS: 'Sem indícios',
  SEM_INDICIOS_RELEVANTES: 'Sem indícios relevantes'
};

function prettifyToken(value: string): string {
  return value
    .toLowerCase()
    .split(/[_\s]+/)
    .filter((token) => token.length > 0)
    .map((token) => token[0].toUpperCase() + token.slice(1))
    .join(' ');
}

export function toOpportunityLabel(value: string | null | undefined): string {
  if (!value) {
    return 'Não informado';
  }

  return opportunityLabels[value] ?? prettifyToken(value);
}

export function toRiskDecisionLabel(value: string | null | undefined): string {
  if (!value) {
    return 'Não informado';
  }

  return riskDecisionLabels[value] ?? prettifyToken(value);
}

export function toDamageLevelLabel(value: string | null | undefined): string {
  if (!value) {
    return 'Não informado';
  }

  return damageLevelLabels[value] ?? prettifyToken(value);
}
