import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { InsightsPage } from './insights.page';

describe('InsightsPage', () => {
  it('loads aggregated insights and protects small customer cohorts', async () => {
    await TestBed.configureTestingModule({
      imports: [InsightsPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(InsightsPage);
    const page = fixture.componentInstance;
    http
      .expectOne(
        (request) => request.url.endsWith('/api/v1/professional/insights') && request.params.get('periodDays') === '30',
      )
      .flush({
        periodDays: 30,
        from: '2026-10-28',
        to: '2026-11-26',
        bookingVolume: 2,
        completedCount: 1,
        cancellationCount: 1,
        noShowCount: 0,
        repeatCustomerCount: null,
        customerPrivacyThresholdMet: false,
        capacityMinutes: 2400,
        bookedMinutes: 120,
        chairUtilisationPercent: 5,
        daily: [],
      });
    fixture.detectChanges();
    expect(page.insights()?.repeatCustomerCount).toBeNull();
    expect(page.rate(1, 2)).toBe(50);
    expect(fixture.nativeElement.textContent).toContain('Shown once at least 3 customers');
    http.verify();
  });
});
