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
          useValue: { snapshot: { paramMap: convertToParamMap({ staffId: 'staff-1', offeringId: 'offering-1' }) } },
        },
      ],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    const page = TestBed.createComponent(BookPage).componentInstance;
    const slot = { startUtc: '2026-11-10T07:00:00Z', endUtc: '2026-11-10T07:30:00Z' };
    http.expectOne((request) => request.url.endsWith('/api/v1/staff/staff-1/availability')).flush([slot]);
    expect(page.availableDates()).toEqual(['2026-11-10']);
    expect(page.selectedDate()).toBe('2026-11-10');
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
