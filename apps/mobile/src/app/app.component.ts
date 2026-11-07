import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IonApp, IonFooter, IonIcon, IonRouterOutlet } from '@ionic/angular';
import { addIcons } from 'ionicons';
import { calendarClearOutline, personCircleOutline, searchOutline, sparklesOutline } from 'ionicons/icons';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  styleUrls: ['app.component.scss'],
  imports: [RouterLink, IonApp, IonRouterOutlet, IonFooter, IonIcon],
})
export class AppComponent {
  private readonly router = inject(Router);

  constructor() {
    addIcons({ searchOutline, calendarClearOutline, personCircleOutline, sparklesOutline });
  }

  isActive(section: 'discover' | 'bookings' | 'account' | 'studio'): boolean {
    const url = this.router.url;
    return section === 'discover'
      ? url === '/' || url.startsWith('/home') || url.startsWith('/businesses/') || url.startsWith('/book/')
      : section === 'bookings'
        ? url.startsWith('/bookings')
        : section === 'account'
          ? url.startsWith('/auth') || url.startsWith('/account')
          : url.startsWith('/professional');
  }
}
