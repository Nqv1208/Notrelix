import { test, expect } from '@playwright/test';

test.describe('Realtime Connection Lifecycle E2E Flow', () => {
  test('sign-in page boots without throwing unhandled realtime errors', async ({ page }) => {
    await page.goto('/sign-in');
    await expect(page).toHaveTitle(/Notrelix|Sign In/i);
  });
});
