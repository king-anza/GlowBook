import { DatePipe } from '@angular/common';
import { HttpClient, type HttpErrorResponse } from '@angular/common/http';
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

import { ProfessionalNavComponent } from './professional-nav.component';

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
interface CalendarStaff {
  id: string;
  displayName: string;
  workspaceName: string | null;
}
interface CalendarEvent {
  id: string;
  type: 'Booking' | 'BlockedTime';
  title: string;
  staffId: string;
  staffName: string;
  workspaceName: string | null;
  startUtc: string;
  endUtc: string;
  status: string;
  canDelete: boolean;
  conflict: string | null;
}
interface CalendarDay {
  date: string;
  scheduledMinutes: number;
  bookedMinutes: number;
  blockedMinutes: number;
  utilisationPercent: number;
  staffedWorkspaceCount: number;
  conflicts: string[];
}
interface ProfessionalCalendar {
  from: string;
  to: string;
  timeZone: string;
  staff: CalendarStaff[];
  events: CalendarEvent[];
  days: CalendarDay[];
}

@Component({
  selector: 'app-availability',
  templateUrl: './availability.page.html',
  imports: [
    DatePipe,
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
    ProfessionalNavComponent,
  ],
})
export class AvailabilityPage {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  readonly staff = signal<Staff[]>([]);
  readonly message = signal<string | null>(null);
  readonly messageIsError = signal(false);
  readonly weeklyRows = signal<WeeklyRow[]>([]);
  readonly calendar = signal<ProfessionalCalendar | null>(null);
  readonly calendarLoading = signal(true);
  readonly calendarView = signal<'day' | 'week'>('week');
  readonly calendarDate = signal(this.todayInStudio());
  readonly calendarStaffId = signal('');
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
    this.http.get<Business>(`${environment.apiUrl}/api/v1/professional/business`).subscribe((item) => {
      this.staff.set(item.staff);
      this.loadCalendar();
    });
  }
  loadCalendar(): void {
    this.calendarLoading.set(true);
    const from = this.calendarDate();
    const to = this.calendarView() === 'week' ? this.shiftDate(from, 6) : from;
    const params: Record<string, string> = { from, to };
    if (this.calendarStaffId()) params['staffId'] = this.calendarStaffId();
    this.http.get<ProfessionalCalendar>(`${environment.apiUrl}/api/v1/professional/calendar`, { params }).subscribe({
      next: (calendar) => {
        this.calendar.set(calendar);
        this.calendarLoading.set(false);
      },
      error: () => {
        this.calendar.set(null);
        this.calendarLoading.set(false);
        this.setMessage('Unable to load the studio calendar. Try again.', true);
      },
    });
  }
  setCalendarView(view: 'day' | 'week'): void {
    this.calendarView.set(view);
    this.loadCalendar();
  }
  setCalendarStaff(staffId: string): void {
    this.calendarStaffId.set(staffId);
    this.loadCalendar();
  }
  moveCalendar(direction: -1 | 1): void {
    this.calendarDate.set(this.shiftDate(this.calendarDate(), direction * (this.calendarView() === 'week' ? 7 : 1)));
    this.loadCalendar();
  }
  eventsFor(date: string): CalendarEvent[] {
    return (this.calendar()?.events ?? []).filter((item) => this.studioDate(item.startUtc) === date);
  }
  addWeeklyRow(): void {
    if (this.form.invalid) return this.form.markAllAsTouched();
    const value = this.form.getRawValue();
    if (value.startTime >= value.endTime) {
      this.setMessage('The start time must be before the end time.', true);
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
      this.setMessage('Choose staff and add at least one interval.', true);
      return;
    }
    this.http
      .put(`${environment.apiUrl}/api/v1/professional/staff/${staffId}/weekly-availability`, this.weeklyRows())
      .subscribe({
        next: () => {
          this.setMessage('Weekly availability saved.');
          this.loadCalendar();
        },
        error: () => this.setMessage('Unable to save availability.', true),
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
        next: () => {
          this.setMessage('Availability exception added.');
          this.loadCalendar();
        },
        error: () => this.setMessage('Unable to add the exception.', true),
      });
  }
  addBlockedTime(): void {
    if (this.blockedForm.invalid) return this.blockedForm.markAllAsTouched();
    const value = this.blockedForm.getRawValue();
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/staff/${value.staffId}/blocked-times`, {
        startUtc: this.studioLocalToUtc(value.startUtc),
        endUtc: this.studioLocalToUtc(value.endUtc),
        reason: value.reason || null,
      })
      .subscribe({
        next: () => {
          this.setMessage('Blocked time added.');
          this.blockedForm.reset({ staffId: value.staffId, startUtc: '', endUtc: '', reason: '' });
          this.loadCalendar();
        },
        error: (error: HttpErrorResponse) =>
          this.setMessage(
            error.status === 409
              ? 'That block overlaps a pending or confirmed booking. Move the booking first.'
              : 'Unable to block that time.',
            true,
          ),
      });
  }
  removeBlockedTime(id: string): void {
    this.http.delete(`${environment.apiUrl}/api/v1/professional/blocked-times/${id}`).subscribe({
      next: () => {
        this.setMessage('Blocked time removed.');
        this.loadCalendar();
      },
      error: () => this.setMessage('Unable to remove blocked time.', true),
    });
  }
  private setMessage(message: string, isError = false): void {
    this.message.set(message);
    this.messageIsError.set(isError);
  }
  private todayInStudio(): string {
    return new Intl.DateTimeFormat('en-CA', {
      timeZone: 'Africa/Johannesburg',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(new Date());
  }
  private shiftDate(value: string, days: number): string {
    const date = new Date(`${value}T12:00:00Z`);
    date.setUTCDate(date.getUTCDate() + days);
    return date.toISOString().slice(0, 10);
  }
  private studioDate(value: string): string {
    return new Intl.DateTimeFormat('en-CA', {
      timeZone: 'Africa/Johannesburg',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(new Date(value));
  }
  private studioLocalToUtc(value: string): string {
    return new Date(`${value}:00+02:00`).toISOString();
  }
}
