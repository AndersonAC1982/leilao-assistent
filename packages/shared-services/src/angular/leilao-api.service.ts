import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BillingCheckoutRequest,
  BillingCheckoutResponse,
  BillingPlanResponse,
  CreateMonitoredVehicleRequest,
  AuthMeResponse,
  HistoryItem,
  Lot,
  LotSearchFilterRequest,
  LotSearchResult,
  ModelAveragePrice,
  MonitoredVehicle,
  Opportunity,
  OpportunityFeedItem,
  OpportunityFeedQuery,
  RiskSummary,
  ScannerRunResponse,
  UpdateMonitoredVehicleRequest,
  UpdateUserSettingsRequest,
  UserSettings
} from '@leilaoauto/shared-types';
import { LEILAOAUTO_API_BASE_URL } from './api.tokens';

@Injectable({ providedIn: 'root' })
export class LeilaoApiService {
  constructor(
    private readonly http: HttpClient,
    @Inject(LEILAOAUTO_API_BASE_URL) private readonly apiBaseUrl: string
  ) {}

  getMonitoredVehicles(): Observable<MonitoredVehicle[]> {
    return this.http.get<MonitoredVehicle[]>(`${this.apiBaseUrl}/monitoring`);
  }

  addMonitoredVehicle(request: CreateMonitoredVehicleRequest): Observable<MonitoredVehicle> {
    return this.http.post<MonitoredVehicle>(`${this.apiBaseUrl}/monitoring`, request);
  }

  updateMonitoredVehicle(id: string, request: UpdateMonitoredVehicleRequest): Observable<MonitoredVehicle> {
    return this.http.put<MonitoredVehicle>(`${this.apiBaseUrl}/monitoring/${id}`, request);
  }

  removeMonitoredVehicle(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/monitoring/${id}`);
  }

  searchLots(filter: LotSearchFilterRequest): Observable<LotSearchResult> {
    return this.http.get<LotSearchResult>(`${this.apiBaseUrl}/lots/search`, { params: this.toHttpParams(filter) });
  }

  getActiveLots(filter: LotSearchFilterRequest): Observable<Lot[]> {
    return this.http.get<Lot[]>(`${this.apiBaseUrl}/lots/active`, { params: this.toHttpParams(filter) });
  }

  getClosedLots(filter: LotSearchFilterRequest): Observable<Lot[]> {
    return this.http.get<Lot[]>(`${this.apiBaseUrl}/lots/closed`, { params: this.toHttpParams(filter) });
  }

  getLotById(id: string): Observable<Lot> {
    return this.http.get<Lot>(`${this.apiBaseUrl}/lots/${id}`);
  }

  refreshLots(): Observable<{ refreshed: number }> {
    return this.http.post<{ refreshed: number }>(`${this.apiBaseUrl}/lots/refresh`, {});
  }

  getAveragePrice(model?: string): Observable<ModelAveragePrice[]> {
    return this.http.get<ModelAveragePrice[]>(`${this.apiBaseUrl}/analytics/average-price`, {
      params: this.toHttpParams({ model })
    });
  }

  getOpportunities(model?: string): Observable<Opportunity[]> {
    return this.http.get<Opportunity[]>(`${this.apiBaseUrl}/analytics/opportunities`, {
      params: this.toHttpParams({ model })
    });
  }

  getRiskSummary(model?: string): Observable<RiskSummary> {
    return this.http.get<RiskSummary>(`${this.apiBaseUrl}/analytics/risk-summary`, {
      params: this.toHttpParams({ model })
    });
  }

  getBillingPlan(): Observable<BillingPlanResponse> {
    return this.http.get<BillingPlanResponse>(`${this.apiBaseUrl}/billing/plan`);
  }

  checkoutBilling(request: BillingCheckoutRequest): Observable<BillingCheckoutResponse> {
    return this.http.post<BillingCheckoutResponse>(`${this.apiBaseUrl}/billing/checkout`, request);
  }

  getMe(): Observable<AuthMeResponse> {
    return this.http.get<AuthMeResponse>(`${this.apiBaseUrl}/me`);
  }

  getOpportunitiesFeed(query: OpportunityFeedQuery = {}): Observable<OpportunityFeedItem[]> {
    return this.http.get<OpportunityFeedItem[]>(`${this.apiBaseUrl}/opportunities`, {
      params: this.toHttpParams(query)
    });
  }

  runScanner(): Observable<ScannerRunResponse> {
    return this.http.post<ScannerRunResponse>(`${this.apiBaseUrl}/scanner/run`, {});
  }

  getExecutionHistory(take = 8): Observable<HistoryItem[]> {
    return this.http.get<HistoryItem[]>(`${this.apiBaseUrl}/history`, {
      params: this.toHttpParams({ take })
    });
  }

  getSettings(): Observable<UserSettings> {
    return this.http.get<UserSettings>(`${this.apiBaseUrl}/settings`);
  }

  updateSettings(request: UpdateUserSettingsRequest): Observable<UserSettings> {
    return this.http.put<UserSettings>(`${this.apiBaseUrl}/settings`, request);
  }

  private toHttpParams(source: object): HttpParams {
    let params = new HttpParams();
    Object.entries(source).forEach(([key, value]) => {
      if (value !== undefined && value !== null && `${value}`.trim() !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }
}
