import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { IonButton, IonContent, IonHeader, IonItem, IonList, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';

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
  imports: [DatePipe, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonItem, IonButton],
})
export class ProfessionalBookingsPage {
  private readonly http = inject(HttpClient);
  readonly bookings = signal<Booking[]>([]);
  readonly message = signal<string | null>(null);
  constructor() {
    this.load();
  }
  act(booking: Booking, action: string): void {
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/bookings/${booking.id}/${action}`, {
        reason: null,
        concurrencyToken: booking.concurrencyToken,
      })
      .subscribe({
        next: () => {
          this.message.set(`Booking ${action}ed.`);
          this.load();
        },
        error: () => this.message.set('The booking changed. Refresh and try again.'),
      });
  }
  private load(): void {
    this.http
      .get<Booking[]>(`${environment.apiUrl}/api/v1/professional/bookings`)
      .subscribe((items) => this.bookings.set(items));
  }
}
