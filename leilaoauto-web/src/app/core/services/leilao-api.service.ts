import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateMonitoredVehicleRequest, MonitoredVehicle } from '../models/monitoring.models';
import { ExactLotRequest, Lot, LotSearchFilterRequest, ModelAverage } from '../models/lot.models';

@Injectable({ providedIn: 'root' })
export class LeilaoApiService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  getMonitoredVehicles(): Observable<MonitoredVehicle[]> {
    return this.http.get<MonitoredVehicle[]>(`${this.apiBaseUrl}/monitoring/vehicles`);
  }

  addMonitoredVehicle(request: CreateMonitoredVehicleRequest): Observable<MonitoredVehicle> {
    return this.http.post<MonitoredVehicle>(`${this.apiBaseUrl}/monitoring/vehicles`, request);
  }

  removeMonitoredVehicle(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/monitoring/vehicles/${id}`);
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
