import { Inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, switchMap, tap } from 'rxjs';
import type {
  AuthMeResponse,
  AuthResponse,
  LoginRequest,
  RegisterRequest
} from '@leilaoauto/shared-types';
import { LEILAOAUTO_API_BASE_URL, LEILAOAUTO_TOKEN_STORAGE_KEY } from './api.tokens';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenSignal;
  private readonly currentUserSignal = signal<AuthMeResponse | null>(null);

  constructor(
    private readonly http: HttpClient,
    @Inject(LEILAOAUTO_API_BASE_URL) private readonly apiBaseUrl: string,
    @Inject(LEILAOAUTO_TOKEN_STORAGE_KEY) private readonly tokenStorageKey: string
  ) {
    this.tokenSignal = signal<string | null>(this.readToken());
  }

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

  private readToken(): string | null {
    try {
      return localStorage.getItem(this.tokenStorageKey);
    } catch {
      return null;
    }
  }
}
