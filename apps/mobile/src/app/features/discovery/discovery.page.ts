import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonHeader,
  IonIcon,
  IonInput,
  IonModal,
  IonSelect,
  IonSelectOption,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { EmptyStateComponent } from '../../core/ui/empty-state.component';
import { FeedbackBannerComponent } from '../../core/ui/feedback-banner.component';

interface BusinessSummary {
  id: string;
  tradingName: string;
  description: string;
  city: string;
  suburb: string;
  province: string;
  isDemo: boolean;
  coverImageUrl: string | null;
  fromPrice: number;
  categories: string[];
  averageRating: number;
  reviewCount: number;
  nextAvailableDate: string | null;
  distanceKm: number | null;
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
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    FormsModule,
    RouterLink,
    IonHeader,
    IonIcon,
    IonToolbar,
    IonTitle,
    IonContent,
    IonInput,
    IonModal,
    IonSelect,
    IonSelectOption,
    IonButton,
    EmptyStateComponent,
    FeedbackBannerComponent,
  ],
})
export class DiscoveryPage {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly selected = signal<BusinessSummary[]>([]);
  readonly comparisonOpen = signal(false);
  readonly businesses = signal<BusinessSummary[]>([]);
  readonly sortedBusinesses = computed(() => this.businesses());
  readonly categories = signal<Category[]>([]);
  readonly currentPage = signal(1);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly filterOpen = signal(false);
  readonly locating = signal(false);
  readonly locationMessage = signal<string | null>(null);
  readonly locationCoordinates = signal<{ latitude: number; longitude: number } | null>(null);
  readonly pageSize = 20;
  search = '';
  categoryId = '';
  province = '';
  city = '';
  suburb = '';
  minPrice: number | null = null;
  maxPrice: number | null = null;
  radiusKm = 25;
  sortMode: 'recommended' | 'distance' | 'rating' | 'price' | 'availability' = 'recommended';
  constructor() {
    this.restoreFilters();
    this.http
      .get<Category[]>(`${environment.apiUrl}/api/v1/categories`)
      .subscribe((items) => this.categories.set(items));
    this.load(this.currentPage());
  }
  load(page = 1): void {
    this.loading.set(true);
    this.error.set(null);
    let params = new HttpParams()
      .set('search', this.search)
      .set('sort', this.sortMode)
      .set('page', page)
      .set('pageSize', this.pageSize);
    if (this.categoryId) params = params.set('categoryId', this.categoryId);
    if (this.province) params = params.set('province', this.province);
    if (this.city) params = params.set('city', this.city);
    if (this.suburb) params = params.set('suburb', this.suburb);
    if (this.minPrice !== null) params = params.set('minPrice', this.minPrice);
    if (this.maxPrice !== null) params = params.set('maxPrice', this.maxPrice);
    const coordinates = this.locationCoordinates();
    if (coordinates) {
      params = params
        .set('latitude', coordinates.latitude)
        .set('longitude', coordinates.longitude)
        .set('radiusKm', this.radiusKm);
    }
    this.persistFilters(page);
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

  clearSearch(): void {
    this.search = '';
    this.categoryId = '';
    this.province = '';
    this.city = '';
    this.suburb = '';
    this.minPrice = null;
    this.maxPrice = null;
    this.radiusKm = 25;
    this.sortMode = 'recommended';
    this.filterOpen.set(false);
    this.locationCoordinates.set(null);
    this.locationMessage.set(null);
    this.load();
  }

  useCurrentLocation(): void {
    if (typeof navigator === 'undefined' || !navigator.geolocation) {
      this.locationMessage.set('Location is not supported on this device. Search by city or suburb instead.');
      return;
    }
    this.locating.set(true);
    this.locationMessage.set('Requesting your location…');
    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.locationCoordinates.set({ latitude: position.coords.latitude, longitude: position.coords.longitude });
        this.city = '';
        this.suburb = '';
        this.province = '';
        this.locating.set(false);
        this.locationMessage.set(`Showing salons within ${this.radiusKm} km of your current location.`);
        this.load();
      },
      (error) => {
        this.locating.set(false);
        this.locationMessage.set(
          error.code === error.PERMISSION_DENIED
            ? 'Location permission was declined. Search by city or suburb instead.'
            : 'We could not determine your location. Search by city or suburb instead.',
        );
      },
      { enableHighAccuracy: false, timeout: 10_000, maximumAge: 300_000 },
    );
  }

  clearCurrentLocation(): void {
    this.locationCoordinates.set(null);
    this.locationMessage.set('Current location cleared. You can search by city or suburb.');
    this.load();
  }
  selectCategory(categoryId: string): void {
    this.categoryId = categoryId;
    this.load();
  }

  toggleFilters(): void {
    this.filterOpen.update((open) => !open);
  }

  get activeFilterCount(): number {
    return [
      this.search.trim(),
      this.categoryId,
      this.province,
      this.city.trim(),
      this.suburb.trim(),
      this.minPrice,
      this.maxPrice,
      this.locationCoordinates(),
    ].filter((value) => value !== '' && value !== null && value !== undefined).length;
  }

  applyFilters(): void {
    this.filterOpen.set(false);
    this.load();
  }

  changeSort(): void {
    if (this.sortMode === 'distance' && !this.locationCoordinates()) this.sortMode = 'recommended';
    this.load();
  }

  categoryIcon(name: string): string {
    const category = name.toLowerCase();
    if (category.includes('hair') || category.includes('braid')) return 'cut-outline';
    if (category.includes('nail')) return 'hand-left-outline';
    if (category.includes('makeup')) return 'color-palette-outline';
    if (category.includes('lash') || category.includes('brow')) return 'eye-outline';
    return 'sparkles-outline';
  }

  imageFor(business: BusinessSummary): string {
    const category = business.categories[0]?.toLowerCase() ?? '';
    if (category === 'hair' || category.includes('braid')) return 'braids';
    if (category === 'nails') return 'nails';
    if (category === 'makeup') return 'makeup';
    if (category.includes('lash') || category.includes('brow')) return 'lashes';
    return 'beauty';
  }

  cardImageFor(business: BusinessSummary): string {
    return business.coverImageUrl || `assets/glowbook/${this.imageFor(business)}.webp`;
  }

  hasCustomCover(business: BusinessSummary): boolean {
    return Boolean(business.coverImageUrl);
  }

  isSelected(id: string): boolean {
    return this.selected().some((business) => business.id === id);
  }

  toggleComparison(business: BusinessSummary): void {
    if (this.isSelected(business.id)) {
      this.selected.update((items) => items.filter((item) => item.id !== business.id));
    } else if (this.selected().length < 2) {
      this.selected.update((items) => [...items, business]);
    }
  }

  previousPage(): void {
    if (this.currentPage() > 1) this.load(this.currentPage() - 1);
  }
  nextPage(): void {
    if (this.currentPage() * this.pageSize < this.totalCount()) this.load(this.currentPage() + 1);
  }

  private restoreFilters(): void {
    const params = this.route.snapshot.queryParamMap;
    this.search = params.get('search') ?? '';
    this.categoryId = params.get('categoryId') ?? '';
    this.province = params.get('province') ?? '';
    this.city = params.get('city') ?? '';
    this.suburb = params.get('suburb') ?? '';
    this.minPrice = this.numberParam(params.get('minPrice'));
    this.maxPrice = this.numberParam(params.get('maxPrice'));
    this.radiusKm = this.numberParam(params.get('radiusKm')) ?? 25;
    const sort = params.get('sort');
    if (sort && ['recommended', 'rating', 'price', 'availability'].includes(sort))
      this.sortMode = sort as typeof this.sortMode;
    this.currentPage.set(Math.max(1, this.numberParam(params.get('page')) ?? 1));
  }

  private persistFilters(page: number): void {
    const queryParams = {
      search: this.search.trim() || null,
      categoryId: this.categoryId || null,
      province: this.province || null,
      city: this.city.trim() || null,
      suburb: this.suburb.trim() || null,
      minPrice: this.minPrice,
      maxPrice: this.maxPrice,
      radiusKm: this.locationCoordinates() ? this.radiusKm : null,
      sort: this.sortMode === 'recommended' ? null : this.sortMode,
      page: page > 1 ? page : null,
    };
    void this.router.navigate([], { relativeTo: this.route, queryParams, replaceUrl: true });
  }

  private numberParam(value: string | null): number | null {
    if (!value) return null;
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }
}
