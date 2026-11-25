import { Component, input, output } from '@angular/core';
import { IonButton, IonIcon } from '@ionic/angular';

export type FeedbackTone = 'error' | 'success' | 'info';

@Component({
  selector: 'app-feedback-banner',
  imports: [IonButton, IonIcon],
  template: `
    <section
      class="notice feedback-banner"
      [class.notice--error]="tone() === 'error'"
      [class.notice--success]="tone() === 'success'"
      [class.notice--info]="tone() === 'info'"
      [attr.role]="tone() === 'error' ? 'alert' : 'status'"
      [attr.aria-live]="tone() === 'error' ? 'assertive' : 'polite'"
    >
      <ion-icon [name]="iconName()" aria-hidden="true" />
      <div class="feedback-banner__copy">
        @if (title()) {
          <strong>{{ title() }}</strong>
        }
        <span>{{ message() }}</span>
      </div>
      @if (actionLabel()) {
        <ion-button size="small" fill="outline" (click)="action.emit()">{{ actionLabel() }}</ion-button>
      }
      @if (dismissible()) {
        <ion-button
          class="feedback-banner__dismiss"
          size="small"
          fill="clear"
          aria-label="Dismiss message"
          (click)="dismiss.emit()"
          >×</ion-button
        >
      }
    </section>
  `,
})
export class FeedbackBannerComponent {
  readonly tone = input<FeedbackTone>('info');
  readonly title = input('');
  readonly message = input.required<string>();
  readonly actionLabel = input('');
  readonly dismissible = input(false);
  readonly action = output<void>();
  readonly dismiss = output<void>();

  iconName(): string {
    if (this.tone() === 'error') return 'alert-circle-outline';
    if (this.tone() === 'success') return 'checkmark-circle-outline';
    return 'information-circle-outline';
  }
}
