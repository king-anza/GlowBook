import type { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import type { Observable } from 'rxjs';
import { catchError, of, retry, tap, throwError, timer } from 'rxjs';

import { NetworkStatusService } from './network-status.service';

const bookingCache = new Map<string, HttpResponse<unknown>>();

export const networkResilienceInterceptor: HttpInterceptorFn = (
  request: HttpRequest<unknown>,
  next: HttpHandlerFn,
): Observable<HttpEvent<unknown>> => {
  if (request.method !== 'GET') return next(request);
  const network = inject(NetworkStatusService);
  const cacheable = isBookingDetailsRequest(request.url);
  const cacheKey = request.urlWithParams;

  return next(request).pipe(
    retry({
      count: 1,
      delay: (error: HttpErrorResponse) =>
        error.status === 0 || error.status >= 500 ? timer(650) : throwError(() => error),
    }),
    tap((event) => {
      if (event instanceof HttpResponse) {
        if (cacheable) bookingCache.set(cacheKey, event.clone());
        network.markCachedData(false);
      }
    }),
    catchError((error: HttpErrorResponse) => {
      const cached = cacheable ? bookingCache.get(cacheKey) : undefined;
      if (cached && (error.status === 0 || !network.online())) {
        network.markCachedData(true);
        return of(cached.clone({ headers: cached.headers.set('X-Glow-Cache', 'session') }));
      }
      return throwError(() => error);
    }),
  );
};

function isBookingDetailsRequest(url: string): boolean {
  const path = new URL(url, 'https://glow.local').pathname;
  return path === '/api/v1/bookings' || path === '/api/v1/professional/bookings';
}
