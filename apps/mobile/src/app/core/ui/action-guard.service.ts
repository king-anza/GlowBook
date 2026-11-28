import { inject, Injectable, signal } from '@angular/core';

import { NetworkStatusService } from '../network/network-status.service';

@Injectable({ providedIn: 'root' })
export class ActionGuardService {
  private readonly active = signal<ReadonlySet<string>>(new Set());
  private readonly network = inject(NetworkStatusService);

  begin(key: string): 'started' | 'busy' | 'offline' {
    if (this.active().has(key)) return 'busy';
    if (!this.network.online()) return 'offline';
    this.active.update((items) => new Set([...items, key]));
    return 'started';
  }

  end(key: string): void {
    this.active.update((items) => {
      const next = new Set(items);
      next.delete(key);
      return next;
    });
  }

  busy(key: string): boolean {
    return this.active().has(key);
  }
}
