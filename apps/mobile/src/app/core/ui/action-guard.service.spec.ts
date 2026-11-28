import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { NetworkStatusService } from '../network/network-status.service';

import { ActionGuardService } from './action-guard.service';

describe('ActionGuardService', () => {
  it('blocks offline and duplicate transactional actions', () => {
    const online = signal(false);
    TestBed.configureTestingModule({
      providers: [ActionGuardService, { provide: NetworkStatusService, useValue: { online } }],
    });
    const guard = TestBed.inject(ActionGuardService);
    expect(guard.begin('save')).toBe('offline');
    online.set(true);
    expect(guard.begin('save')).toBe('started');
    expect(guard.begin('save')).toBe('busy');
    guard.end('save');
    expect(guard.begin('save')).toBe('started');
  });
});
