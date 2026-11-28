import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';

import { RecoveryPage } from './recovery.page';

describe('RecoveryPage', () => {
  it('validates email and suppresses duplicate recovery requests', async () => {
    await TestBed.configureTestingModule({
      imports: [RecoveryPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(RecoveryPage).componentInstance;
    page.requestReset();
    expect(page.message()).toContain('complete email address');
    page.email = 'customer@example.test';
    page.requestReset();
    page.requestReset();
    const request = http.expectOne(`${environment.apiUrl}/api/v1/auth/forgot-password`);
    request.flush(null);
    expect(page.message()).toContain('instructions have been sent');
    http.verify();
  });
});
