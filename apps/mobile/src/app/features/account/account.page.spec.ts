import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import { ConfirmationService } from '../../core/ui/confirmation.service';

import { AccountPage } from './account.page';

describe('AccountPage', () => {
  it('loads and updates correction and marketing-consent fields', async () => {
    await TestBed.configureTestingModule({
      imports: [AccountPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ConfirmationService, useValue: { confirm: vi.fn().mockResolvedValue(true) } },
      ],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const page = TestBed.createComponent(AccountPage).componentInstance;
    const account = {
      id: 'user-1',
      email: 'old@example.test',
      displayName: 'Old Name',
      marketingConsent: false,
      accountStatus: 'Active',
      createdAtUtc: '2026-11-07T00:00:00Z',
      termsVersion: '1',
      privacyNoticeVersion: '1',
    };
    http.expectOne(`${environment.apiUrl}/api/v1/me/data`).flush(account);
    page.email = 'new@example.test';
    page.displayName = 'New Name';
    page.marketingConsent = true;

    page.save();

    const update = http.expectOne(`${environment.apiUrl}/api/v1/me`);
    expect(update.request.method).toBe('PATCH');
    expect(update.request.body).toEqual({
      email: 'new@example.test',
      displayName: 'New Name',
      marketingConsent: true,
    });
    update.flush({ ...account, email: 'new@example.test', displayName: 'New Name', marketingConsent: true });
    expect(router.navigate).toHaveBeenCalledWith(['/auth/recovery'], {
      queryParams: { email: 'new@example.test' },
    });
    http.verify();
  });
});
