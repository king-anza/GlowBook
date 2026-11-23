import type { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  IonButton,
  IonCheckbox,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonLabel,
  IonList,
  IonSegment,
  IonSegmentButton,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';

import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-auth',
  templateUrl: './auth.page.html',
  styleUrls: ['./auth.page.scss'],
  imports: [
    ReactiveFormsModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonSegment,
    IonSegmentButton,
    IonLabel,
    IonList,
    IonItem,
    IonInput,
    IonCheckbox,
    IonButton,
    RouterLink,
  ],
})
export class AuthPage {
  private readonly formBuilder = inject(FormBuilder);
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly mode = signal<'login' | 'register'>('login');
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly form = this.formBuilder.nonNullable.group({
    displayName: ['', [Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(10)]],
    professional: [false],
    acceptTerms: [false],
    confirmMinimumAge: [false],
  });

  submit(): void {
    const value = this.form.getRawValue();
    if (
      this.form.invalid ||
      (this.mode() === 'register' &&
        (!value.displayName.trim() ||
          !value.acceptTerms ||
          !value.confirmMinimumAge ||
          !/[A-Z]/.test(value.password) ||
          !/[a-z]/.test(value.password) ||
          !/\d/.test(value.password) ||
          !/[^A-Za-z0-9]/.test(value.password)))
    ) {
      this.form.markAllAsTouched();
      if (this.mode() === 'register')
        this.error.set('Enter a display name and a password with uppercase, lowercase, a number, and a symbol.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    if (this.mode() === 'login') {
      this.auth.login(value.email, value.password).subscribe({
        next: (tokens) => {
          this.auth.setSession(tokens);
          void this.router.navigateByUrl('/home');
        },
        error: (error: HttpErrorResponse) => {
          this.error.set(this.apiError(error, 'Sign-in failed. Check your details and email verification.'));
          this.busy.set(false);
        },
      });
      return;
    }
    this.auth.register(value.email, value.password, value.displayName, value.professional).subscribe({
      next: () => {
        this.mode.set('login');
        this.busy.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.error.set(this.apiError(error, 'Registration failed. Please review your details.'));
        this.busy.set(false);
      },
    });
  }

  signOut(): void {
    this.auth.logout().subscribe({
      next: () => this.messageAfterSignOut(),
      error: () => {
        this.auth.clearSession();
        this.messageAfterSignOut();
      },
    });
  }

  private messageAfterSignOut(): void {
    this.error.set(null);
    this.busy.set(false);
  }

  private apiError(error: HttpErrorResponse, fallback: string): string {
    const errors = error.error?.errors;
    return Array.isArray(errors) && errors.length ? errors.join(' ') : fallback;
  }
}
