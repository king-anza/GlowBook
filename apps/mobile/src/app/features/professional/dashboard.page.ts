import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { EmptyStateComponent } from '../../core/ui/empty-state.component';
import { FeedbackBannerComponent } from '../../core/ui/feedback-banner.component';

interface DashboardBooking {
  id: string;
  customerName: string;
  staffName: string;
  serviceName: string;
  startUtc: string;
  endUtc: string;
  status: string;
}

interface DashboardSummary {
  approvalStatus: string;
  reviewReason: string | null;
  setupCompletedCount: number;
  setupTotalCount: number;
  todayBookingCount: number;
  pendingBookingCount: number;
  activeStaffCount: number;
  staffScheduledTodayCount: number;
  publishedSpaceCount: number;
  pendingSpaceApplicationCount: number;
  todayBookings: DashboardBooking[];
}

@Component({
  selector: 'app-professional-dashboard',
  templateUrl: './dashboard.page.html',
  styleUrls: ['./dashboard.page.scss'],
  imports: [
    DatePipe,
    RouterLink,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    EmptyStateComponent,
    FeedbackBannerComponent,
  ],
})
export class DashboardPage {
  private readonly http = inject(HttpClient);
  readonly summary = signal<DashboardSummary | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly setupProgress = computed(() => {
    const item = this.summary();
    return item ? Math.round((item.setupCompletedCount / item.setupTotalCount) * 100) : 0;
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<DashboardSummary>(`${environment.apiUrl}/api/v1/professional/dashboard`).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('We could not load your studio dashboard. Check your connection and try again.');
        this.loading.set(false);
      },
    });
  }
}
