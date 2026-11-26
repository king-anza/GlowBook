import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';

import { ManageSpacesPage } from './manage-spaces.page';

describe('ManageSpacesPage', () => {
  it('creates a workspace opportunity as a draft', async () => {
    await TestBed.configureTestingModule({
      imports: [ManageSpacesPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(ManageSpacesPage).componentInstance;
    http.expectOne(`${environment.apiUrl}/api/v1/professional/spaces`).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/professional/space-applications/received`).flush([]);

    page.form.patchValue({
      title: 'Chair available on Saturdays',
      description: 'A fully equipped styling chair for an independent stylist.',
      startDate: '2026-12-01',
      rateAmount: 250,
    });
    page.save();

    const create = http.expectOne(`${environment.apiUrl}/api/v1/professional/spaces`);
    expect(create.request.method).toBe('POST');
    expect(create.request.body.opportunityType).toBe('ChairRental');
    expect(create.request.body.rateAmount).toBe(250);
    expect(create.request.body.imageUrls).toEqual([]);
    create.flush({});
    http.expectOne(`${environment.apiUrl}/api/v1/professional/spaces`).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/professional/space-applications/received`).flush([]);
    expect(page.message()).toContain('Draft');
    http.verify();
  });

  it('records when an owner views a submitted application', async () => {
    await TestBed.configureTestingModule({
      imports: [ManageSpacesPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(ManageSpacesPage).componentInstance;
    http.expectOne(`${environment.apiUrl}/api/v1/professional/spaces`).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/professional/space-applications/received`).flush([]);
    const application = { id: 'application-1', status: 'Submitted' } as never;

    page.viewApplication(application);

    const viewed = http.expectOne(`${environment.apiUrl}/api/v1/professional/space-applications/application-1/viewed`);
    expect(viewed.request.method).toBe('POST');
    viewed.flush(null);
    http.expectOne(`${environment.apiUrl}/api/v1/professional/spaces`).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/professional/space-applications/received`).flush([]);
    expect(page.selectedApplication()).toBe(application);
    http.verify();
  });
});
