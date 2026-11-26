import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { FeedbackBannerComponent } from '../../core/ui/feedback-banner.component';

import { ProfessionalNavComponent } from './professional-nav.component';

interface OnboardingSummary {
  accountVerified: boolean;
  businessComplete: boolean;
  locationComplete: boolean;
  portfolioComplete: boolean;
  staffComplete: boolean;
  servicesComplete: boolean;
  pricingComplete: boolean;
  availabilityComplete: boolean;
  approvalSubmitted: boolean;
  approvalStatus: string;
  reviewReason: string | null;
}

interface OnboardingStep {
  title: string;
  description: string;
  complete: boolean;
  route?: string;
  action: string;
}

@Component({
  selector: 'app-professional-onboarding',
  templateUrl: './onboarding.page.html',
  styleUrls: ['./onboarding.page.scss'],
  imports: [
    RouterLink,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonButton,
    FeedbackBannerComponent,
    ProfessionalNavComponent,
  ],
})
export class OnboardingPage {
  private readonly http = inject(HttpClient);
  readonly summary = signal<OnboardingSummary | null>(null);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly steps = computed<OnboardingStep[]>(() => {
    const item = this.summary();
    return [
      {
        title: 'Verify your account',
        description: 'Confirm your email and secure your sign-in.',
        complete: item?.accountVerified ?? false,
        route: '/account/profile',
        action: 'Review account',
      },
      {
        title: 'Describe your business',
        description: 'Add your trading name, description, contact details and cancellation policy.',
        complete: item?.businessComplete ?? false,
        route: '/professional/profile',
        action: 'Edit business',
      },
      {
        title: 'Confirm your location',
        description: 'Add the address clients will use to find your studio.',
        complete: item?.locationComplete ?? false,
        route: '/professional/profile',
        action: 'Add location',
      },
      {
        title: 'Build your portfolio',
        description: 'Publish at least one clear image with an accessible description.',
        complete: item?.portfolioComplete ?? false,
        route: '/professional/profile',
        action: 'Add portfolio',
      },
      {
        title: 'Add your team',
        description: 'Add every professional who will accept bookings.',
        complete: item?.staffComplete ?? false,
        route: '/professional/services',
        action: 'Manage team',
      },
      {
        title: 'Create your services',
        description: 'Tell clients which treatments and styles you offer.',
        complete: item?.servicesComplete ?? false,
        route: '/professional/services',
        action: 'Add services',
      },
      {
        title: 'Set prices and duration',
        description: 'Connect active services to staff with a price and realistic duration.',
        complete: item?.pricingComplete ?? false,
        route: '/professional/services',
        action: 'Add pricing',
      },
      {
        title: 'Publish availability',
        description: 'Add weekly hours for every active professional accepting bookings.',
        complete: item?.availabilityComplete ?? false,
        route: '/professional/calendar',
        action: 'Set hours',
      },
      {
        title: 'Submit for approval',
        description: 'Send the completed studio to Glow for a final trust and quality review.',
        complete: item?.approvalSubmitted ?? false,
        action: 'Submit for review',
      },
    ];
  });
  readonly completedCount = computed(() => this.steps().filter((step) => step.complete).length);
  readonly progress = computed(() => Math.round((this.completedCount() / this.steps().length) * 100));
  readonly readyToSubmit = computed(() =>
    this.steps()
      .slice(0, 8)
      .every((step) => step.complete),
  );

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<OnboardingSummary>(`${environment.apiUrl}/api/v1/professional/onboarding`).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('We could not load your studio setup. Check your connection and try again.');
        this.loading.set(false);
      },
    });
  }

  submitForReview(): void {
    if (!this.readyToSubmit() || this.submitting()) return;
    this.submitting.set(true);
    this.message.set(null);
    this.http.post<void>(`${environment.apiUrl}/api/v1/professional/business/submit`, {}).subscribe({
      next: () => {
        this.message.set('Your studio has been submitted. We will show the decision here.');
        this.submitting.set(false);
        this.load();
      },
      error: () => {
        this.error.set('Your studio could not be submitted. Review the incomplete steps and try again.');
        this.submitting.set(false);
      },
    });
  }
}
