import { expect, test } from '@playwright/test';

test('should display welcome message without browser errors', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('console', message => {
    if (message.type() === 'error') {
      errors.push(message.text());
    }
  });

  await page.goto('/');
  await expect(page.locator('app-root h1')).toHaveText('Welcome to hello-world!');
  expect(errors).toEqual([]);
});
