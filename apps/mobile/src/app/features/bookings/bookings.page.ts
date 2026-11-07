import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonButton,
  IonContent,
  IonHeader,
  IonSelect,
  IonSelectOption,
  IonTextarea,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { environment } from '../../../environments/environment';

interface Booking {
  id: string;
  businessName: string;
  serviceName: string;
  staffName: string;
  startUtc: string;
  status: string;
  concurrencyToken: string;
}
interface Review {
  id: string;
  bookingId: string;
  rating: number;
  comment: string | null;
  concurrencyToken: string;
}

@Component({
  selector: 'app-bookings',
  templateUrl: './bookings.page.html',
  imports: [
    DatePipe,
    FormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonButton,
    IonSelect,
    IonSelectOption,
    IonTextarea,
  ],
})
export class BookingsPage {
  private readonly http = inject(HttpClient);
  readonly bookings = signal<Booking[]>([]);
  readonly reviews = signal<Review[]>([]);
  readonly message = signal<string | null>(null);
  readonly reviewRatings: Record<string, number> = {};
  readonly reviewComments: Record<string, string> = {};
  constructor() {
    this.load();
  }
  cancel(booking: Booking): void {
    this.http
      .post(`${environment.apiUrl}/api/v1/bookings/${booking.id}/cancel`, {
        reason: null,
        concurrencyToken: booking.concurrencyToken,
      })
      .subscribe(() => this.load());
  }
  review(booking: Booking): void {
    this.http
      .post(`${environment.apiUrl}/api/v1/bookings/${booking.id}/review`, {
        rating: this.reviewRatings[booking.id] ?? 5,
        comment: this.reviewComments[booking.id] || null,
      })
      .subscribe({
        next: () => {
          this.message.set('Thank you for your review.');
          this.loadReviews();
        },
        error: () => this.message.set('This booking cannot be reviewed.'),
      });
  }
  reviewFor(bookingId: string): Review | undefined {
    return this.reviews().find((item) => item.bookingId === bookingId);
  }
  updateReview(review: Review): void {
    this.http
      .patch<Review>(`${environment.apiUrl}/api/v1/reviews/${review.id}`, {
        rating: this.reviewRatings[review.id] ?? review.rating,
        comment: this.reviewComments[review.id] ?? review.comment,
        concurrencyToken: review.concurrencyToken,
      })
      .subscribe({
        next: () => {
          this.message.set('Review updated.');
          this.loadReviews();
        },
        error: () => this.message.set('The review changed. Refresh and try again.'),
      });
  }
  deleteReview(review: Review): void {
    this.http.delete(`${environment.apiUrl}/api/v1/reviews/${review.id}`).subscribe(() => {
      this.message.set('Review removed.');
      this.loadReviews();
    });
  }
  private load(): void {
    this.http.get<Booking[]>(`${environment.apiUrl}/api/v1/bookings`).subscribe((items) => this.bookings.set(items));
    this.loadReviews();
  }
  private loadReviews(): void {
    this.http.get<Review[]>(`${environment.apiUrl}/api/v1/reviews`).subscribe((items) => {
      this.reviews.set(items);
      for (const item of items) {
        this.reviewRatings[item.id] = item.rating;
        this.reviewComments[item.id] = item.comment ?? '';
      }
    });
  }
}
