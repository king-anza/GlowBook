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
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);
  readonly form = this.formBuilder.nonNullable.group({
    displayName: ['', [Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(10)]],
    professional: [false],
    acceptTerms: [false],
    confirmMinimumAge: [false],
  });

  get passwordRequirements(): { label: string; met: boolean }[] {
    const password = this.form.controls.password.value;
    return [
      { label: 'At least 10 characters', met: password.length >= 10 },
      { label: 'One uppercase letter (A–Z)', met: /[A-Z]/.test(password) },
      { label: 'One lowercase letter (a–z)', met: /[a-z]/.test(password) },
      { label: 'One number (0–9)', met: /\d/.test(password) },
      { label: 'One symbol, such as !, @, # or ?', met: /[^A-Za-z0-9]/.test(password) },
    ];
  }

  get emailHasValue(): boolean {
    return this.form.controls.email.value.trim().length > 0;
  }

  get emailIsValid(): boolean {
    return this.emailHasValue && this.form.controls.email.valid;
  }

  changeMode(mode: 'login' | 'register'): void {
    this.mode.set(mode);
    this.error.set(null);
    this.message.set(null);
    this.form.controls.password.setValue('');
  }

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
      this.error.set(this.validationMessage(value));
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
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
        this.changeMode('login');
        this.form.controls.email.setValue(value.email);
        this.message.set('Account created. Check your email for the verification link before signing in.');
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
    const code = error.error?.code;
    if (code === 'invalid_credentials') return 'The email address or password is incorrect.';
    if (code === 'email_not_verified')
      return 'Your email address has not been verified. Use “Verify email or reset password” below.';
    if (code === 'password_requirements_not_met') return 'The password does not meet every requirement shown above.';
    if (code === 'registration_failed')
      return 'Registration could not be completed. Try signing in, or resend the verification email if you registered before.';
    const errors = error.error?.errors;
    return Array.isArray(errors) && errors.length ? errors.join(' ') : fallback;
  }

  private validationMessage(value: ReturnType<typeof this.form.getRawValue>): string {
    if (!this.emailIsValid)
      return 'Enter a complete email address in the correct format, for example name@example.com.';
    if (this.mode() === 'login') return 'Enter your password to sign in.';
    if (!value.displayName.trim()) return 'Enter the name you want displayed on your Glow account.';
    if (this.passwordRequirements.some((requirement) => !requirement.met))
      return 'Complete every password requirement marked with an X.';
    if (!value.acceptTerms) return 'Accept the terms and privacy notice to create your account.';
    if (!value.confirmMinimumAge) return 'Confirm that you are at least 18 to create your account.';
    return 'Review the highlighted fields and try again.';
  }
}
