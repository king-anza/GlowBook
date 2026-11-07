import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardTitle,
  IonContent,
  IonHeader,
  IonInput,
  IonSelect,
  IonSelectOption,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { environment } from '../../../environments/environment';

interface BusinessSummary {
  id: string;
  tradingName: string;
  description: string;
  city: string;
  suburb: string;
  province: string;
  fromPrice: number;
  categories: string[];
}
interface Page {
  items: BusinessSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}
interface Category {
  id: string;
  name: string;
}

@Component({
  selector: 'app-discovery',
  templateUrl: './discovery.page.html',
  imports: [
    FormsModule,
    RouterLink,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonInput,
    IonSelect,
    IonSelectOption,
    IonButton,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
  ],
})
export class DiscoveryPage {
  private readonly http = inject(HttpClient);
  readonly businesses = signal<BusinessSummary[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly currentPage = signal(1);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly pageSize = 20;
  search = '';
  categoryId = '';
  province = '';
  city = '';
  suburb = '';
  minPrice: number | null = null;
  maxPrice: number | null = null;
  constructor() {
    this.http
      .get<Category[]>(`${environment.apiUrl}/api/v1/categories`)
      .subscribe((items) => this.categories.set(items));
    this.load();
  }
  load(page = 1): void {
    this.loading.set(true);
    this.error.set(null);
    let params = new HttpParams().set('search', this.search).set('page', page).set('pageSize', this.pageSize);
    if (this.categoryId) params = params.set('categoryId', this.categoryId);
    if (this.province) params = params.set('province', this.province);
    if (this.city) params = params.set('city', this.city);
    if (this.suburb) params = params.set('suburb', this.suburb);
    if (this.minPrice !== null) params = params.set('minPrice', this.minPrice);
    if (this.maxPrice !== null) params = params.set('maxPrice', this.maxPrice);
    this.http.get<Page>(`${environment.apiUrl}/api/v1/businesses`, { params }).subscribe({
      next: (result) => {
        this.businesses.set(result.items);
        this.currentPage.set(result.page);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('We could not load beauty professionals right now. Please try again.');
        this.loading.set(false);
      },
    });
  }
  previousPage(): void {
    if (this.currentPage() > 1) this.load(this.currentPage() - 1);
  }
  nextPage(): void {
    if (this.currentPage() * this.pageSize < this.totalCount()) this.load(this.currentPage() + 1);
  }
}
