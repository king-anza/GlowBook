import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { FeedbackBannerComponent } from '../../core/ui/feedback-banner.component';

import { ProfessionalNavComponent } from './professional-nav.component';

interface InsightPoint {
  date: string;
  bookingCount: number;
  completedCount: number;
  cancelledCount: number;
  noShowCount: number;
}
interface ProfessionalInsights {
  periodDays: number;
  from: string;
  to: string;
  bookingVolume: number;
  completedCount: number;
  cancellationCount: number;
  noShowCount: number;
  repeatCustomerCount: number | null;
  customerPrivacyThresholdMet: boolean;
  capacityMinutes: number;
  bookedMinutes: number;
  chairUtilisationPercent: number;
  daily: InsightPoint[];
}

@Component({
  selector: 'app-professional-insights',
  templateUrl: './insights.page.html',
  styleUrls: ['./insights.page.scss'],
  imports: [DatePipe, IonHeader, IonToolbar, IonTitle, IonContent, FeedbackBannerComponent, ProfessionalNavComponent],
})
export class InsightsPage {
  private readonly http = inject(HttpClient);
  readonly insights = signal<ProfessionalInsights | null>(null);
  readonly periodDays = signal<30 | 90>(30);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  setPeriod(periodDays: 30 | 90): void {
    if (periodDays === this.periodDays()) return;
    this.periodDays.set(periodDays);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http
      .get<ProfessionalInsights>(`${environment.apiUrl}/api/v1/professional/insights`, {
        params: { periodDays: this.periodDays() },
      })
      .subscribe({
        next: (insights) => {
          this.insights.set(insights);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('We could not load studio insights. Check your connection and try again.');
          this.loading.set(false);
        },
      });
  }

  rate(count: number, total: number): number {
    return total ? Math.round((count * 1000) / total) / 10 : 0;
  }
}
