import { Injectable, signal } from '@angular/core';

type NetworkInformation = EventTarget & { effectiveType?: string; saveData?: boolean };

@Injectable({ providedIn: 'root' })
export class NetworkStatusService {
  readonly online = signal(typeof navigator === 'undefined' ? true : navigator.onLine);
  readonly weakConnection = signal(false);
  readonly usingCachedData = signal(false);

  constructor() {
    if (typeof window === 'undefined') return;
    window.addEventListener('online', () => {
      this.online.set(true);
      this.readConnection();
    });
    window.addEventListener('offline', () => this.online.set(false));
    const connection = this.connection();
    connection?.addEventListener('change', () => this.readConnection());
    this.readConnection();
  }

  markCachedData(value: boolean): void {
    this.usingCachedData.set(value);
  }

  private readConnection(): void {
    const connection = this.connection();
    this.weakConnection.set(
      connection?.saveData === true || ['slow-2g', '2g'].includes(connection?.effectiveType ?? ''),
    );
  }

  private connection(): NetworkInformation | undefined {
    return (navigator as Navigator & { connection?: NetworkInformation }).connection;
  }
}
