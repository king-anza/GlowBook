import { DecimalPipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardTitle,
  IonContent,
  IonHeader,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { environment } from '../../../environments/environment';

interface Offering {
  id: string;
  staffId: string;
  staffName: string;
  price: number;
  durationMinutes: number;
}
interface Service {
  id: string;
  category: string;
  name: string;
  description: string;
  offerings: Offering[];
}
interface Business {
  tradingName: string;
  description: string;
  addressLine1: string;
  suburb: string;
  city: string;
  province: string;
  services: Service[];
  portfolioImages: { id: string; imageUrl: string; altText: string }[];
}
interface Reviews {
  averageRating: number;
  reviewCount: number;
  reviews: { id: string; rating: number; comment: string | null }[];
}

@Component({
  selector: 'app-business-detail',
  templateUrl: './business-detail.page.html',
  imports: [
    DecimalPipe,
    RouterLink,
    IonButton,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
  ],
})
export class BusinessDetailPage {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly businessId = this.route.snapshot.paramMap.get('id')!;
  readonly business = signal<Business | null>(null);
  readonly reviews = signal<Reviews | null>(null);
  readonly reviewPage = signal(1);
  readonly reviewPageSize = 20;
  constructor() {
    this.http
      .get<Business>(`${environment.apiUrl}/api/v1/businesses/${this.businessId}`)
      .subscribe((value) => this.business.set(value));
    this.loadReviews();
  }
  loadReviews(page = 1): void {
    const params = new HttpParams().set('page', page).set('pageSize', this.reviewPageSize);
    this.http
      .get<Reviews>(`${environment.apiUrl}/api/v1/businesses/${this.businessId}/reviews`, { params })
      .subscribe((value) => {
        this.reviews.set(value);
        this.reviewPage.set(page);
      });
  }
  previousReviewPage(): void {
    if (this.reviewPage() > 1) this.loadReviews(this.reviewPage() - 1);
  }
  nextReviewPage(): void {
    if (this.reviewPage() * this.reviewPageSize < (this.reviews()?.reviewCount ?? 0))
      this.loadReviews(this.reviewPage() + 1);
  }
}
