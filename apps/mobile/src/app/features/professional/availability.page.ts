import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  IonButton,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonList,
  IonSelect,
  IonSelectOption,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { environment } from '../../../environments/environment';

interface Staff {
  id: string;
  displayName: string;
}
interface Business {
  staff: Staff[];
}
interface WeeklyRow {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

@Component({
  selector: 'app-availability',
  templateUrl: './availability.page.html',
  imports: [
    ReactiveFormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonInput,
    IonSelect,
    IonSelectOption,
    IonButton,
  ],
})
export class AvailabilityPage {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  readonly staff = signal<Staff[]>([]);
  readonly message = signal<string | null>(null);
  readonly weeklyRows = signal<WeeklyRow[]>([]);
  readonly form = this.fb.nonNullable.group({
    staffId: ['', Validators.required],
    dayOfWeek: [1, Validators.required],
    startTime: ['09:00', Validators.required],
    endTime: ['17:00', Validators.required],
  });
  readonly exceptionForm = this.fb.nonNullable.group({
    staffId: ['', Validators.required],
    date: ['', Validators.required],
    type: [0, Validators.required],
    startTime: [''],
    endTime: [''],
    reason: [''],
  });
  readonly blockedForm = this.fb.nonNullable.group({
    staffId: ['', Validators.required],
    startUtc: ['', Validators.required],
    endUtc: ['', Validators.required],
    reason: [''],
  });
  constructor() {
    this.http
      .get<Business>(`${environment.apiUrl}/api/v1/professional/business`)
      .subscribe((item) => this.staff.set(item.staff));
  }
  addWeeklyRow(): void {
    if (this.form.invalid) return this.form.markAllAsTouched();
    const value = this.form.getRawValue();
    if (value.startTime >= value.endTime) {
      this.message.set('The start time must be before the end time.');
      return;
    }
    this.weeklyRows.update((rows) => [
      ...rows,
      { dayOfWeek: value.dayOfWeek, startTime: `${value.startTime}:00`, endTime: `${value.endTime}:00` },
    ]);
  }
  removeWeeklyRow(index: number): void {
    this.weeklyRows.update((rows) => rows.filter((_, current) => current !== index));
  }
  loadWeekly(): void {
    const staffId = this.form.controls.staffId.value;
    if (!staffId) return;
    this.http
      .get<WeeklyRow[]>(`${environment.apiUrl}/api/v1/professional/staff/${staffId}/weekly-availability`)
      .subscribe((rows) =>
        this.weeklyRows.set(
          rows.map((row) => ({ ...row, startTime: row.startTime.slice(0, 8), endTime: row.endTime.slice(0, 8) })),
        ),
      );
  }
  save(): void {
    const staffId = this.form.controls.staffId.value;
    if (!staffId || this.weeklyRows().length === 0) {
      this.message.set('Choose staff and add at least one interval.');
      return;
    }
    this.http
      .put(`${environment.apiUrl}/api/v1/professional/staff/${staffId}/weekly-availability`, this.weeklyRows())
      .subscribe({
        next: () => this.message.set('Weekly availability saved.'),
        error: () => this.message.set('Unable to save availability.'),
      });
  }
  addException(): void {
    if (this.exceptionForm.invalid) return this.exceptionForm.markAllAsTouched();
    const value = this.exceptionForm.getRawValue();
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/staff/${value.staffId}/availability-exceptions`, {
        date: value.date,
        type: value.type,
        startTime: value.startTime || null,
        endTime: value.endTime || null,
        reason: value.reason || null,
      })
      .subscribe({
        next: () => this.message.set('Availability exception added.'),
        error: () => this.message.set('Unable to add the exception.'),
      });
  }
  addBlockedTime(): void {
    if (this.blockedForm.invalid) return this.blockedForm.markAllAsTouched();
    const value = this.blockedForm.getRawValue();
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/staff/${value.staffId}/blocked-times`, {
        startUtc: new Date(value.startUtc).toISOString(),
        endUtc: new Date(value.endUtc).toISOString(),
        reason: value.reason || null,
      })
      .subscribe({
        next: () => this.message.set('Blocked time added.'),
        error: () => this.message.set('Unable to block that time.'),
      });
  }
}
