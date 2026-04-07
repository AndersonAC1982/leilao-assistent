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
import { ExactLotRequest, Lot, LotSearchFilterRequest, ModelAverage } from '../models/lot.models';

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

  searchActiveLots(filter: LotSearchFilterRequest): Observable<Lot[]> {
    return this.http.get<Lot[]>(`${this.apiBaseUrl}/lots/active`, { params: this.toHttpParams(filter) });
  }

  getClosedHistory(filter: LotSearchFilterRequest): Observable<Lot[]> {
    return this.http.get<Lot[]>(`${this.apiBaseUrl}/lots/history`, { params: this.toHttpParams(filter) });
  }

  getExactLot(request: ExactLotRequest): Observable<Lot> {
    return this.http.get<Lot>(`${this.apiBaseUrl}/lots/exact`, {
      params: this.toHttpParams(request)
    });
  }

  getModelAverages(): Observable<ModelAverage[]> {
    return this.http.get<ModelAverage[]>(`${this.apiBaseUrl}/lots/averages`);
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

  syncLots(): Observable<{ synced: number }> {
    return this.http.post<{ synced: number }>(`${this.apiBaseUrl}/lots/sync`, {});
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
