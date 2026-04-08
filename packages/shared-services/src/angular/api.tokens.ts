import { InjectionToken } from '@angular/core';

export const LEILAOAUTO_API_BASE_URL = new InjectionToken<string>('LEILAOAUTO_API_BASE_URL', {
  providedIn: 'root',
  factory: () => 'http://localhost:8080/api'
});

export const LEILAOAUTO_TOKEN_STORAGE_KEY = new InjectionToken<string>('LEILAOAUTO_TOKEN_STORAGE_KEY', {
  providedIn: 'root',
  factory: () => 'leilaoauto_token'
});
