import { DatePipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';

interface Slot {
  startUtc: string;
  endUtc: string;
}

@Component({
  selector: 'app-book',
  templateUrl: './book.page.html',
  imports: [DatePipe, IonHeader, IonToolbar, IonTitle, IonContent, IonButton],
})
export class BookPage {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly staffId = this.route.snapshot.paramMap.get('staffId')!;
  private readonly offeringId = this.route.snapshot.paramMap.get('offeringId')!;

  readonly slots = signal<Slot[]>([]);
  readonly error = signal<string | null>(null);

  constructor() {
    const from = new Date();
    from.setDate(from.getDate() + 1);
    const to = new Date(from);
    to.setDate(to.getDate() + 6);
    const params = new HttpParams()
      .set('offeringId', this.offeringId)
      .set('from', from.toISOString().slice(0, 10))
      .set('to', to.toISOString().slice(0, 10));

    this.http.get<Slot[]>(`${environment.apiUrl}/api/v1/staff/${this.staffId}/availability`, { params }).subscribe({
      next: (items) => this.slots.set(items),
      error: () => this.error.set('Availability could not be loaded. Please try again.'),
    });
  }

  book(slot: Slot): void {
    this.error.set(null);
    this.http
      .post(`${environment.apiUrl}/api/v1/bookings`, {
        offeringId: this.offeringId,
        startUtc: slot.startUtc,
        idempotencyKey: crypto.randomUUID(),
        notes: null,
      })
      .subscribe({
        next: () => void this.router.navigateByUrl('/bookings'),
        error: () => this.error.set('That slot is no longer available.'),
      });
  }
}
