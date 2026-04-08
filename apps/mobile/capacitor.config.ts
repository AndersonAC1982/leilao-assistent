import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'br.com.leilaoauto.mobile',
  appName: 'LEILAOAUTO',
  webDir: 'dist/leilaoauto-mobile/browser',
  server: {
    androidScheme: 'https'
  }
};

export default config;
