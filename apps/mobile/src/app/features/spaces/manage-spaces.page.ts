import { KeyValuePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
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
import { ProfessionalNavComponent } from '../professional/professional-nav.component';

import { type OpportunityStatus, type SpaceApplication, type SpaceOpportunity, workspaceLabels } from './space.models';

@Component({
  selector: 'app-manage-spaces',
  templateUrl: './manage-spaces.page.html',
  styleUrls: ['./manage-spaces.page.scss'],
  imports: [
    KeyValuePipe,
    ReactiveFormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonInput,
    IonTextarea,
    IonSelect,
    IonSelectOption,
    IonCheckbox,
    IonButton,
    ProfessionalNavComponent,
  ],
})
export class ManageSpacesPage {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  readonly highlightedApplicationId = this.route.snapshot.queryParamMap.get('application');
  readonly opportunities = signal<SpaceOpportunity[]>([]);
  readonly applications = signal<SpaceApplication[]>([]);
  readonly message = signal<string | null>(null);
  readonly editing = signal<SpaceOpportunity | null>(null);
  readonly labels = workspaceLabels;
  readonly form = this.fb.nonNullable.group({
    workspaceType: ['StylingChair', Validators.required],
    opportunityType: ['ChairRental', Validators.required],
    title: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    requiredSkills: ['', Validators.maxLength(1000)],
    includedAmenities: ['', Validators.maxLength(1000)],
    houseRules: ['', Validators.maxLength(1500)],
    spacesAvailable: [1, [Validators.required, Validators.min(1), Validators.max(50)]],
    rateAmount: [0 as number | null],
    ratePeriod: ['Day', Validators.required],
    startDate: [new Date().toISOString().slice(0, 10), Validators.required],
    endDate: ['' as string | null],
    recurring: [false],
    bringOwnClients: [false],
    approvalRequired: [true],
    imageUrl: ['', Validators.pattern(/^$|^https:\/\/.+/)],
    imageUrls: ['', Validators.maxLength(10244)],
  });

  constructor() {
    this.load();
  }
  load(): void {
    this.http
      .get<SpaceOpportunity[]>(`${environment.apiUrl}/api/v1/professional/spaces`)
      .subscribe({ next: (x) => this.opportunities.set(x) });
    this.http
      .get<SpaceApplication[]>(`${environment.apiUrl}/api/v1/professional/space-applications/received`)
      .subscribe({ next: (x) => this.applications.set(x) });
  }
  save(): void {
    if (this.form.invalid) return this.form.markAllAsTouched();
    const item = this.editing();
    const raw = this.form.getRawValue();
    const body = {
      ...raw,
      endDate: raw.endDate || null,
      imageUrl: raw.imageUrl || null,
      imageUrls: raw.imageUrls
        .split('\n')
        .map((value) => value.trim())
        .filter(Boolean),
      rateAmount: raw.ratePeriod === 'Negotiable' ? null : raw.rateAmount,
    };
    const request = item
      ? this.http.patch(`${environment.apiUrl}/api/v1/professional/spaces/${item.id}`, body)
      : this.http.post(`${environment.apiUrl}/api/v1/professional/spaces`, body);
    request.subscribe({
      next: () => {
        this.message.set(item ? 'Opportunity updated.' : 'Draft opportunity created. Publish it when ready.');
        this.cancelEdit();
        this.load();
      },
      error: () => this.message.set('The opportunity could not be saved. Check the dates and required fields.'),
    });
  }
  edit(item: SpaceOpportunity): void {
    this.editing.set(item);
    this.form.patchValue({
      ...item,
      endDate: item.endDate ?? '',
      imageUrl: item.imageUrl ?? '',
      imageUrls: item.imageUrls.join('\n'),
      rateAmount: item.rateAmount ?? 0,
    });
  }
  cancelEdit(): void {
    this.editing.set(null);
    this.form.reset({
      workspaceType: 'StylingChair',
      opportunityType: 'ChairRental',
      spacesAvailable: 1,
      rateAmount: 0,
      ratePeriod: 'Day',
      startDate: new Date().toISOString().slice(0, 10),
      recurring: false,
      bringOwnClients: false,
      approvalRequired: true,
    });
  }
  setStatus(item: SpaceOpportunity, status: OpportunityStatus): void {
    this.http.post(`${environment.apiUrl}/api/v1/professional/spaces/${item.id}/status/${status}`, {}).subscribe({
      next: () => {
        this.message.set(`“${item.title}” is now ${status.toLowerCase()}.`);
        this.load();
      },
      error: () => this.message.set('Only approved salons can publish opportunities.'),
    });
  }
  review(application: SpaceApplication, status: 'Accepted' | 'Declined'): void {
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/space-applications/${application.id}/${status}`, {})
      .subscribe({
        next: () => {
          this.message.set(`Application ${status.toLowerCase()}.`);
          this.load();
        },
      });
  }
}
