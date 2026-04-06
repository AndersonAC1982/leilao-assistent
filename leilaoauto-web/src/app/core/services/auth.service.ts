import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenStorageKey = 'leilaoauto_token';
  private readonly tokenSignal = signal<string | null>(localStorage.getItem(this.tokenStorageKey));
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  token(): string | null {
    return this.tokenSignal();
  }

  isAuthenticated(): boolean {
    return !!this.tokenSignal();
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/auth/login`, request)
      .pipe(tap((response) => this.setSession(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/auth/register`, request)
      .pipe(tap((response) => this.setSession(response)));
  }

  logout(): void {
    localStorage.removeItem(this.tokenStorageKey);
    this.tokenSignal.set(null);
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenStorageKey, response.token);
    this.tokenSignal.set(response.token);
  }
}
