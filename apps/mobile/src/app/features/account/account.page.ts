import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  IonButton,
  IonCheckbox,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonList,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmationService } from '../../core/ui/confirmation.service';

interface AccountData {
  id: string;
  email: string;
  displayName: string;
  marketingConsent: boolean;
  accountStatus: string;
  createdAtUtc: string;
  termsVersion: string;
  privacyNoticeVersion: string;
}

interface CurrentUser {
  roles: string[];
}

@Component({
  selector: 'app-account',
  templateUrl: './account.page.html',
  imports: [
    FormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonInput,
    IonCheckbox,
    IonButton,
    RouterLink,
  ],
})
export class AccountPage {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);
  private readonly confirmation = inject(ConfirmationService);
  readonly account = signal<AccountData | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);
  readonly isAdmin = signal(false);
  email = '';
  displayName = '';
  marketingConsent = false;
  constructor() {
    this.load();
  }
  save(): void {
    this.busy.set(true);
    this.message.set(null);
    const previousEmail = this.account()?.email;
    this.http
      .patch<AccountData>(`${environment.apiUrl}/api/v1/me`, {
        email: this.email,
        displayName: this.displayName,
        marketingConsent: this.marketingConsent,
      })
      .subscribe({
        next: (data) => {
          this.account.set(data);
          if (previousEmail && previousEmail.toLowerCase() !== data.email.toLowerCase()) {
            this.auth.clearSession();
            void this.router.navigate(['/auth/recovery'], { queryParams: { email: data.email } });
          } else {
            this.message.set('Profile updated.');
          }
          this.busy.set(false);
        },
        error: () => {
          this.message.set('Unable to update the profile.');
          this.busy.set(false);
        },
      });
  }
  exportData(): void {
    this.http.get<AccountData>(`${environment.apiUrl}/api/v1/me/data`).subscribe((data) => {
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = 'glowbook-account-data.json';
      link.click();
      URL.revokeObjectURL(link.href);
    });
  }
  async deleteAccount(): Promise<void> {
    if (
      !(await this.confirmation.confirm(
        'Anonymise your account?',
        'Your sign-in details and personal profile will be permanently removed. This cannot be undone.',
        'Anonymise account',
      ))
    )
      return;
    this.busy.set(true);
    this.http.delete(`${environment.apiUrl}/api/v1/me`).subscribe({
      next: () => {
        this.auth.clearSession();
        void this.router.navigateByUrl('/auth');
      },
      error: () => {
        this.message.set('Your account could not be anonymised. Please try again.');
        this.busy.set(false);
      },
    });
  }
  private load(): void {
    this.http.get<CurrentUser>(`${environment.apiUrl}/api/v1/me`).subscribe({
      next: (user) => this.isAdmin.set(user.roles.some((role) => role.toLowerCase() === 'admin')),
      error: () => this.isAdmin.set(false),
    });
    this.http.get<AccountData>(`${environment.apiUrl}/api/v1/me/data`).subscribe({
      next: (data) => {
        this.account.set(data);
        this.email = data.email;
        this.displayName = data.displayName;
        this.marketingConsent = data.marketingConsent;
      },
      error: () => this.message.set('Sign in to manage your profile.'),
    });
  }
}
