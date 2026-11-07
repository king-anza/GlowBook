import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';

import { AuthService, type AuthTokens } from './auth.service';
import { SessionVaultService } from './session-vault.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  let vault: {
    loadRefreshToken: ReturnType<typeof vi.fn>;
    saveRefreshToken: ReturnType<typeof vi.fn>;
    clear: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    vault = {
      loadRefreshToken: vi.fn().mockResolvedValue(null),
      saveRefreshToken: vi.fn().mockResolvedValue(undefined),
      clear: vi.fn().mockResolvedValue(undefined),
    };
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: SessionVaultService, useValue: vault }],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('registers with terms and minimum-age attestations', () => {
    service.register('customer@example.test', 'ValidPass!1', 'Customer').subscribe();
    const request = http.expectOne(`${environment.apiUrl}/api/v1/auth/register/customer`);
    expect(request.request.body).toMatchObject({ acceptTerms: true, confirmMinimumAge: true });
    request.flush(null);
  });

  it('shares a single refresh rotation across concurrent callers', () => {
    const current: AuthTokens = {
      accessToken: 'old',
      refreshToken: 'refresh-one',
      accessTokenExpiresAtUtc: '2026-11-07T10:00:00Z',
    };
    const rotated: AuthTokens = {
      accessToken: 'new',
      refreshToken: 'refresh-two',
      accessTokenExpiresAtUtc: '2026-11-07T10:15:00Z',
    };
    service.setSession(current);
    service.refresh().subscribe();
    service.refresh().subscribe();
    const request = http.expectOne(`${environment.apiUrl}/api/v1/auth/refresh`);
    expect(request.request.body).toEqual({ refreshToken: 'refresh-one' });
    request.flush(rotated);
    expect(service.accessToken()).toBe('new');
    expect(vault.saveRefreshToken).toHaveBeenCalledWith('refresh-two');
  });

  it('revokes the refresh token on logout and clears memory', () => {
    service.setSession({
      accessToken: 'access',
      refreshToken: 'refresh',
      accessTokenExpiresAtUtc: '2026-11-07T10:00:00Z',
    });
    service.logout().subscribe();
    const request = http.expectOne(`${environment.apiUrl}/api/v1/auth/logout`);
    expect(request.request.body).toEqual({ refreshToken: 'refresh' });
    request.flush(null);
    expect(service.isAuthenticated()).toBe(false);
    expect(vault.clear).toHaveBeenCalled();
  });

  it('restores a native session by rotating the securely stored refresh token', async () => {
    vault.loadRefreshToken.mockResolvedValue('stored-refresh');
    const restored = service.restoreSession();
    await new Promise((resolve) => setTimeout(resolve, 0));
    const request = http.expectOne(`${environment.apiUrl}/api/v1/auth/refresh`);
    expect(request.request.body).toEqual({ refreshToken: 'stored-refresh' });
    request.flush({
      accessToken: 'restored-access',
      refreshToken: 'rotated-refresh',
      accessTokenExpiresAtUtc: '2026-11-07T10:15:00Z',
    });
    await restored;

    expect(service.accessToken()).toBe('restored-access');
    expect(vault.saveRefreshToken).toHaveBeenCalledWith('rotated-refresh');
  });
});
