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
  IonSelect,
  IonSelectOption,
  IonTextarea,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { environment } from '../../../environments/environment';

interface Category {
  id: string;
  name: string;
}
interface Staff {
  id: string;
  displayName: string;
}
interface Offering {
  id: string;
  staffName: string;
  price: number;
  durationMinutes: number;
  isActive: boolean;
}
interface ServiceItem {
  id: string;
  name: string;
  categoryName: string;
  isActive: boolean;
  offerings: Offering[];
}
interface Business {
  staff: Staff[];
}

@Component({
  selector: 'app-services',
  templateUrl: './services.page.html',
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
    IonSelect,
    IonSelectOption,
    IonButton,
  ],
})
export class ServicesPage {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  readonly categories = signal<Category[]>([]);
  readonly staff = signal<Staff[]>([]);
  readonly services = signal<ServiceItem[]>([]);
  readonly message = signal<string | null>(null);
  readonly serviceForm = this.fb.nonNullable.group({
    categoryId: ['', Validators.required],
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
  });
  readonly staffForm = this.fb.nonNullable.group({
    displayName: ['', Validators.required],
    biography: [''],
    acceptingBookings: [true],
  });
  readonly offeringForm = this.fb.nonNullable.group({
    serviceId: ['', Validators.required],
    staffMemberId: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
    durationMinutes: [30, [Validators.required, Validators.min(5)]],
    bufferMinutes: [0, Validators.min(0)],
  });

  constructor() {
    this.load();
  }
  saveService(): void {
    if (this.serviceForm.invalid) return this.serviceForm.markAllAsTouched();
    this.http.post(`${environment.apiUrl}/api/v1/professional/services`, this.serviceForm.getRawValue()).subscribe({
      next: () => {
        this.serviceForm.reset();
        this.message.set('Service created.');
        this.loadServices();
      },
      error: () => this.message.set('Unable to create the service.'),
    });
  }
  addStaff(): void {
    if (this.staffForm.invalid) return this.staffForm.markAllAsTouched();
    this.http.post(`${environment.apiUrl}/api/v1/professional/staff`, this.staffForm.getRawValue()).subscribe({
      next: () => {
        this.staffForm.reset({ acceptingBookings: true });
        this.message.set('Staff member added.');
        this.loadBusiness();
      },
      error: () => this.message.set('Unable to add staff.'),
    });
  }
  addOffering(): void {
    if (this.offeringForm.invalid) return this.offeringForm.markAllAsTouched();
    const value = this.offeringForm.getRawValue();
    const { serviceId, ...body } = value;
    this.http.post(`${environment.apiUrl}/api/v1/professional/services/${serviceId}/offerings`, body).subscribe({
      next: () => {
        this.message.set('Price and duration added.');
        this.loadServices();
      },
      error: () => this.message.set('Unable to add the offering.'),
    });
  }
  toggleService(item: ServiceItem): void {
    const action = item.isActive ? 'deactivate' : 'activate';
    this.http
      .post(`${environment.apiUrl}/api/v1/professional/services/${item.id}/${action}`, {})
      .subscribe(() => this.loadServices());
  }
  private load(): void {
    this.http
      .get<Category[]>(`${environment.apiUrl}/api/v1/categories`)
      .subscribe((items) => this.categories.set(items));
    this.loadBusiness();
    this.loadServices();
  }
  private loadBusiness(): void {
    this.http
      .get<Business>(`${environment.apiUrl}/api/v1/professional/business`)
      .subscribe((item) => this.staff.set(item.staff));
  }
  private loadServices(): void {
    this.http
      .get<ServiceItem[]>(`${environment.apiUrl}/api/v1/professional/services`)
      .subscribe((items) => this.services.set(items));
  }
}
