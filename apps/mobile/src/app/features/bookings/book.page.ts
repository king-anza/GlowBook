import { DatePipe, Location } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { IonButton, IonContent, IonItem, IonTextarea } from '@ionic/angular';

import { environment } from '../../../environments/environment';

interface Slot {
  startUtc: string;
  endUtc: string;
}
interface CalendarDay {
  key: string;
  date: Date;
  available: boolean;
  slotCount: number;
}
interface TimeGroup {
  label: string;
  slots: Slot[];
}

@Component({
  selector: 'app-book',
  templateUrl: './book.page.html',
  styleUrls: ['./book.page.scss'],
  imports: [DatePipe, ReactiveFormsModule, IonContent, IonButton, IonItem, IonTextarea],
})
export class BookPage {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly fb = inject(FormBuilder);
  private readonly staffId = this.route.snapshot.paramMap.get('staffId')!;
  private readonly offeringId = this.route.snapshot.paramMap.get('offeringId')!;
  private readonly rangeStart = this.startOfTomorrow();

  readonly serviceName = this.route.snapshot.queryParamMap.get('service') ?? 'Beauty appointment';
  readonly staffName = this.route.snapshot.queryParamMap.get('professional') ?? 'Your professional';
  readonly businessName = this.route.snapshot.queryParamMap.get('business') ?? 'Selected studio';
  readonly durationMinutes = Number(this.route.snapshot.queryParamMap.get('duration')) || null;
  readonly price = Number(this.route.snapshot.queryParamMap.get('price')) || null;
  readonly timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'your local time zone';

  readonly slots = signal<Slot[]>([]);
  readonly selectedDate = signal<string>(this.localDateKey(this.rangeStart));
  readonly calendarDays = computed<CalendarDay[]>(() =>
    Array.from({ length: 14 }, (_, offset) => {
      const date = new Date(this.rangeStart);
      date.setDate(date.getDate() + offset);
      const key = this.localDateKey(date);
      const slotCount = this.slots().filter((slot) => this.localDateKey(new Date(slot.startUtc)) === key).length;
      return { key, date, available: slotCount > 0, slotCount };
    }),
  );
  readonly availableDates = computed(() =>
    this.calendarDays()
      .filter((day) => day.available)
      .map((day) => day.key),
  );
  readonly slotsForSelectedDate = computed(() =>
    this.slots().filter((slot) => this.localDateKey(new Date(slot.startUtc)) === this.selectedDate()),
  );
  readonly timeGroups = computed<TimeGroup[]>(() => {
    const slots = this.slotsForSelectedDate();
    return [
      { label: 'Morning', slots: slots.filter((slot) => new Date(slot.startUtc).getHours() < 12) },
      {
        label: 'Afternoon',
        slots: slots.filter((slot) => {
          const hour = new Date(slot.startUtc).getHours();
          return hour >= 12 && hour < 17;
        }),
      },
      { label: 'Evening', slots: slots.filter((slot) => new Date(slot.startUtc).getHours() >= 17) },
    ].filter((group) => group.slots.length > 0);
  });
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);
  readonly selectedSlot = signal<Slot | null>(null);
  readonly submitting = signal(false);
  readonly notesForm = this.fb.nonNullable.group({ notes: ['', Validators.maxLength(500)] });

  constructor() {
    this.loadAvailability();
  }

  loadAvailability(): void {
    this.loading.set(true);
    this.error.set(null);
    const to = new Date(this.rangeStart);
    to.setDate(to.getDate() + 13);
    const params = new HttpParams()
      .set('offeringId', this.offeringId)
      .set('from', this.localDateKey(this.rangeStart))
      .set('to', this.localDateKey(to));
    this.http.get<Slot[]>(`${environment.apiUrl}/api/v1/staff/${this.staffId}/availability`, { params }).subscribe({
      next: (items) => {
        this.slots.set(items);
        const first = this.calendarDays().find((day) => day.available);
        if (first) this.selectedDate.set(first.key);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Availability could not be loaded. Check your connection and try again.');
        this.loading.set(false);
      },
    });
  }

  chooseDate(day: CalendarDay): void {
    if (day.available) {
      this.selectedDate.set(day.key);
      this.selectedSlot.set(null);
    }
  }
  chooseSlot(slot: Slot): void {
    this.selectedSlot.set(slot);
    this.error.set(null);
  }
  book(slot: Slot): void {
    if (this.submitting() || this.notesForm.invalid) return;
    this.error.set(null);
    this.submitting.set(true);
    const notes = this.notesForm.controls.notes.value.trim();
    this.http
      .post(`${environment.apiUrl}/api/v1/bookings`, {
        offeringId: this.offeringId,
        startUtc: slot.startUtc,
        idempotencyKey: crypto.randomUUID(),
        notes: notes || null,
      })
      .subscribe({
        next: () => {
          this.selectedSlot.set(null);
          this.submitting.set(false);
          void this.router.navigateByUrl('/bookings');
        },
        error: () => {
          this.submitting.set(false);
          this.error.set('That time has just become unavailable. Choose another available time.');
          this.selectedSlot.set(null);
          this.loadAvailability();
        },
      });
  }
  goBack(): void {
    const navigationId = typeof window === 'undefined' ? 0 : Number(window.history.state?.navigationId ?? 0);
    if (navigationId > 1) this.location.back();
    else void this.router.navigateByUrl('/home');
  }
  slotDuration(slot: Slot): number {
    return Math.round((new Date(slot.endUtc).getTime() - new Date(slot.startUtc).getTime()) / 60_000);
  }
  private startOfTomorrow(): Date {
    const date = new Date();
    date.setHours(0, 0, 0, 0);
    date.setDate(date.getDate() + 1);
    return date;
  }
  private localDateKey(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
