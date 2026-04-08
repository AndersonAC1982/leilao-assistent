import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthMeResponse, AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenStorageKey = 'leilaoauto_token';
  private readonly tokenSignal = signal<string | null>(localStorage.getItem(this.tokenStorageKey));
  private readonly currentUserSignal = signal<AuthMeResponse | null>(null);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  token(): string | null {
    return this.tokenSignal();
  }

  currentUser(): AuthMeResponse | null {
    return this.currentUserSignal();
  }

  isAuthenticated(): boolean {
    return !!this.tokenSignal();
  }

  bootstrapUser(): Observable<AuthMeResponse | null> {
    if (!this.tokenSignal()) {
      this.currentUserSignal.set(null);
      return of(null);
    }

    return this.getMe();
  }

  login(request: LoginRequest): Observable<AuthMeResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/auth/login`, request)
      .pipe(
        tap((response) => this.setSession(response)),
        switchMap(() => this.getMe())
      );
  }

  register(request: RegisterRequest): Observable<AuthMeResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/auth/register`, request)
      .pipe(
        tap((response) => this.setSession(response)),
        switchMap(() => this.getMe())
      );
  }

  getMe(): Observable<AuthMeResponse> {
    return this.http.get<AuthMeResponse>(`${this.apiBaseUrl}/auth/me`).pipe(
      tap((response) => this.currentUserSignal.set(response))
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenStorageKey);
    this.tokenSignal.set(null);
    this.currentUserSignal.set(null);
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenStorageKey, response.token);
    this.tokenSignal.set(response.token);
  }
}
