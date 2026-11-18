import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e/src',
  testMatch: '**/*.e2e-spec.ts',
  use: {
    baseURL: 'http://127.0.0.1:4200',
    browserName: 'chromium'
  },
  webServer: {
    command: 'npm start -- --host 127.0.0.1 --port 4200',
    url: 'http://127.0.0.1:4200',
    reuseExistingServer: false,
    timeout: 120000
  }
});
