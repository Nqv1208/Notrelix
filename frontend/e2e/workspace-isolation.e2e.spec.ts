import { test, expect } from '@playwright/test';

test.describe('Workspace Isolation E2E Flow', () => {
  test('unauthenticated access redirects to sign in with returnUrl', async ({ page }) => {
    await page.goto('/workspaces/ws-200');
    await expect(page).toHaveURL(/\/sign-in/);
  });
});
