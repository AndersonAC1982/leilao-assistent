import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateMonitoredVehicleRequest,
  MonitoredVehicle,
  UpdateMonitoredVehicleRequest
} from '../models/monitoring.models';
import {
  ModelAveragePrice,
  Opportunity,
  RiskSummary
} from '../models/analytics.models';
import { Lot, LotSearchFilterRequest, LotSearchResult } from '../models/lot.models';

@Injectable({ providedIn: 'root' })
export class LeilaoApiService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

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

  searchActiveLots(filter: LotSearchFilterRequest): Observable<Lot[]> {
    return this.getActiveLots(filter);
  }

  syncLots(): Observable<{ refreshed: number }> {
    return this.refreshLots();
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
