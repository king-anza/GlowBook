import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';

import { environment } from '../../../environments/environment';

import { BookPage } from './book.page';

describe('BookPage', () => {
  it('loads server slots and submits a generated idempotency key', async () => {
    await TestBed.configureTestingModule({
      imports: [BookPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ staffId: 'staff-1', offeringId: 'offering-1' }),
              queryParamMap: convertToParamMap({
                business: 'Glow Studio',
                service: 'Silk press',
                professional: 'Customer Test',
                duration: '30',
                price: '350',
              }),
            },
          },
        },
      ],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    const page = TestBed.createComponent(BookPage).componentInstance;
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(9, 0, 0, 0);
    const end = new Date(tomorrow);
    end.setMinutes(end.getMinutes() + 30);
    const dateKey = `${tomorrow.getFullYear()}-${String(tomorrow.getMonth() + 1).padStart(2, '0')}-${String(tomorrow.getDate()).padStart(2, '0')}`;
    const slot = { startUtc: tomorrow.toISOString(), endUtc: end.toISOString() };
    http.expectOne((request) => request.url.endsWith('/api/v1/staff/staff-1/availability')).flush([slot]);
    expect(page.availableDates()).toEqual([dateKey]);
    expect(page.selectedDate()).toBe(dateKey);
    expect(page.slotsForSelectedDate()).toEqual([slot]);

    page.book(slot);

    const booking = http.expectOne(`${environment.apiUrl}/api/v1/bookings`);
    expect(booking.request.body.offeringId).toBe('offering-1');
    expect(booking.request.body.idempotencyKey).toMatch(/^[0-9a-f-]{36}$/i);
    booking.flush({});
    expect(router.navigateByUrl).toHaveBeenCalledWith('/bookings');
    http.verify();
  });
});
