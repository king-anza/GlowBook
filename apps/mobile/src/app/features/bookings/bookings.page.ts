import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IonButton, IonContent, IonSelect, IonSelectOption, IonTextarea } from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { NetworkStatusService } from '../../core/network/network-status.service';
import { ConfirmationService } from '../../core/ui/confirmation.service';
import { EmptyStateComponent } from '../../core/ui/empty-state.component';
import { FeedbackBannerComponent } from '../../core/ui/feedback-banner.component';

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
    IonContent,
    IonButton,
    IonSelect,
    IonSelectOption,
    IonTextarea,
    EmptyStateComponent,
    FeedbackBannerComponent,
  ],
})
export class BookingsPage {
  private readonly http = inject(HttpClient);
  private readonly confirmation = inject(ConfirmationService);
  private readonly route = inject(ActivatedRoute);
  private readonly network = inject(NetworkStatusService);
  readonly highlightedId = this.route.snapshot.queryParamMap.get('booking');
  readonly bookings = signal<Booking[]>([]);
  readonly loading = signal(true);
  readonly reviews = signal<Review[]>([]);
  readonly message = signal<string | null>(null);
  readonly error = signal<string | null>(null);
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
    this.message.set(null);
    this.error.set(null);
    if (!this.startAction(booking.id)) return;
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
          this.error.set('The booking changed. Refresh and try again.');
          this.busyId.set(null);
        },
      });
  }
  review(booking: Booking): void {
    if (!this.startAction(booking.id)) return;
    this.http
      .post(`${environment.apiUrl}/api/v1/bookings/${booking.id}/review`, {
        rating: this.reviewRatings[booking.id] ?? 5,
        comment: this.reviewComments[booking.id] || null,
      })
      .subscribe({
        next: () => {
          this.message.set('Thank you for your review.');
          this.busyId.set(null);
          this.loadReviews();
        },
        error: () => {
          this.error.set('This booking cannot be reviewed. Your comment is preserved.');
          this.busyId.set(null);
        },
      });
  }
  reviewFor(bookingId: string): Review | undefined {
    return this.reviews().find((item) => item.bookingId === bookingId);
  }
  updateReview(review: Review): void {
    if (!this.startAction(review.id)) return;
    this.http
      .patch<Review>(`${environment.apiUrl}/api/v1/reviews/${review.id}`, {
        rating: this.reviewRatings[review.id] ?? review.rating,
        comment: this.reviewComments[review.id] ?? review.comment,
        concurrencyToken: review.concurrencyToken,
      })
      .subscribe({
        next: () => {
          this.message.set('Review updated.');
          this.busyId.set(null);
          this.loadReviews();
        },
        error: () => {
          this.error.set('The review changed. Your edits are preserved; refresh and try again.');
          this.busyId.set(null);
        },
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
    if (!this.startAction(review.id)) return;
    this.http.delete(`${environment.apiUrl}/api/v1/reviews/${review.id}`).subscribe({
      next: () => {
        this.message.set('Review removed.');
        this.busyId.set(null);
        this.loadReviews();
      },
      error: () => {
        this.error.set('The review could not be removed.');
        this.busyId.set(null);
      },
    });
  }
  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<Booking[]>(`${environment.apiUrl}/api/v1/bookings`).subscribe({
      next: (items) => {
        this.bookings.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Your bookings could not be loaded. Please try again.');
        this.loading.set(false);
      },
    });
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
  private startAction(id: string): boolean {
    if (this.busyId()) return false;
    if (!this.network.online()) {
      this.error.set('Reconnect before making this change. It was not queued or submitted.');
      return false;
    }
    this.busyId.set(id);
    this.error.set(null);
    return true;
  }
}
