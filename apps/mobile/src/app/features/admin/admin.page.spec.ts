import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';
import { ConfirmationService } from '../../core/ui/confirmation.service';

import { AdminPage } from './admin.page';

describe('AdminPage', () => {
  it('loads operational data and approves a pending professional after confirmation', async () => {
    const confirmation = { confirm: vi.fn().mockResolvedValue(true) };
    await TestBed.configureTestingModule({
      imports: [AdminPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ConfirmationService, useValue: confirmation },
      ],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(AdminPage).componentInstance;
    const business = {
      id: 'business-1',
      tradingName: 'Glow Studio',
      description: 'Inclusive beauty services',
      status: 'PendingReview',
      location: { suburb: 'Rosebank', city: 'Johannesburg', province: 'Gauteng' },
      staff: [],
    };
    http.expectOne(`${environment.apiUrl}/api/v1/admin/businesses/pending`).flush([business]);
    http.expectOne(`${environment.apiUrl}/api/v1/admin/businesses`).flush([]);
    http.expectOne((request) => request.url.endsWith('/api/v1/admin/users')).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/admin/bookings`).flush([]);
    http.expectOne((request) => request.url.endsWith('/api/v1/admin/audit-logs')).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/admin/reviews`).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/admin/portfolio-images`).flush([]);

    await page.approve(business);

    expect(confirmation.confirm).toHaveBeenCalled();
    const approval = http.expectOne(`${environment.apiUrl}/api/v1/admin/businesses/business-1/approve`);
    expect(approval.request.method).toBe('POST');
    approval.flush(null);
    expect(page.pendingBusinesses()).toEqual([]);
    expect(page.message()).toContain('approved');
    http.verify();
  });
});
