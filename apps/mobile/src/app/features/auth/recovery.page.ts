import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonInput, IonItem, IonList, IonTitle, IonToolbar } from '@ionic/angular';

import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-recovery',
  templateUrl: './recovery.page.html',
  imports: [FormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonItem, IonInput, IonButton],
})
export class RecoveryPage {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  readonly message = signal<string | null>(null);
  email = this.route.snapshot.queryParamMap.get('email') ?? '';
  userId = '';
  token = '';
  newPassword = '';
  requestVerification(): void {
    this.post(
      '/resend-verification',
      { email: this.email },
      'If the account exists, a verification email has been sent.',
    );
  }
  requestReset(): void {
    this.post(
      '/forgot-password',
      { email: this.email },
      'If the account exists, password-reset instructions have been sent.',
    );
  }
  verify(): void {
    this.post('/verify-email', { userId: this.userId, token: this.token }, 'Email verified. You can now sign in.');
  }
  reset(): void {
    this.post(
      '/reset-password',
      { email: this.email, token: this.token, newPassword: this.newPassword },
      'Password reset. You can now sign in.',
    );
  }
  private post(path: string, body: object, success: string): void {
    this.http.post(`${environment.apiUrl}/api/v1/auth${path}`, body).subscribe({
      next: () => this.message.set(success),
      error: () => this.message.set('The request could not be completed. Check the supplied token and fields.'),
    });
  }
}
