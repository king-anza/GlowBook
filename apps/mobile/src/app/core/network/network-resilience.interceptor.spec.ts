import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { networkResilienceInterceptor } from './network-resilience.interceptor';
import { NetworkStatusService } from './network-status.service';

describe('networkResilienceInterceptor', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([networkResilienceInterceptor])),
        provideHttpClientTesting(),
        NetworkStatusService,
      ],
    }),
  );

  it('serves session-cached booking details after safe read retries fail', async () => {
    const client = TestBed.inject(HttpTestingController);
    const http = TestBed.inject(HttpClient);
    const url = '/api/v1/bookings';
    const initial = firstValueFrom(http.get<{ id: string }[]>(url));
    client.expectOne(url).flush([{ id: 'booking-1' }]);
    await expect(initial).resolves.toEqual([{ id: 'booking-1' }]);

    const offline = firstValueFrom(http.get<{ id: string }[]>(url));
    client.expectOne(url).error(new ProgressEvent('offline'), { status: 0 });
    await new Promise((resolve) => setTimeout(resolve, 700));
    client.expectOne(url).error(new ProgressEvent('offline'), { status: 0 });

    await expect(offline).resolves.toEqual([{ id: 'booking-1' }]);
    expect(TestBed.inject(NetworkStatusService).usingCachedData()).toBe(true);
    client.verify();
  });

  it('never retries transactional requests', async () => {
    const client = TestBed.inject(HttpTestingController);
    const http = TestBed.inject(HttpClient);
    const request = firstValueFrom(http.post('/api/v1/bookings', {}));
    client.expectOne('/api/v1/bookings').error(new ProgressEvent('offline'), { status: 0 });
    await expect(request).rejects.toBeTruthy();
    client.verify();
  });
});
