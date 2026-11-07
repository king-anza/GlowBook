import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, firstValueFrom, shareReplay, tap, type Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

import { SessionVaultService } from './session-vault.service';

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly vault = inject(SessionVaultService);
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
    void this.vault.saveRefreshToken(tokens.refreshToken).catch(() => undefined);
  }
  clearSession(): void {
    this.tokens.set(null);
    void this.vault.clear().catch(() => undefined);
  }
  async restoreSession(): Promise<void> {
    const refreshToken = await this.vault.loadRefreshToken().catch(() => null);
    if (!refreshToken) return;
    try {
      const tokens = await firstValueFrom(
        this.http.post<AuthTokens>(`${environment.apiUrl}/api/v1/auth/refresh`, { refreshToken }),
      );
      this.setSession(tokens);
    } catch (error) {
      if (error instanceof HttpErrorResponse && [400, 401].includes(error.status)) this.clearSession();
    }
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
