import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonButton, IonIcon } from '@ionic/angular';

@Component({
  selector: 'app-empty-state',
  imports: [RouterLink, IonButton, IonIcon],
  template: `
    <section class="empty-state empty-state--guided">
      <ion-icon class="empty-state__icon" [name]="icon()" aria-hidden="true" />
      <h2>{{ title() }}</h2>
      <p>{{ description() }}</p>
      @if (actionLabel() && actionLink()) {
        <a class="empty-state__action" [routerLink]="actionLink()">{{ actionLabel() }}</a>
      } @else if (actionLabel()) {
        <ion-button size="small" fill="outline" (click)="action.emit()">{{ actionLabel() }}</ion-button>
      }
    </section>
  `,
})
export class EmptyStateComponent {
  readonly icon = input('sparkles-outline');
  readonly title = input.required<string>();
  readonly description = input.required<string>();
  readonly actionLabel = input('');
  readonly actionLink = input('');
  readonly action = output<void>();
}
