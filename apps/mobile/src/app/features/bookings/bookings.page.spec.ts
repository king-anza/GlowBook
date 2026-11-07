import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';

import { BookingsPage } from './bookings.page';

describe('BookingsPage', () => {
  it('cancels with the current concurrency token and reloads private data', async () => {
    await TestBed.configureTestingModule({
      imports: [BookingsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
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

    page.cancel(booking);

    const cancellation = http.expectOne(`${environment.apiUrl}/api/v1/bookings/booking-1/cancel`);
    expect(cancellation.request.body).toEqual({ reason: null, concurrencyToken: 'version-1' });
    cancellation.flush(null);
    http.expectOne(`${environment.apiUrl}/api/v1/bookings`).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/reviews`).flush([]);
    expect(page.bookings()).toEqual([]);
    http.verify();
  });
});
