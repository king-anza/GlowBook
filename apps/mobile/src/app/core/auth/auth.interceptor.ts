import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();
  const authenticated = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  return next(authenticated).pipe(
    catchError((error: { status?: number }) => {
      if (error.status !== 401 || !auth.refreshToken() || request.url.includes('/api/v1/auth/refresh'))
        return throwError(() => error);
      return auth.refresh().pipe(
        switchMap((tokens) => next(request.clone({ setHeaders: { Authorization: `Bearer ${tokens.accessToken}` } }))),
        catchError((refreshError) => {
          auth.clearSession();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
