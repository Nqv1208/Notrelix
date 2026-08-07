import { test, expect } from '@playwright/test';

test.describe('Error Boundary E2E Flow', () => {
  test('does not leak raw backend stack trace on sign in page', async ({ page }) => {
    await page.goto('/sign-in');
    const pageText = await page.textContent('body');
    expect(pageText).not.toContain('TypeError: Cannot read property');
    expect(pageText).not.toContain('at Module.evaluate');
  });
});
