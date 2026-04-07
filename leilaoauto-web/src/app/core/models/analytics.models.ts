export interface ModelAveragePrice {
  comparableModel: string;
  averagePrice: number;
  minPrice: number;
  maxPrice: number;
  quantity: number;
}

export interface Opportunity {
  lotId: string;
  title: string;
  auctioneer: string;
  lotNumber: string;
  model: string;
  comparableModel: string;
  currentPrice: number;
  historicalAveragePrice: number;
  priceGap: number;
  priceGapPercent: number;
  opportunityScore: number;
  opportunityLabel: 'OPORTUNIDADE' | 'BOM_PRECO' | 'ACIMA_DA_MEDIA';
  riskScore: number;
  damageLevel: string;
  riskDecision: 'COMPRA_SEGURA' | 'OPORTUNIDADE_COM_RISCO' | 'ALTO_RISCO';
  lotUrl: string;
}

export interface RiskModelSummary {
  comparableModel: string;
  quantity: number;
  averageRiskScore: number;
}

export interface RiskSummary {
  totalActiveLots: number;
  averageRiskScore: number;
  lowRiskCount: number;
  mediumRiskCount: number;
  highRiskCount: number;
  topRiskModels: RiskModelSummary[];
}
