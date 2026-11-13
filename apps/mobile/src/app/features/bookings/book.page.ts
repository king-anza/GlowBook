import { DatePipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonButton, IonContent } from '@ionic/angular';

import { environment } from '../../../environments/environment';

interface Slot {
  startUtc: string;
  endUtc: string;
}

@Component({
  selector: 'app-book',
  templateUrl: './book.page.html',
  imports: [DatePipe, IonContent, IonButton],
})
export class BookPage {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly staffId = this.route.snapshot.paramMap.get('staffId')!;
  private readonly offeringId = this.route.snapshot.paramMap.get('offeringId')!;

  readonly slots = signal<Slot[]>([]);
  readonly selectedDate = signal<string | null>(null);
  readonly availableDates = computed(() => [...new Set(this.slots().map((slot) => slot.startUtc.slice(0, 10)))]);
  readonly slotsForSelectedDate = computed(() => {
    const date = this.selectedDate();
    return date ? this.slots().filter((slot) => slot.startUtc.startsWith(date)) : [];
  });
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);
  readonly selectedSlot = signal<Slot | null>(null);
  readonly submitting = signal(false);

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
      next: (items) => {
        this.slots.set(items);
        this.selectedDate.set(items[0]?.startUtc.slice(0, 10) ?? null);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Availability could not be loaded. Please try again.');
        this.loading.set(false);
      },
    });
  }

  chooseDate(date: string): void {
    this.selectedDate.set(date);
  }

  book(slot: Slot): void {
    if (this.submitting()) return;
    this.error.set(null);
    this.submitting.set(true);
    this.http
      .post(`${environment.apiUrl}/api/v1/bookings`, {
        offeringId: this.offeringId,
        startUtc: slot.startUtc,
        idempotencyKey: crypto.randomUUID(),
        notes: null,
      })
      .subscribe({
        next: () => {
          this.selectedSlot.set(null);
          this.submitting.set(false);
          void this.router.navigateByUrl('/bookings');
        },
        error: () => {
          this.submitting.set(false);
          this.error.set('That slot is no longer available.');
        },
      });
  }
}
