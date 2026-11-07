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
  IonTextarea,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { environment } from '../../../environments/environment';

interface BusinessProfile {
  tradingName: string;
  description: string;
  cancellationPolicy: string;
  publicEmail: string | null;
  publicPhone: string | null;
  status: string;
  concurrencyToken: string;
  location: {
    addressLine1: string;
    addressLine2: string | null;
    suburb: string;
    city: string;
    province: string;
    postalCode: string;
  };
}
interface PortfolioImage {
  id: string;
  imageUrl: string;
  altText: string;
  displayOrder: number;
  status: string;
}

@Component({
  selector: 'app-professional-profile',
  templateUrl: './profile.page.html',
  styleUrls: ['./profile.page.scss'],
  imports: [
    ReactiveFormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonInput,
    IonTextarea,
    IonButton,
  ],
})
export class ProfilePage {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  readonly status = signal('Draft');
  readonly concurrencyToken = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly portfolioImages = signal<PortfolioImage[]>([]);
  readonly form = this.fb.nonNullable.group({
    tradingName: ['', Validators.required],
    description: [''],
    cancellationPolicy: [''],
    publicEmail: ['', Validators.email],
    publicPhone: [''],
    addressLine1: ['', Validators.required],
    addressLine2: [''],
    suburb: ['', Validators.required],
    city: ['', Validators.required],
    province: ['', Validators.required],
    postalCode: ['', [Validators.required, Validators.pattern(/^\d{4}$/)]],
  });
  readonly portfolioForm = this.fb.nonNullable.group({
    imageUrl: ['', [Validators.required, Validators.pattern(/^https:\/\//)]],
    altText: ['', [Validators.required, Validators.maxLength(200)]],
    displayOrder: [0, [Validators.required, Validators.min(0), Validators.max(9)]],
  });

  constructor() {
    this.http.get<BusinessProfile>(`${environment.apiUrl}/api/v1/professional/business`).subscribe({
      next: (profile) => {
        this.status.set(profile.status);
        this.concurrencyToken.set(profile.concurrencyToken);
        this.form.setValue({
          tradingName: profile.tradingName,
          description: profile.description,
          cancellationPolicy: profile.cancellationPolicy,
          publicEmail: profile.publicEmail ?? '',
          publicPhone: profile.publicPhone ?? '',
          addressLine1: profile.location.addressLine1,
          addressLine2: profile.location.addressLine2 ?? '',
          suburb: profile.location.suburb,
          city: profile.location.city,
          province: profile.location.province,
          postalCode: profile.location.postalCode,
        });
      },
      error: () => undefined,
    });
    this.loadPortfolio();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const body = {
      tradingName: value.tradingName,
      description: value.description,
      cancellationPolicy: value.cancellationPolicy,
      publicEmail: value.publicEmail || null,
      publicPhone: value.publicPhone || null,
      location: {
        addressLine1: value.addressLine1,
        addressLine2: value.addressLine2 || null,
        suburb: value.suburb,
        city: value.city,
        province: value.province,
        postalCode: value.postalCode,
      },
      concurrencyToken: this.concurrencyToken(),
    };
    this.http
      .request<{ status: string; concurrencyToken: string }>(
        this.concurrencyToken() ? 'PATCH' : 'POST',
        `${environment.apiUrl}/api/v1/professional/business`,
        { body },
      )
      .subscribe({
        next: (profile) => {
          this.status.set(profile.status);
          this.concurrencyToken.set(profile.concurrencyToken);
          this.message.set('Profile saved.');
        },
        error: () => this.message.set('Unable to save the profile.'),
      });
  }

  submitForReview(): void {
    this.http.post<void>(`${environment.apiUrl}/api/v1/professional/business/submit`, {}).subscribe({
      next: () => {
        this.status.set('PendingReview');
        this.message.set('Submitted for review.');
      },
      error: () => this.message.set('Complete and save the profile before submitting.'),
    });
  }

  addPortfolioImage(): void {
    if (this.portfolioForm.invalid) {
      this.portfolioForm.markAllAsTouched();
      return;
    }
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/portfolio-images`, this.portfolioForm.getRawValue())
      .subscribe({
        next: () => {
          this.portfolioForm.reset({ displayOrder: 0 });
          this.message.set('Portfolio image added.');
          this.loadPortfolio();
        },
        error: () => this.message.set('Unable to add the image. Use a unique position from 0 to 9.'),
      });
  }
  deletePortfolioImage(image: PortfolioImage): void {
    this.http.delete(`${environment.apiUrl}/api/v1/professional/portfolio-images/${image.id}`).subscribe(() => {
      this.message.set('Portfolio image removed.');
      this.loadPortfolio();
    });
  }
  private loadPortfolio(): void {
    this.http
      .get<PortfolioImage[]>(`${environment.apiUrl}/api/v1/professional/portfolio-images`)
      .subscribe({ next: (items) => this.portfolioImages.set(items), error: () => undefined });
  }
}
