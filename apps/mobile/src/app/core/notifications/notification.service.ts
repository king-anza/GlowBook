import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Capacitor } from '@capacitor/core';
import { PushNotifications, type Token } from '@capacitor/push-notifications';
import { firstValueFrom, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface AppNotification {
  id: string;
  eventType: string;
  title: string;
  message: string;
  actionUrl: string;
  createdAtUtc: string;
  readAtUtc: string | null;
}
export interface NotificationPreferences {
  bookingPush: boolean;
  applicationPush: boolean;
  accountPush: boolean;
  reminderPush: boolean;
  emailFallback: boolean;
  pushAvailable: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  readonly items = signal<AppNotification[]>([]);
  readonly unreadCount = signal(0);

  refresh(): void {
    this.http
      .get<{ items: AppNotification[]; unreadCount: number }>(`${environment.apiUrl}/api/v1/notifications`)
      .subscribe({
        next: (value) => {
          this.items.set(value.items);
          this.unreadCount.set(value.unreadCount);
        },
      });
  }

  clear(): void {
    this.items.set([]);
    this.unreadCount.set(0);
  }

  async open(item: AppNotification): Promise<void> {
    if (!item.readAtUtc) {
      await firstValueFrom(this.http.post<void>(`${environment.apiUrl}/api/v1/notifications/${item.id}/read`, {}));
      this.items.update((items) =>
        items.map((candidate) =>
          candidate.id === item.id ? { ...candidate, readAtUtc: new Date().toISOString() } : candidate,
        ),
      );
      this.unreadCount.update((count) => Math.max(0, count - 1));
    }
    await this.router.navigateByUrl(item.actionUrl || '/home');
  }

  markAllRead(): void {
    this.http.post<void>(`${environment.apiUrl}/api/v1/notifications/read-all`, {}).subscribe({
      next: () => {
        this.items.update((items) =>
          items.map((item) => ({ ...item, readAtUtc: item.readAtUtc ?? new Date().toISOString() })),
        );
        this.unreadCount.set(0);
      },
    });
  }

  preferences(): Observable<NotificationPreferences> {
    return this.http.get<NotificationPreferences>(`${environment.apiUrl}/api/v1/notifications/preferences`);
  }
  updatePreferences(value: Omit<NotificationPreferences, 'pushAvailable'>): Observable<NotificationPreferences> {
    return this.http.put<NotificationPreferences>(`${environment.apiUrl}/api/v1/notifications/preferences`, value);
  }

  async enableAndroidPush(): Promise<'enabled' | 'denied' | 'unavailable'> {
    if (!Capacitor.isNativePlatform() || Capacitor.getPlatform() !== 'android') return 'unavailable';
    let permission = await PushNotifications.checkPermissions();
    if (permission.receive === 'prompt') permission = await PushNotifications.requestPermissions();
    if (permission.receive !== 'granted') return 'denied';
    await this.registerNativeToken();
    return 'enabled';
  }

  async syncAndroidPushIfPermitted(): Promise<void> {
    if (!Capacitor.isNativePlatform() || Capacitor.getPlatform() !== 'android') return;
    if ((await PushNotifications.checkPermissions()).receive === 'granted') await this.registerNativeToken();
  }

  revokeCurrentDevice(): Observable<void> | null {
    const id = localStorage.getItem('glowbook-push-device-id');
    if (!id) return null;
    return new Observable<void>((subscriber) => {
      this.http.delete<void>(`${environment.apiUrl}/api/v1/notifications/devices/${id}`).subscribe({
        next: () => {
          localStorage.removeItem('glowbook-push-device-id');
          subscriber.next();
          subscriber.complete();
        },
        error: (error) => subscriber.error(error),
      });
    });
  }

  private async registerNativeToken(): Promise<void> {
    let resolveToken!: (value: Token) => void;
    let rejectToken!: (reason: unknown) => void;
    const tokenResult = new Promise<Token>((resolve, reject) => {
      resolveToken = resolve;
      rejectToken = reject;
    });
    const timeout = setTimeout(() => rejectToken(new Error('Push registration timed out')), 15000);
    const registered = await PushNotifications.addListener('registration', (value) => {
      clearTimeout(timeout);
      resolveToken(value);
    });
    const failed = await PushNotifications.addListener('registrationError', (error) => {
      clearTimeout(timeout);
      rejectToken(error);
    });
    await PushNotifications.register();
    let token: Token;
    try {
      token = await tokenResult;
    } finally {
      await registered.remove();
      await failed.remove();
    }
    const device = await firstValueFrom(
      this.http.post<{ id: string }>(`${environment.apiUrl}/api/v1/notifications/devices`, {
        token: token.value,
        platform: 'android',
      }),
    );
    localStorage.setItem('glowbook-push-device-id', device.id);
  }
}
