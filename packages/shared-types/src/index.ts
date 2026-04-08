export type PlanType = 1 | 2 | 3 | 4;

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  token: string;
  expiresAtUtc: string;
}

export interface AuthMeResponse {
  userId: string;
  email: string;
  role: number;
  plan: PlanType;
  createdAt: string;
}

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

export interface CreateMonitoredVehicleRequest {
  brand: string;
  model: string;
  year: number;
  type: number;
  uf: string;
  vehicleState: number;
}

export interface UpdateMonitoredVehicleRequest {
  brand: string;
  model: string;
  year: number;
  type: number;
  uf: string;
  vehicleState: number;
}

export interface MonitoredVehicle {
  id: string;
  brand: string;
  model: string;
  year: number;
  type: number;
  uf: string;
  vehicleState: number;
  createdAt: string;
}

export interface BillingPlanDetails {
  plan: PlanType;
  displayName: string;
  monthlyPrice: number;
  features: string[];
}

export interface BillingPlanResponse {
  userId: string;
  currentPlan: PlanType;
  currentPlanDisplayName: string;
  subscriptionStatus: number | null;
  subscriptionEndsAt: string | null;
  plans: BillingPlanDetails[];
}

export interface BillingCheckoutRequest {
  targetPlan: PlanType;
  successUrl?: string | null;
  cancelUrl?: string | null;
}

export interface BillingCheckoutResponse {
  provider: string;
  sessionId: string;
  checkoutUrl: string;
  targetPlan: PlanType;
  expiresAtUtc: string;
  message: string;
}

export interface OpportunityFeedQuery {
  search?: string;
  source?: string;
  minScore?: number;
  vehicleType?: number;
  region?: string;
  model?: string;
  year?: number;
  uf?: string;
  vehicleCondition?: number;
}

export interface OpportunityFeedItem {
  lotId: string;
  source: string;
  score: number;
  scoreLabel: string;
  title: string;
  location: string;
  value: number;
  dateUtc: string;
  summary: string;
  riskScore: number;
  riskDecision: string;
  lotUrl: string;
  status: number;
}

export interface ScannerRunResponse {
  startedAtUtc: string;
  completedAtUtc: string;
  refreshedLots: number;
  success: boolean;
  message: string;
}

export interface HistoryItem {
  id: string;
  source: string;
  executedAtUtc: string;
  success: boolean;
  recordsRead: number;
  recordsSaved: number;
  newLots: number;
  status: string;
  message: string | null;
}

export interface UserSettings {
  search: string;
  source: string;
  minScore: number;
  vehicleType?: number | null;
  region?: string | null;
  advancedFiltersEnabled: boolean;
  updatedAtUtc: string;
}

export interface UpdateUserSettingsRequest {
  search: string;
  source: string;
  minScore: number;
  vehicleType?: number | null;
  region?: string | null;
  advancedFiltersEnabled: boolean;
}
