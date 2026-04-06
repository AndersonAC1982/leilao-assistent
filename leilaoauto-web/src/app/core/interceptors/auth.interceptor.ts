import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const token = authService.token();

  if (!token) {
    return next(request);
  }

  const authorizedRequest = request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authorizedRequest);
};
