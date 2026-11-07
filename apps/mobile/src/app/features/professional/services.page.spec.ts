import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';

import { ServicesPage } from './services.page';

describe('ServicesPage', () => {
  it('submits server-authoritative offering fields without putting the service id in the body', async () => {
    await TestBed.configureTestingModule({
      imports: [ServicesPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(ServicesPage).componentInstance;
    http.expectOne(`${environment.apiUrl}/api/v1/categories`).flush([]);
    http.expectOne(`${environment.apiUrl}/api/v1/professional/business`).flush({ staff: [] });
    http.expectOne(`${environment.apiUrl}/api/v1/professional/services`).flush([]);
    page.offeringForm.setValue({
      serviceId: 'service-1',
      staffMemberId: 'staff-1',
      price: 250,
      durationMinutes: 60,
      bufferMinutes: 15,
    });

    page.addOffering();

    const offering = http.expectOne(`${environment.apiUrl}/api/v1/professional/services/service-1/offerings`);
    expect(offering.request.body).toEqual({
      staffMemberId: 'staff-1',
      price: 250,
      durationMinutes: 60,
      bufferMinutes: 15,
    });
    offering.flush({});
    http.expectOne(`${environment.apiUrl}/api/v1/professional/services`).flush([]);
    expect(page.message()).toBe('Price and duration added.');
    http.verify();
  });
});
