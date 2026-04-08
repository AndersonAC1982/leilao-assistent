import type { PlanType } from '@leilaoauto/shared-types';

const LOT_URL_PATTERN = /^https?:\/\/.+\/.+/i;

export function isValidLotUrl(url: string | null | undefined): boolean {
  if (!url) {
    return false;
  }

  const trimmed = url.trim();
  if (!LOT_URL_PATTERN.test(trimmed)) {
    return false;
  }

  try {
    const parsed = new URL(trimmed);
    return (parsed.protocol === 'http:' || parsed.protocol === 'https:') && parsed.hostname.length > 0;
  } catch {
    return false;
  }
}

export function toOpportunityLabel(label: string | null | undefined): string {
  switch ((label ?? '').trim().toUpperCase()) {
    case 'OPORTUNIDADE':
      return 'OPORTUNIDADE';
    case 'BOM_PRECO':
      return 'BOM PRECO';
    case 'ACIMA_DA_MEDIA':
      return 'ACIMA DA MEDIA';
    default:
      return label?.trim() || 'N/A';
  }
}

export function toRiskDecisionLabel(decision: string | null | undefined): string {
  switch ((decision ?? '').trim().toUpperCase()) {
    case 'COMPRA_SEGURA':
      return 'COMPRA SEGURA';
    case 'OPORTUNIDADE_COM_RISCO':
      return 'OPORTUNIDADE COM RISCO';
    case 'ALTO_RISCO':
      return 'ALTO RISCO';
    default:
      return decision?.trim() || 'N/A';
  }
}

export function toDamageLevelLabel(level: string | null | undefined): string {
  switch ((level ?? '').trim().toUpperCase()) {
    case 'LOW':
    case 'BAIXO':
      return 'Baixo';
    case 'MEDIUM':
    case 'MEDIO':
      return 'Medio';
    case 'HIGH':
    case 'ALTO':
      return 'Alto';
    default:
      return level?.trim() || 'Nao informado';
  }
}

export function toPlanLabel(plan: PlanType | number): string {
  switch (Number(plan)) {
    case 1:
      return 'Free';
    case 2:
      return 'Pro';
    case 3:
      return 'Premium';
    case 4:
      return 'Elite';
    default:
      return 'Desconhecido';
  }
}

export function toLotStatusLabel(status: number): string {
  switch (status) {
    case 0:
      return 'RASCUNHO';
    case 1:
      return 'ATIVO';
    case 2:
      return 'ENCERRADO';
    case 3:
      return 'CONFIRMADO';
    default:
      return 'N/A';
  }
}

export function toVehicleTypeLabel(type: number): string {
  switch (type) {
    case 1:
      return 'Carro';
    case 2:
      return 'Moto';
    case 3:
      return 'Caminhao';
    case 4:
      return 'Utilitario';
    case 5:
      return 'Outro';
    default:
      return 'N/A';
  }
}
