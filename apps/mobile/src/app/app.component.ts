import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IonApp, IonHeader, IonIcon, IonRouterOutlet, IonToolbar } from '@ionic/angular';
import { addIcons } from 'ionicons';
import {
  calendarClearOutline,
  calendarOutline,
  colorPaletteOutline,
  compassOutline,
  cutOutline,
  eyeOutline,
  handLeftOutline,
  locationOutline,
  optionsOutline,
  personCircleOutline,
  searchOutline,
  sparklesOutline,
  storefrontOutline,
} from 'ionicons/icons';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  styleUrls: ['app.component.scss'],
  imports: [RouterLink, IonApp, IonRouterOutlet, IonHeader, IonToolbar, IonIcon],
})
export class AppComponent {
  private readonly router = inject(Router);

  constructor() {
    addIcons({
      searchOutline,
      calendarClearOutline,
      calendarOutline,
      colorPaletteOutline,
      compassOutline,
      cutOutline,
      eyeOutline,
      handLeftOutline,
      locationOutline,
      optionsOutline,
      storefrontOutline,
      personCircleOutline,
      sparklesOutline,
    });
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
