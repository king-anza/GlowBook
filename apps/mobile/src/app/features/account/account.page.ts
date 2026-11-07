import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
  ],
})
export class AccountPage {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  readonly account = signal<AccountData | null>(null);
  readonly message = signal<string | null>(null);
  email = '';
  displayName = '';
  marketingConsent = false;
  constructor() {
    this.load();
  }
  save(): void {
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
        },
        error: () => this.message.set('Unable to update the profile.'),
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
  deleteAccount(): void {
    if (!globalThis.confirm('Anonymise your GlowBook account? This cannot be undone.')) return;
    this.http.delete(`${environment.apiUrl}/api/v1/me`).subscribe(() => {
      this.auth.clearSession();
      void this.router.navigateByUrl('/auth');
    });
  }
  private load(): void {
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
