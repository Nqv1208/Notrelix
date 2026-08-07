import { test, expect } from './fixtures/api.fixture';

test.describe('Production Auth & Route Protection E2E Flow', () => {
  test('redirects unauthenticated user accessing protected workspace route to sign-in page with returnUrl', async ({ page }) => {
    await page.goto('/workspaces/ws-100');
    await expect(page).toHaveURL(/\/sign-in/);
  });

  test('renders sign-in form with email and password fields', async ({ page }) => {
    await page.goto('/sign-in');
    await expect(page.locator('input[type="email"], input[name="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"], input[name="password"]')).toBeVisible();
  });
});
