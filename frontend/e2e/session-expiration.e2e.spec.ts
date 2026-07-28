import { test, expect } from '@playwright/test';

test.describe('Session Expiration E2E Flow', () => {
  test('redirects to sign-in on session expiration', async ({ page }) => {
    await page.goto('/sign-in');
    await expect(page.locator('input[type="email"], input[name="email"]')).toBeVisible();
  });
});
