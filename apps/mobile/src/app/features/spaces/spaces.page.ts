import { KeyValuePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  IonButton,
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
import { EmptyStateComponent } from '../../core/ui/empty-state.component';
import { FeedbackBannerComponent } from '../../core/ui/feedback-banner.component';

import type { SpaceOpportunity, OpportunityType, WorkspaceType } from './space.models';
import { workspaceLabels } from './space.models';

@Component({
  selector: 'app-spaces',
  templateUrl: './spaces.page.html',
  styleUrls: ['./spaces.page.scss'],
  imports: [
    ReactiveFormsModule,
    KeyValuePipe,
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
    EmptyStateComponent,
    FeedbackBannerComponent,
  ],
})
export class SpacesPage {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  readonly highlightedId = this.route.snapshot.queryParamMap.get('opportunity');
  readonly auth = inject(AuthService);
  readonly opportunities = signal<SpaceOpportunity[]>([]);
  readonly loading = signal(true);
  readonly selected = signal<SpaceOpportunity | null>(null);
  readonly reporting = signal<SpaceOpportunity | null>(null);
  readonly showingSaved = signal(false);
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
  readonly reportForm = this.fb.nonNullable.group({
    reason: ['Misleading'],
    details: ['', Validators.maxLength(1000)],
  });
  private coordinates: { latitude: number; longitude: number } | null = null;

  constructor() {
    this.load();
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
    const url = this.showingSaved()
      ? `${environment.apiUrl}/api/v1/professional/spaces/saved`
      : `${environment.apiUrl}/api/v1/spaces`;
    this.http.get<SpaceOpportunity[]>(url, { params: this.showingSaved() ? {} : params }).subscribe({
      next: (items) => {
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
    this.showingSaved.update((value) => !value);
    this.load();
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
    const request = item.isSaved
      ? this.http.delete(`${environment.apiUrl}/api/v1/professional/spaces/${item.id}/saved`)
      : this.http.put(`${environment.apiUrl}/api/v1/professional/spaces/${item.id}/saved`, {});
    request.subscribe({
      next: () =>
        this.opportunities.update((items) =>
          items.map((candidate) =>
            candidate.id === item.id ? { ...candidate, isSaved: !candidate.isSaved } : candidate,
          ),
        ),
      error: () => this.error.set('Use a signed-in professional account to save opportunities.'),
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
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/spaces/${item.id}/reports`, this.reportForm.getRawValue())
      .subscribe({
        next: () => {
          this.reporting.set(null);
          this.reportForm.reset({ reason: 'Misleading', details: '' });
          this.message.set('Thank you. The opportunity was reported for review.');
        },
        error: () => this.error.set('The report could not be sent. You may have reported this opportunity already.'),
      });
  }

  apply(item: SpaceOpportunity): void {
    if (!this.auth.isAuthenticated()) return;
    if (this.applicationForm.invalid) return this.applicationForm.markAllAsTouched();
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
        },
        error: () =>
          this.error.set(
            'The application could not be sent. You may have applied already, or need a professional account.',
          ),
      });
  }

  rate(item: SpaceOpportunity): string {
    if (item.ratePeriod === 'Negotiable' || item.rateAmount === null) return 'Rate negotiable';
    if (item.ratePeriod === 'RevenueShare') return `${item.rateAmount}% revenue share`;
    return `R${item.rateAmount} / ${item.ratePeriod.toLowerCase()}`;
  }
}
