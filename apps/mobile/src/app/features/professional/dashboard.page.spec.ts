import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';

import { DashboardPage } from './dashboard.page';

describe('DashboardPage', () => {
  it('loads the professional daily summary and calculates setup progress', async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(DashboardPage).componentInstance;
    http.expectOne(`${environment.apiUrl}/api/v1/professional/dashboard`).flush({
      approvalStatus: 'PendingReview',
      reviewReason: null,
      setupCompletedCount: 8,
      setupTotalCount: 9,
      todayBookingCount: 1,
      pendingBookingCount: 2,
      activeStaffCount: 3,
      staffScheduledTodayCount: 2,
      publishedSpaceCount: 1,
      pendingSpaceApplicationCount: 4,
      todayBookings: [
        {
          id: 'booking-1',
          customerName: 'Lerato',
          staffName: 'Nomsa',
          serviceName: 'Braids',
          startUtc: '2026-11-26T07:00:00Z',
          endUtc: '2026-11-26T09:00:00Z',
          status: 'Confirmed',
        },
      ],
    });
    expect(page.setupProgress()).toBe(89);
    expect(page.summary()?.pendingBookingCount).toBe(2);
    expect(page.loading()).toBe(false);
    http.verify();
  });
});
