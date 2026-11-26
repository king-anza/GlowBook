import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';

import { AvailabilityPage } from './availability.page';

describe('AvailabilityPage', () => {
  it('rejects an invalid interval and saves accumulated weekly intervals', async () => {
    await TestBed.configureTestingModule({
      imports: [AvailabilityPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(AvailabilityPage).componentInstance;
    http.expectOne(`${environment.apiUrl}/api/v1/professional/business`).flush({
      staff: [{ id: 'staff-1', displayName: 'Stylist' }],
    });
    http
      .expectOne((request) => request.url.endsWith('/api/v1/professional/calendar'))
      .flush({
        from: '2026-11-26',
        to: '2026-12-02',
        timeZone: 'Africa/Johannesburg',
        staff: [],
        events: [],
        days: [],
      });
    page.form.setValue({ staffId: 'staff-1', dayOfWeek: 1, startTime: '17:00', endTime: '09:00' });
    page.addWeeklyRow();
    expect(page.weeklyRows()).toHaveLength(0);
    expect(page.message()).toContain('before');

    page.form.patchValue({ startTime: '09:00', endTime: '17:00' });
    page.addWeeklyRow();
    page.save();

    const save = http.expectOne(`${environment.apiUrl}/api/v1/professional/staff/staff-1/weekly-availability`);
    expect(save.request.method).toBe('PUT');
    expect(save.request.body).toEqual([{ dayOfWeek: 1, startTime: '09:00:00', endTime: '17:00:00' }]);
    save.flush({});
    expect(page.message()).toBe('Weekly availability saved.');
    http
      .expectOne((request) => request.url.endsWith('/api/v1/professional/calendar'))
      .flush({
        from: '2026-11-26',
        to: '2026-12-02',
        timeZone: 'Africa/Johannesburg',
        staff: [],
        events: [],
        days: [],
      });
    http.verify();
  });

  it('explains a blocked-time conflict without adding it to the calendar', async () => {
    await TestBed.configureTestingModule({
      imports: [AvailabilityPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(AvailabilityPage).componentInstance;
    http.expectOne(`${environment.apiUrl}/api/v1/professional/business`).flush({
      staff: [{ id: 'staff-1', displayName: 'Stylist' }],
    });
    http
      .expectOne((request) => request.url.endsWith('/api/v1/professional/calendar'))
      .flush({
        from: '2026-11-26',
        to: '2026-12-02',
        timeZone: 'Africa/Johannesburg',
        staff: [],
        events: [],
        days: [],
      });
    page.blockedForm.setValue({
      staffId: 'staff-1',
      startUtc: '2026-11-26T10:00',
      endUtc: '2026-11-26T11:00',
      reason: 'Training',
    });
    page.addBlockedTime();
    const request = http.expectOne(`${environment.apiUrl}/api/v1/professional/staff/staff-1/blocked-times`);
    expect(request.request.body.startUtc).toBe('2026-11-26T08:00:00.000Z');
    request.flush({ detail: 'Conflict' }, { status: 409, statusText: 'Conflict' });
    expect(page.message()).toContain('overlaps');
    expect(page.messageIsError()).toBe(true);
    http.verify();
  });
});
