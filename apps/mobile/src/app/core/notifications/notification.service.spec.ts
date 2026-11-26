import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { environment } from '../../../environments/environment';

import { type AppNotification, NotificationService } from './notification.service';

describe('NotificationService', () => {
  it('loads unread state and marks a direct-link notification read before navigation', async () => {
    await TestBed.configureTestingModule({
      providers: [NotificationService, provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    const service = TestBed.inject(NotificationService);
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigation = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    const item: AppNotification = {
      id: 'note-1',
      eventType: 'BookingConfirmed',
      title: 'Confirmed',
      message: 'Ready',
      actionUrl: '/bookings',
      createdAtUtc: '2026-11-26T08:00:00Z',
      readAtUtc: null,
    };
    service.items.set([item]);
    service.unreadCount.set(1);

    const opened = service.open(item);
    http.expectOne(`${environment.apiUrl}/api/v1/notifications/note-1/read`).flush(null);
    await opened;

    expect(service.unreadCount()).toBe(0);
    expect(navigation).toHaveBeenCalledWith('/bookings');
    http.verify();
  });
});
