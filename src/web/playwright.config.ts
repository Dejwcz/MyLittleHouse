import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: '../tests/web/e2e',
  use: {
    baseURL: 'http://localhost:5173'
  },
  webServer: {
    command: 'npm run dev -- --host',
    port: 5173,
    reuseExistingServer: !process.env.CI
  }
});
