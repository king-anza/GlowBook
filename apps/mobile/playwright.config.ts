import { defineConfig, devices } from '@playwright/test';

const port = process.env['PLAYWRIGHT_PORT'] ?? '4200';
const baseURL = `http://localhost:${port}`;

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 7'] },
    },
    {
      name: 'Tablet Chrome',
      grep: /@visual/,
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 834, height: 1194 },
        hasTouch: true,
        isMobile: true,
      },
    },
    {
      name: 'Desktop Chrome',
      grep: /@visual/,
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 1000 } },
    },
  ],
  webServer: {
    command: `npm start -- --port ${port}`,
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
