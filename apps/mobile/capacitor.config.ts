import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'za.co.lunamaygroup.glow',
  appName: 'Glow',
  webDir: 'www',
  includePlugins: ['capacitor-token-vault', '@capacitor/push-notifications'],
  server: {
    hostname: 'localhost',
    androidScheme: 'https',
  },
};

export default config;
