import { KeyValuePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
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
  ],
})
export class SpacesPage {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  readonly opportunities = signal<SpaceOpportunity[]>([]);
  readonly loading = signal(true);
  readonly selected = signal<SpaceOpportunity | null>(null);
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
    this.http.get<SpaceOpportunity[]>(`${environment.apiUrl}/api/v1/spaces`, { params }).subscribe({
      next: (items) => {
        this.opportunities.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Glow Spaces could not be loaded. Please try again.');
        this.loading.set(false);
      },
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
