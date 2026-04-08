import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import {
  authInterceptor,
  LEILAOAUTO_API_BASE_URL,
  LEILAOAUTO_TOKEN_STORAGE_KEY
} from '@leilaoauto/shared-services';
import { routes } from './app.routes';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    { provide: LOCALE_ID, useValue: 'pt-BR' },
    { provide: LEILAOAUTO_API_BASE_URL, useValue: environment.apiBaseUrl },
    { provide: LEILAOAUTO_TOKEN_STORAGE_KEY, useValue: 'leilaoauto_mobile_token' }
  ]
};
