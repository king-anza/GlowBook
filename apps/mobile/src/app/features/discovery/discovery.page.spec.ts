import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';

import { DiscoveryPage } from './discovery.page';

describe('DiscoveryPage', () => {
  it('loads categories and sends the selected discovery filters', async () => {
    await TestBed.configureTestingModule({
      imports: [DiscoveryPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(DiscoveryPage);
    const page = fixture.componentInstance;
    http.expectOne(`${environment.apiUrl}/api/v1/categories`).flush([{ id: 'category-id', name: 'Hair' }]);
    http
      .expectOne((request) => request.url.endsWith('/api/v1/businesses'))
      .flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
    page.search = 'Glow';
    page.categoryId = 'category-id';
    page.province = 'Gauteng';
    page.city = 'Johannesburg';
    page.suburb = 'Sandton';
    page.minPrice = 100;
    page.maxPrice = 500;
    page.load();
    const request = http.expectOne(
      (candidate) =>
        candidate.url.endsWith('/api/v1/businesses') && candidate.params.get('categoryId') === 'category-id',
    );
    expect(request.request.params.get('province')).toBe('Gauteng');
    expect(request.request.params.get('minPrice')).toBe('100');
    request.flush({ items: [], page: 1, pageSize: 20, totalCount: 21 });
    page.nextPage();
    const next = http.expectOne((candidate) => candidate.params.get('page') === '2');
    next.flush({ items: [], page: 2, pageSize: 20, totalCount: 21 });
    expect(page.currentPage()).toBe(2);
    http.verify();
  });
});
