import { Injectable } from '@angular/core';
import { TokenVault } from 'capacitor-token-vault';

@Injectable({ providedIn: 'root' })
export class SessionVaultService {
  async loadRefreshToken(): Promise<string | null> {
    return (await TokenVault.getToken()).value;
  }

  async saveRefreshToken(value: string): Promise<void> {
    await TokenVault.setToken({ value });
  }

  async clear(): Promise<void> {
    await TokenVault.clear();
  }
}
