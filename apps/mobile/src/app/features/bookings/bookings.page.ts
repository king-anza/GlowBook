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
import { ConfirmationService } from '../../core/ui/confirmation.service';

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
  private readonly confirmation = inject(ConfirmationService);
  readonly bookings = signal<Booking[]>([]);
  readonly reviews = signal<Review[]>([]);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<string | null>(null);
  readonly reviewRatings: Record<string, number> = {};
  readonly reviewComments: Record<string, string> = {};
  constructor() {
    this.load();
  }
  async cancel(booking: Booking): Promise<void> {
    if (
      !(await this.confirmation.confirm(
        'Cancel this booking?',
        'Your appointment will be released and may be booked by someone else.',
        'Cancel booking',
      ))
    )
      return;
    this.busyId.set(booking.id);
    this.message.set(null);
    this.http
      .post(`${environment.apiUrl}/api/v1/bookings/${booking.id}/cancel`, {
        reason: null,
        concurrencyToken: booking.concurrencyToken,
      })
      .subscribe({
        next: () => {
          this.message.set('Booking cancelled.');
          this.busyId.set(null);
          this.load();
        },
        error: () => {
          this.message.set('The booking changed. Refresh and try again.');
          this.busyId.set(null);
        },
      });
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
  async deleteReview(review: Review): Promise<void> {
    if (
      !(await this.confirmation.confirm(
        'Remove your review?',
        'This review will no longer appear on the business profile.',
        'Remove review',
      ))
    )
      return;
    this.http.delete(`${environment.apiUrl}/api/v1/reviews/${review.id}`).subscribe({
      next: () => {
        this.message.set('Review removed.');
        this.loadReviews();
      },
      error: () => this.message.set('The review could not be removed.'),
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
