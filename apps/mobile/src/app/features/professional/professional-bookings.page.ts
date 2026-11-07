import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { ConfirmationService } from '../../core/ui/confirmation.service';

interface Booking {
  id: string;
  serviceName: string;
  staffName: string;
  startUtc: string;
  status: string;
  concurrencyToken: string;
}

@Component({
  selector: 'app-professional-bookings',
  templateUrl: './professional-bookings.page.html',
  imports: [DatePipe, RouterLink, IonHeader, IonToolbar, IonTitle, IonContent, IonButton],
})
export class ProfessionalBookingsPage {
  private readonly http = inject(HttpClient);
  private readonly confirmation = inject(ConfirmationService);
  readonly bookings = signal<Booking[]>([]);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<string | null>(null);
  constructor() {
    this.load();
  }
  async act(booking: Booking, action: string): Promise<void> {
    if (['reject', 'cancel', 'no-show'].includes(action)) {
      const labels: Record<string, string> = {
        reject: 'Reject booking',
        cancel: 'Cancel booking',
        'no-show': 'Mark no-show',
      };
      if (
        !(await this.confirmation.confirm(
          `${labels[action]}?`,
          'This status change is recorded and the client will be notified.',
          labels[action],
        ))
      )
        return;
    }
    this.busyId.set(booking.id);
    this.message.set(null);
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/bookings/${booking.id}/${action}`, {
        reason: null,
        concurrencyToken: booking.concurrencyToken,
      })
      .subscribe({
        next: () => {
          this.message.set(`Booking ${action}ed.`);
          this.busyId.set(null);
          this.load();
        },
        error: () => {
          this.message.set('The booking changed. Refresh and try again.');
          this.busyId.set(null);
        },
      });
  }
  private load(): void {
    this.http
      .get<Booking[]>(`${environment.apiUrl}/api/v1/professional/bookings`)
      .subscribe((items) => this.bookings.set(items));
  }
}
