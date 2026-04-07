export interface ModelAveragePrice {
  comparableModel: string;
  averagePrice: number;
  minPrice: number;
  maxPrice: number;
  quantity: number;
}

export interface Opportunity {
  lotId: string;
  auctioneer: string;
  lotNumber: string;
  model: string;
  comparableModel: string;
  currentPrice: number;
  historicalAveragePrice: number;
  priceGap: number;
  priceGapPercent: number;
  riskScore: number;
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
