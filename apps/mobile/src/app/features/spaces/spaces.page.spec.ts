import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';

import { SpacesPage } from './spaces.page';

describe('SpacesPage', () => {
  it('keeps the loading state until opportunities arrive', async () => {
    await TestBed.configureTestingModule({
      imports: [SpacesPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(SpacesPage).componentInstance;

    expect(page.loading()).toBe(true);
    http.expectOne(`${environment.apiUrl}/api/v1/spaces`).flush([]);

    expect(page.loading()).toBe(false);
    expect(page.opportunities()).toEqual([]);
    http.verify();
  });

  it('ends loading and exposes a useful error when opportunities fail', async () => {
    await TestBed.configureTestingModule({
      imports: [SpacesPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(SpacesPage).componentInstance;

    http.expectOne(`${environment.apiUrl}/api/v1/spaces`).flush('Unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    });

    expect(page.loading()).toBe(false);
    expect(page.error()).toContain('could not be loaded');
    http.verify();
  });

  it('persists a saved opportunity and updates the card state', async () => {
    await TestBed.configureTestingModule({
      imports: [SpacesPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const page = TestBed.createComponent(SpacesPage).componentInstance;
    http.expectOne(`${environment.apiUrl}/api/v1/spaces`).flush([]);
    const opportunity = { id: 'space-1', isSaved: false } as never;
    page.opportunities.set([opportunity]);

    page.toggleSaved(opportunity);
    const save = http.expectOne(`${environment.apiUrl}/api/v1/professional/spaces/space-1/saved`);
    expect(save.request.method).toBe('PUT');
    save.flush(null);

    expect(page.opportunities()[0].isSaved).toBe(true);
    http.verify();
  });
});
