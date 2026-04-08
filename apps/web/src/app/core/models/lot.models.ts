export interface LotSearchFilterRequest {
  make?: string;
  model?: string;
  year?: number;
  yearFrom?: number;
  yearTo?: number;
  vehicleType?: number;
  uf?: string;
  vehicleCondition?: number;
}

export interface ExactLotRequest {
  auctioneer: string;
  lotNumber: string;
}

export interface Lot {
  id: string;
  title: string;
  description: string | null;
  source: string;
  auctioneer: string;
  lotNumber: string;
  make: string;
  model: string;
  year: number;
  vehicleType: number;
  uf: string;
  vehicleCondition: number;
  status: number;
  currentBid: number | null;
  finalPrice: number | null;
  referenceAveragePrice: number | null;
  lotUrl: string;
  opportunityScore: number;
  opportunityLabel: 'OPORTUNIDADE' | 'BOM_PRECO' | 'ACIMA_DA_MEDIA';
  riskScore: number;
  damageLevel: string;
  riskDecision: 'COMPRA_SEGURA' | 'OPORTUNIDADE_COM_RISCO' | 'ALTO_RISCO';
  updatedAtUtc: string;
}

export interface ModelAverage {
  normalizedModel: string;
  averageFinalPrice: number;
}

export interface ModelPriceRange {
  comparableModel: string;
  averagePrice: number;
  minPrice: number;
  maxPrice: number;
  quantity: number;
}

export interface LotSearchResult {
  activeLots: Lot[];
  closedLots: Lot[];
  averages: ModelPriceRange[];
}
