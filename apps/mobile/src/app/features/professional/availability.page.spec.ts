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
    http.verify();
  });
});
