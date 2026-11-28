import { HttpClient, type HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import type { OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonInput, IonItem, IonList, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-recovery',
  templateUrl: './recovery.page.html',
  imports: [FormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonItem, IonInput, IonButton],
})
export class RecoveryPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);
  email = '';
  userId = '';
  token = '';
  newPassword = '';
  action: 'verify' | 'reset' | null = null;

  ngOnInit(): void {
    const query = this.route.snapshot.queryParamMap;
    this.email = query.get('email') ?? '';
    this.userId = query.get('userId') ?? '';
    this.token = query.get('token') ?? '';
    const action = query.get('action');
    this.action = action === 'verify' || action === 'reset' ? action : null;
    if (this.action === 'verify' && this.userId && this.token) this.verify();
  }
  requestVerification(): void {
    if (!this.validEmail()) return;
    this.post(
      '/resend-verification',
      { email: this.email },
      'If the account exists, a verification email has been sent.',
    );
  }
  requestReset(): void {
    if (!this.validEmail()) return;
    this.post(
      '/forgot-password',
      { email: this.email },
      'If the account exists, password-reset instructions have been sent.',
    );
  }
  verify(): void {
    if (!this.userId.trim() || !this.token.trim()) {
      this.message.set('Enter the user ID and token from your verification email.');
      return;
    }
    this.post('/verify-email', { userId: this.userId, token: this.token }, 'Email verified. You can now sign in.');
  }
  reset(): void {
    if (!this.validEmail() || !this.token.trim() || this.newPassword.length < 10) {
      this.message.set('Enter the account email, reset token and a new password of at least 10 characters.');
      return;
    }
    this.post(
      '/reset-password',
      { email: this.email, token: this.token, newPassword: this.newPassword },
      'Password reset. You can now sign in.',
    );
  }
  private post(path: string, body: object, success: string): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.http.post(`${environment.apiUrl}/api/v1/auth${path}`, body).subscribe({
      next: () => {
        this.message.set(success);
        this.busy.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.message.set(
          error.status === 0
            ? 'Reconnect and try again. The request was not queued.'
            : 'The request could not be completed. Check the supplied token and fields.',
        );
        this.busy.set(false);
      },
    });
  }
  private validEmail(): boolean {
    if (/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email.trim())) return true;
    this.message.set('Enter a complete email address, for example name@example.com.');
    return false;
  }
}
