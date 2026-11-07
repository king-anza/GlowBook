import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'za.co.glowbook.app',
  appName: 'GlowBook',
  webDir: 'www',
  includePlugins: ['capacitor-token-vault'],
};

export default config;
