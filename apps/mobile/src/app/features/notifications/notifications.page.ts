import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { IonButton, IonCheckbox, IonContent, IonHeader, IonItem, IonList, IonTitle, IonToolbar } from '@ionic/angular';

import { NotificationService } from '../../core/notifications/notification.service';
import { FeedbackBannerComponent } from '../../core/ui/feedback-banner.component';

@Component({
  selector: 'app-notifications',
  templateUrl: './notifications.page.html',
  styleUrls: ['./notifications.page.scss'],
  imports: [
    DatePipe,
    ReactiveFormsModule,
    IonButton,
    IonCheckbox,
    IonContent,
    IonHeader,
    IonItem,
    IonList,
    IonTitle,
    IonToolbar,
    FeedbackBannerComponent,
  ],
})
export class NotificationsPage {
  private readonly fb = inject(FormBuilder);
  readonly notifications = inject(NotificationService);
  readonly status = signal<string | null>(null);
  readonly settings = this.fb.nonNullable.group({
    bookingPush: true,
    applicationPush: true,
    accountPush: true,
    reminderPush: true,
    emailFallback: true,
  });

  constructor() {
    this.notifications.refresh();
    this.notifications.preferences().subscribe({ next: (value) => this.settings.patchValue(value) });
  }
  save(): void {
    this.notifications
      .updatePreferences(this.settings.getRawValue())
      .subscribe({ next: () => this.status.set('Notification preferences saved.') });
  }
  async enablePush(): Promise<void> {
    try {
      const result = await this.notifications.enableAndroidPush();
      this.status.set(
        result === 'enabled'
          ? 'Android push notifications are enabled.'
          : result === 'denied'
            ? 'Push permission was not granted. In-app and email fallbacks remain active.'
            : 'Push setup is available in the installed Android app. In-app and email fallbacks remain active here.',
      );
    } catch {
      this.status.set('Push could not be registered. In-app and email fallbacks remain active.');
    }
  }
  disconnectPush(): void {
    const request = this.notifications.revokeCurrentDevice();
    if (!request) {
      this.status.set('This device is not registered for push notifications.');
      return;
    }
    request.subscribe({
      next: () => this.status.set('This device was disconnected from push notifications.'),
      error: () => this.status.set('The device could not be disconnected. Please try again.'),
    });
  }
}
