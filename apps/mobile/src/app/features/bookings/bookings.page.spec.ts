import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { ConfirmationService } from '../../core/ui/confirmation.service';

import { BookingsPage } from './bookings.page';

describe('BookingsPage', () => {
  it('cancels with the current concurrency token and reloads private data', async () => {
    const confirmation = { confirm: vi.fn().mockResolvedValue(true) };
    await TestBed.configureTestingModule({
      imports: [BookingsPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ConfirmationService, useValue: confirmation },
      ],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(BookingsPage).componentInstance;
    const booking = {
      id: 'booking-1',
      businessName: 'Glow Studio',
      serviceName: 'Silk press',
      staffName: 'Lerato',
      startUtc: '2026-11-10T07:00:00Z',
      status: 'Confirmed',
      concurrencyToken: 'version-1',
    };
    http.expectOne(`${environment.apiUrl}/api/v1/bookings`).flush([booking]);
    http.expectOne(`${environment.apiUrl}/api/v1/reviews`).flush([]);

    await page.cancel(booking);

    expect(confirmation.confirm).toHaveBeenCalledWith('Cancel this booking?', expect.any(String), 'Cancel booking');

    const cancellation = http.expectOne(`${environment.apiUrl}/api/v1/bookings/booking-1/cancel`);
    expect(cancellation.request.body).toEqual({ reason: null, concurrencyToken: 'version-1' });
    cancellation.flush(null);
    http.expectOne(`${environment.apiUrl}/api/v1/bookings`).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/reviews`).flush([]);
    expect(page.bookings()).toEqual([]);
    http.verify();
  });
});
