import { DatePipe, KeyValuePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  IonButton,
  IonCheckbox,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonList,
  IonSelect,
  IonSelectOption,
  IonTextarea,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';
import { ActionGuardService } from '../../core/ui/action-guard.service';
import { EmptyStateComponent } from '../../core/ui/empty-state.component';
import { FeedbackBannerComponent } from '../../core/ui/feedback-banner.component';
import { FormBehaviorDirective } from '../../core/ui/form-behavior.directive';

import type {
  MatchedOpportunity,
  SpaceApplicantProfile,
  SpaceApplication,
  SpaceOpportunity,
  OpportunityType,
  WorkspaceType,
} from './space.models';
import { workspaceLabels } from './space.models';

@Component({
  selector: 'app-spaces',
  templateUrl: './spaces.page.html',
  styleUrls: ['./spaces.page.scss'],
  imports: [
    ReactiveFormsModule,
    FormBehaviorDirective,
    KeyValuePipe,
    DatePipe,
    RouterLink,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonInput,
    IonSelect,
    IonSelectOption,
    IonTextarea,
    IonButton,
    IonCheckbox,
    EmptyStateComponent,
    FeedbackBannerComponent,
  ],
})
export class SpacesPage {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  readonly route = inject(ActivatedRoute);
  readonly highlightedId = this.route.snapshot.queryParamMap.get('opportunity');
  readonly auth = inject(AuthService);
  readonly actions = inject(ActionGuardService);
  readonly opportunities = signal<SpaceOpportunity[]>([]);
  readonly matches = signal<Record<string, MatchedOpportunity>>({});
  readonly myApplications = signal<SpaceApplication[]>([]);
  readonly profile = signal<SpaceApplicantProfile | null>(null);
  readonly editingProfile = signal(false);
  readonly loading = signal(true);
  readonly selected = signal<SpaceOpportunity | null>(null);
  readonly reporting = signal<SpaceOpportunity | null>(null);
  readonly showingSaved = signal(false);
  readonly showingMatches = signal(false);
  readonly message = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly labels = workspaceLabels;
  readonly filters = this.fb.nonNullable.group({
    city: [''],
    type: ['' as '' | OpportunityType],
    workspaceType: ['' as '' | WorkspaceType],
  });
  readonly applicationForm = this.fb.nonNullable.group({
    message: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(1500)]],
    portfolioUrl: ['', Validators.pattern(/^$|^https:\/\/.+/)],
  });
  readonly profileForm = this.fb.nonNullable.group({
    summary: ['', [Validators.required, Validators.maxLength(1000)]],
    skills: ['', [Validators.required, Validators.maxLength(1000)]],
    availability: ['', [Validators.required, Validators.maxLength(500)]],
    availableFrom: [''],
    portfolioUrl: ['', Validators.pattern(/^$|^https:\/\/.+/)],
    preferredCities: ['', Validators.maxLength(500)],
    chairRental: [true],
    staffVacancy: [true],
    matchingAlertsEnabled: [false],
  });
  readonly reportForm = this.fb.nonNullable.group({
    reason: ['Misleading'],
    details: ['', Validators.maxLength(1000)],
  });
  private coordinates: { latitude: number; longitude: number } | null = null;

  constructor() {
    this.load();
    if (this.auth.hasRole('Professional')) this.loadWorkspace();
  }

  private loadWorkspace(): void {
    this.http.get<SpaceApplicantProfile | null>(`${environment.apiUrl}/api/v1/professional/space-profile`).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        if (profile)
          this.profileForm.patchValue({
            ...profile,
            availableFrom: profile.availableFrom ?? '',
            portfolioUrl: profile.portfolioUrl ?? '',
            preferredCities: profile.preferredCities.join(', '),
            chairRental: profile.preferredOpportunityTypes.includes('ChairRental'),
            staffVacancy: profile.preferredOpportunityTypes.includes('StaffVacancy'),
          });
      },
    });
    this.http
      .get<SpaceApplication[]>(`${environment.apiUrl}/api/v1/professional/space-applications/mine`)
      .subscribe({ next: (items) => this.myApplications.set(items) });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const value = this.filters.getRawValue();
    const params: Record<string, string> = {};
    if (value.city.trim()) params['city'] = value.city.trim();
    if (value.type) params['type'] = value.type;
    if (value.workspaceType) params['workspaceType'] = value.workspaceType;
    if (this.coordinates) {
      params['latitude'] = String(this.coordinates.latitude);
      params['longitude'] = String(this.coordinates.longitude);
    }
    const url = this.showingMatches()
      ? `${environment.apiUrl}/api/v1/professional/spaces/matches`
      : this.showingSaved()
        ? `${environment.apiUrl}/api/v1/professional/spaces/saved`
        : `${environment.apiUrl}/api/v1/spaces`;
    this.http
      .get<SpaceOpportunity[] | MatchedOpportunity[]>(url, {
        params: this.showingSaved() || this.showingMatches() ? {} : params,
      })
      .subscribe({
        next: (response) => {
          const matched = this.showingMatches() ? (response as MatchedOpportunity[]) : [];
          const items = this.showingMatches() ? matched.map((x) => x.opportunity) : (response as SpaceOpportunity[]);
          this.matches.set(Object.fromEntries(matched.map((x) => [x.opportunity.id, x])));
          this.opportunities.set(items);
          if (this.highlightedId) this.selected.set(items.find((item) => item.id === this.highlightedId) ?? null);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Glow Spaces could not be loaded. Please try again.');
          this.loading.set(false);
        },
      });
  }

  toggleSavedView(): void {
    this.showingMatches.set(false);
    this.showingSaved.update((value) => !value);
    this.load();
  }

  toggleMatchesView(): void {
    this.showingSaved.set(false);
    this.showingMatches.update((value) => !value);
    this.load();
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return this.profileForm.markAllAsTouched();
    if (!this.startAction('space-profile')) return;
    const raw = this.profileForm.getRawValue();
    const preferredOpportunityTypes: OpportunityType[] = [];
    if (raw.chairRental) preferredOpportunityTypes.push('ChairRental');
    if (raw.staffVacancy) preferredOpportunityTypes.push('StaffVacancy');
    this.http
      .put<SpaceApplicantProfile>(`${environment.apiUrl}/api/v1/professional/space-profile`, {
        summary: raw.summary,
        skills: raw.skills,
        availability: raw.availability,
        availableFrom: raw.availableFrom || null,
        portfolioUrl: raw.portfolioUrl || null,
        preferredCities: raw.preferredCities
          .split(',')
          .map((x) => x.trim())
          .filter(Boolean),
        preferredOpportunityTypes,
        matchingAlertsEnabled: raw.matchingAlertsEnabled,
      })
      .subscribe({
        next: (profile) => {
          this.profile.set(profile);
          this.editingProfile.set(false);
          this.message.set('Your reusable application profile was saved.');
          this.actions.end('space-profile');
        },
        error: () => {
          this.error.set('Your application profile could not be saved. Your entries are still here.');
          this.actions.end('space-profile');
        },
      });
  }

  clearFilters(): void {
    this.filters.reset({ city: '', type: '', workspaceType: '' });
    this.load();
  }

  useLocation(): void {
    this.message.set('Your precise location is used only for this search and is not saved.');
    navigator.geolocation?.getCurrentPosition(
      (position) => {
        this.coordinates = { latitude: position.coords.latitude, longitude: position.coords.longitude };
        this.load();
      },
      () => this.error.set('Location was not available. You can still search by city or suburb.'),
      { enableHighAccuracy: false, timeout: 10000 },
    );
  }

  toggleSaved(item: SpaceOpportunity): void {
    const key = `save-space-${item.id}`;
    if (!this.startAction(key)) return;
    const request = item.isSaved
      ? this.http.delete(`${environment.apiUrl}/api/v1/professional/spaces/${item.id}/saved`)
      : this.http.put(`${environment.apiUrl}/api/v1/professional/spaces/${item.id}/saved`, {});
    request.subscribe({
      next: () => {
        this.opportunities.update((items) =>
          items.map((candidate) =>
            candidate.id === item.id ? { ...candidate, isSaved: !candidate.isSaved } : candidate,
          ),
        );
        this.actions.end(key);
      },
      error: () => {
        this.error.set('Use a signed-in professional account to save opportunities.');
        this.actions.end(key);
      },
    });
  }

  async share(item: SpaceOpportunity): Promise<void> {
    const url = `${location.origin}/spaces?opportunity=${item.id}`;
    if (navigator.share)
      await navigator.share({ title: item.title, text: `${item.title} at ${item.businessName}`, url });
    else {
      await navigator.clipboard.writeText(url);
      this.message.set('Opportunity link copied.');
    }
  }

  report(item: SpaceOpportunity): void {
    const key = `report-space-${item.id}`;
    if (!this.startAction(key)) return;
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/spaces/${item.id}/reports`, this.reportForm.getRawValue())
      .subscribe({
        next: () => {
          this.reporting.set(null);
          this.reportForm.reset({ reason: 'Misleading', details: '' });
          this.message.set('Thank you. The opportunity was reported for review.');
          this.actions.end(key);
        },
        error: () => {
          this.error.set('The report could not be sent. Your details are preserved so you can try again.');
          this.actions.end(key);
        },
      });
  }

  apply(item: SpaceOpportunity): void {
    if (!this.auth.isAuthenticated()) return;
    if (this.applicationForm.invalid) return this.applicationForm.markAllAsTouched();
    const key = `apply-space-${item.id}`;
    if (!this.startAction(key)) return;
    this.error.set(null);
    this.http
      .post(
        `${environment.apiUrl}/api/v1/professional/spaces/${item.id}/applications`,
        this.applicationForm.getRawValue(),
      )
      .subscribe({
        next: () => {
          this.message.set(`Your application for “${item.title}” was sent to ${item.businessName}.`);
          this.selected.set(null);
          this.applicationForm.reset();
          this.loadWorkspace();
          this.actions.end(key);
        },
        error: () => {
          this.error.set(
            'The application could not be sent. You may have applied already, or need a professional account.',
          );
          this.actions.end(key);
        },
      });
  }

  withdraw(application: SpaceApplication): void {
    const key = `withdraw-application-${application.id}`;
    if (!this.startAction(key)) return;
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/space-applications/${application.id}/withdraw`, {})
      .subscribe({
        next: () => {
          this.message.set('Application withdrawn.');
          this.loadWorkspace();
          this.actions.end(key);
        },
        error: () => {
          this.error.set('This application can no longer be withdrawn.');
          this.actions.end(key);
        },
      });
  }

  rate(item: SpaceOpportunity): string {
    if (item.ratePeriod === 'Negotiable' || item.rateAmount === null) return 'Rate negotiable';
    if (item.ratePeriod === 'RevenueShare') return `${item.rateAmount}% revenue share`;
    return `R${item.rateAmount} / ${item.ratePeriod.toLowerCase()}`;
  }

  private startAction(key: string): boolean {
    const result = this.actions.begin(key);
    if (result === 'offline')
      this.error.set('Reconnect before making this change. It has not been queued or submitted.');
    return result === 'started';
  }
}
