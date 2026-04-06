export interface LotSearchFilterRequest {
  make?: string;
  model?: string;
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
  lotUrl: string;
  opportunityScore: number;
  riskScore: number;
  updatedAtUtc: string;
}

export interface ModelAverage {
  normalizedModel: string;
  averageFinalPrice: number;
}
