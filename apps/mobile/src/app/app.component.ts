import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router, RouterLink } from '@angular/router';
import { IonApp, IonHeader, IonIcon, IonRouterOutlet, IonToolbar } from '@ionic/angular';
import { addIcons } from 'ionicons';
import {
  calendarClearOutline,
  calendarOutline,
  businessOutline,
  colorPaletteOutline,
  compassOutline,
  cutOutline,
  eyeOutline,
  handLeftOutline,
  locationOutline,
  notificationsOutline,
  optionsOutline,
  personCircleOutline,
  searchOutline,
  shieldCheckmarkOutline,
  sparklesOutline,
  storefrontOutline,
  timeOutline,
} from 'ionicons/icons';

import { environment } from '../environments/environment';

import { AuthService } from './core/auth/auth.service';
import { NotificationService } from './core/notifications/notification.service';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  styleUrls: ['app.component.scss'],
  imports: [RouterLink, IonApp, IonRouterOutlet, IonHeader, IonToolbar, IonIcon],
})
export class AppComponent {
  readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private navigationTimer: ReturnType<typeof setTimeout> | null = null;
  readonly auth = inject(AuthService);
  readonly notifications = inject(NotificationService);
  readonly isDemo = environment.demoMode;
  readonly navigating = signal(false);

  constructor() {
    void this.auth.restoreSession();
    addIcons({
      searchOutline,
      shieldCheckmarkOutline,
      calendarClearOutline,
      calendarOutline,
      businessOutline,
      colorPaletteOutline,
      compassOutline,
      cutOutline,
      eyeOutline,
      handLeftOutline,
      locationOutline,
      notificationsOutline,
      optionsOutline,
      storefrontOutline,
      timeOutline,
      personCircleOutline,
      sparklesOutline,
    });
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.notifications.refresh();
        void this.notifications.syncAndroidPushIfPermitted().catch(() => undefined);
      } else this.notifications.clear();
    });
    this.router.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
      if (event instanceof NavigationStart) {
        this.clearNavigationTimer();
        this.navigationTimer = setTimeout(() => this.navigating.set(true), 120);
      }
      if (event instanceof NavigationEnd || event instanceof NavigationCancel || event instanceof NavigationError) {
        this.clearNavigationTimer();
        this.navigating.set(false);
        if (event instanceof NavigationEnd && this.auth.isAuthenticated()) this.notifications.refresh();
      }
    });
  }

  private clearNavigationTimer(): void {
    if (this.navigationTimer !== null) clearTimeout(this.navigationTimer);
    this.navigationTimer = null;
  }

  isActive(section: 'discover' | 'bookings' | 'spaces' | 'account' | 'studio'): boolean {
    const url = this.router.url;
    return section === 'discover'
      ? url === '/' || url.startsWith('/home') || url.startsWith('/businesses/') || url.startsWith('/book/')
      : section === 'bookings'
        ? url.startsWith('/bookings')
        : section === 'spaces'
          ? url.startsWith('/spaces')
          : section === 'account'
            ? url.startsWith('/auth') || url.startsWith('/account')
            : url.startsWith('/professional') || url.startsWith('/admin');
  }

  workspace(): { label: string; route: string; icon: string } | null {
    if (this.auth.hasRole('Admin')) return { label: 'Admin', route: '/admin', icon: 'shield-checkmark-outline' };
    if (!this.auth.isAuthenticated() || this.auth.hasRole('Professional'))
      return { label: 'Studio', route: '/professional/dashboard', icon: 'storefront-outline' };
    return null;
  }
}
