import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';

import { OnboardingPage } from './onboarding.page';

describe('OnboardingPage', () => {
  it('calculates progress and submits only after the setup is complete', async () => {
    await TestBed.configureTestingModule({
      imports: [OnboardingPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(OnboardingPage).componentInstance;
    const ready = {
      accountVerified: true,
      businessComplete: true,
      locationComplete: true,
      portfolioComplete: true,
      staffComplete: true,
      servicesComplete: true,
      pricingComplete: true,
      availabilityComplete: true,
      approvalSubmitted: false,
      approvalStatus: 'Draft',
      reviewReason: null,
    };
    http.expectOne(`${environment.apiUrl}/api/v1/professional/onboarding`).flush(ready);
    expect(page.completedCount()).toBe(8);
    expect(page.readyToSubmit()).toBe(true);

    page.submitForReview();
    const submit = http.expectOne(`${environment.apiUrl}/api/v1/professional/business/submit`);
    expect(submit.request.method).toBe('POST');
    submit.flush(null);
    http.expectOne(`${environment.apiUrl}/api/v1/professional/onboarding`).flush({
      ...ready,
      approvalSubmitted: true,
      approvalStatus: 'PendingReview',
    });
    expect(page.completedCount()).toBe(9);
    http.verify();
  });
});
