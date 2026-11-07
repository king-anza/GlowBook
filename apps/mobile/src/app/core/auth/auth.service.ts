import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, shareReplay, tap, type Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokens = signal<AuthTokens | null>(null);
  private refreshRequest?: Observable<AuthTokens>;
  readonly isAuthenticated: () => boolean = () => this.tokens() !== null;

  register(email: string, password: string, displayName: string, professional = false): Observable<void> {
    const kind = professional ? 'professional' : 'customer';
    return this.http.post<void>(`${environment.apiUrl}/api/v1/auth/register/${kind}`, {
      email,
      password,
      displayName,
      acceptTerms: true,
      confirmMinimumAge: true,
    });
  }

  login(email: string, password: string): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${environment.apiUrl}/api/v1/auth/login`, { email, password });
  }

  setSession(tokens: AuthTokens): void {
    this.tokens.set(tokens);
  }
  clearSession(): void {
    this.tokens.set(null);
  }
  refresh(): Observable<AuthTokens> {
    if (this.refreshRequest) return this.refreshRequest;
    this.refreshRequest = this.http
      .post<AuthTokens>(`${environment.apiUrl}/api/v1/auth/refresh`, { refreshToken: this.tokens()?.refreshToken })
      .pipe(
        tap((tokens) => this.setSession(tokens)),
        finalize(() => (this.refreshRequest = undefined)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    return this.refreshRequest;
  }
  logout(): Observable<void> {
    return this.http
      .post<void>(`${environment.apiUrl}/api/v1/auth/logout`, { refreshToken: this.tokens()?.refreshToken })
      .pipe(tap(() => this.clearSession()));
  }
  refreshToken(): string | null {
    return this.tokens()?.refreshToken ?? null;
  }
  accessToken(): string | null {
    return this.tokens()?.accessToken ?? null;
  }
}
