import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { IonButton, IonContent, IonHeader, IonInput, IonItem, IonTextarea, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { ConfirmationService } from '../../core/ui/confirmation.service';
import { FormBehaviorDirective } from '../../core/ui/form-behavior.directive';

import { ProfessionalNavComponent } from './professional-nav.component';

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
    latitude: number | null;
    longitude: number | null;
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
    FormBehaviorDirective,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonItem,
    IonInput,
    IonTextarea,
    IonButton,
    ProfessionalNavComponent,
  ],
})
export class ProfilePage {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly confirmation = inject(ConfirmationService);
  readonly status = signal('Draft');
  readonly concurrencyToken = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly portfolioImages = signal<PortfolioImage[]>([]);
  readonly locationCoordinates = signal<{ latitude: number; longitude: number } | null>(null);
  readonly locating = signal(false);
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
        if (typeof profile.location.latitude === 'number' && typeof profile.location.longitude === 'number')
          this.locationCoordinates.set({ latitude: profile.location.latitude, longitude: profile.location.longitude });
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
        latitude: this.locationCoordinates()?.latitude ?? null,
        longitude: this.locationCoordinates()?.longitude ?? null,
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

  useCurrentLocation(): void {
    if (typeof navigator === 'undefined' || !navigator.geolocation) {
      this.message.set('Location is not supported on this device.');
      return;
    }
    this.locating.set(true);
    this.message.set('Requesting the studio location…');
    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.locationCoordinates.set({ latitude: position.coords.latitude, longitude: position.coords.longitude });
        this.locating.set(false);
        this.message.set('Studio coordinates captured. Save the profile to apply them.');
      },
      () => {
        this.locating.set(false);
        this.message.set('Location permission was declined or the studio location could not be determined.');
      },
      { enableHighAccuracy: true, timeout: 15_000, maximumAge: 60_000 },
    );
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
  async deletePortfolioImage(image: PortfolioImage): Promise<void> {
    if (
      !(await this.confirmation.confirm(
        'Remove this portfolio image?',
        'Clients will no longer see this image on your studio profile.',
        'Remove image',
      ))
    )
      return;
    this.http.delete(`${environment.apiUrl}/api/v1/professional/portfolio-images/${image.id}`).subscribe({
      next: () => {
        this.message.set('Portfolio image removed.');
        this.loadPortfolio();
      },
      error: () => this.message.set('The portfolio image could not be removed.'),
    });
  }
  private loadPortfolio(): void {
    this.http
      .get<PortfolioImage[]>(`${environment.apiUrl}/api/v1/professional/portfolio-images`)
      .subscribe({ next: (items) => this.portfolioImages.set(items), error: () => undefined });
  }
}
