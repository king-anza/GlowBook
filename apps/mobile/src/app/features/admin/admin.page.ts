import { DatePipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonButton, IonContent, IonHeader, IonInput, IonItem, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { ConfirmationService } from '../../core/ui/confirmation.service';

interface Business {
  id: string;
  tradingName: string;
  description: string;
  status: string;
  location: { suburb: string; city: string; province: string };
  staff: { id: string }[];
}

interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  accountStatus: string;
  createdAtUtc: string;
}

interface Booking {
  id: string;
  businessName: string;
  serviceName: string;
  staffName: string;
  startUtc: string;
  status: string;
}

interface AuditLog {
  id: string;
  action: string;
  subjectType: string;
  subjectId: string;
  details: string | null;
  occurredAtUtc: string;
}

interface ModerationReview {
  id: string;
  businessName: string;
  rating: number;
  comment: string | null;
  status: string;
  createdAtUtc: string;
}

interface ModerationImage {
  id: string;
  businessName: string;
  imageUrl: string;
  altText: string;
  status: string;
  createdAtUtc: string;
}

@Component({
  selector: 'app-admin',
  templateUrl: './admin.page.html',
  imports: [DatePipe, FormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonButton, IonItem, IonInput],
})
export class AdminPage {
  private readonly http = inject(HttpClient);
  private readonly confirmation = inject(ConfirmationService);
  readonly pendingBusinesses = signal<Business[]>([]);
  readonly managedBusinesses = signal<Business[]>([]);
  readonly users = signal<AdminUser[]>([]);
  readonly bookings = signal<Booking[]>([]);
  readonly audits = signal<AuditLog[]>([]);
  readonly reviews = signal<ModerationReview[]>([]);
  readonly portfolioImages = signal<ModerationImage[]>([]);
  readonly busyId = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly loading = signal(true);
  readonly userPage = signal(1);
  readonly auditPage = signal(1);
  readonly pageSize = 20;
  readonly rejectionReasons: Record<string, string> = {};

  constructor() {
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.loadBusinesses();
    this.loadManagedBusinesses();
    this.loadUsers(this.userPage());
    this.loadBookings();
    this.loadAudits(this.auditPage());
    this.loadModeration();
  }

  async approve(business: Business): Promise<void> {
    if (
      !(await this.confirmation.confirm(
        `Approve ${business.tradingName}?`,
        'The business will become visible in public discovery.',
        'Approve business',
      ))
    )
      return;
    this.reviewBusiness(business, 'approve', {});
  }

  async reject(business: Business): Promise<void> {
    const reason = this.rejectionReasons[business.id]?.trim();
    if (!reason) {
      this.message.set('Add a clear rejection reason before rejecting the business.');
      return;
    }
    if (
      !(await this.confirmation.confirm(
        `Reject ${business.tradingName}?`,
        'The professional will need to address the reason and resubmit.',
        'Reject business',
      ))
    )
      return;
    this.reviewBusiness(business, 'reject', { reason });
  }

  async toggleBusinessStatus(business: Business): Promise<void> {
    const action = business.status === 'Suspended' ? 'reactivate' : 'suspend';
    const reason = action === 'suspend' ? this.rejectionReasons[business.id]?.trim() : undefined;
    if (action === 'suspend' && !reason) {
      this.message.set('Add a reason before suspending the business.');
      return;
    }
    if (
      !(await this.confirmation.confirm(
        `${action === 'suspend' ? 'Suspend' : 'Reactivate'} ${business.tradingName}?`,
        action === 'suspend' ? 'It will be removed from public discovery.' : 'It will return to public discovery.',
        action === 'suspend' ? 'Suspend business' : 'Reactivate business',
      ))
    )
      return;
    this.busyId.set(business.id);
    this.http
      .post(`${environment.apiUrl}/api/v1/admin/businesses/${business.id}/${action}`, reason ? { reason } : {})
      .subscribe({
        next: () => {
          this.message.set(`${business.tradingName} was ${action === 'suspend' ? 'suspended' : 'reactivated'}.`);
          this.busyId.set(null);
          this.loadManagedBusinesses();
        },
        error: () => {
          this.message.set('The business status could not be updated.');
          this.busyId.set(null);
        },
      });
  }

  changeUserPage(offset: number): void {
    const page = Math.max(1, this.userPage() + offset);
    this.userPage.set(page);
    this.loadUsers(page);
  }

  changeAuditPage(offset: number): void {
    const page = Math.max(1, this.auditPage() + offset);
    this.auditPage.set(page);
    this.loadAudits(page);
  }

  async moderateReview(review: ModerationReview): Promise<void> {
    const action = review.status === 'Hidden' ? 'restore' : 'hide';
    if (
      !(await this.confirmation.confirm(
        `${action === 'hide' ? 'Hide' : 'Restore'} this review?`,
        'This changes its visibility on the public business profile.',
        action === 'hide' ? 'Hide review' : 'Restore review',
      ))
    )
      return;
    this.moderate(`/api/v1/admin/reviews/${review.id}/${action}`, () => this.loadModeration());
  }

  async moderateImage(image: ModerationImage): Promise<void> {
    const action = image.status === 'Hidden' ? 'restore' : 'hide';
    if (
      !(await this.confirmation.confirm(
        `${action === 'hide' ? 'Hide' : 'Restore'} this image?`,
        'This changes its visibility in the public portfolio.',
        action === 'hide' ? 'Hide image' : 'Restore image',
      ))
    )
      return;
    this.moderate(`/api/v1/admin/portfolio-images/${image.id}/${action}`, () => this.loadModeration());
  }

  private reviewBusiness(business: Business, action: 'approve' | 'reject', body: object): void {
    this.busyId.set(business.id);
    this.message.set(null);
    this.http.post(`${environment.apiUrl}/api/v1/admin/businesses/${business.id}/${action}`, body).subscribe({
      next: () => {
        this.pendingBusinesses.update((items) => items.filter((item) => item.id !== business.id));
        this.message.set(`${business.tradingName} was ${action === 'approve' ? 'approved' : 'rejected'}.`);
        this.busyId.set(null);
      },
      error: () => {
        this.message.set('The review could not be saved. Refresh and try again.');
        this.busyId.set(null);
      },
    });
  }

  private loadBusinesses(): void {
    this.http.get<Business[]>(`${environment.apiUrl}/api/v1/admin/businesses/pending`).subscribe({
      next: (items) => this.pendingBusinesses.set(items),
      error: () => this.message.set('Administrator access is required or applications could not be loaded.'),
    });
  }

  private loadManagedBusinesses(): void {
    this.http.get<Business[]>(`${environment.apiUrl}/api/v1/admin/businesses`).subscribe({
      next: (items) => this.managedBusinesses.set(items.filter((item) => item.status !== 'PendingReview')),
      error: () => this.message.set('Business management data could not be loaded.'),
    });
  }

  private loadUsers(page: number): void {
    this.http
      .get<AdminUser[]>(`${environment.apiUrl}/api/v1/admin/users`, { params: this.pagination(page) })
      .subscribe({
        next: (items) => this.users.set(items),
        error: () => this.message.set('Support users could not be loaded.'),
      });
  }

  private loadBookings(): void {
    this.http.get<Booking[]>(`${environment.apiUrl}/api/v1/admin/bookings`).subscribe({
      next: (items) => this.bookings.set(items),
      error: () => this.message.set('Bookings could not be loaded.'),
    });
  }

  private loadAudits(page: number): void {
    this.http
      .get<AuditLog[]>(`${environment.apiUrl}/api/v1/admin/audit-logs`, { params: this.pagination(page) })
      .subscribe({
        next: (items) => {
          this.audits.set(items);
          this.loading.set(false);
        },
        error: () => {
          this.message.set('Audit activity could not be loaded.');
          this.loading.set(false);
        },
      });
  }

  private loadModeration(): void {
    this.http.get<ModerationReview[]>(`${environment.apiUrl}/api/v1/admin/reviews`).subscribe({
      next: (items) => this.reviews.set(items),
      error: () => this.message.set('Review moderation could not be loaded.'),
    });
    this.http.get<ModerationImage[]>(`${environment.apiUrl}/api/v1/admin/portfolio-images`).subscribe({
      next: (items) => this.portfolioImages.set(items),
      error: () => this.message.set('Portfolio moderation could not be loaded.'),
    });
  }

  private moderate(path: string, complete: () => void): void {
    this.http.post(`${environment.apiUrl}${path}`, {}).subscribe({
      next: () => {
        this.message.set('Moderation status updated.');
        complete();
      },
      error: () => this.message.set('The moderation status could not be updated.'),
    });
  }

  private pagination(page: number): HttpParams {
    return new HttpParams().set('page', page).set('pageSize', this.pageSize);
  }
}
