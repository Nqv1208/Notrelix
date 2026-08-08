import { test, expect } from '@playwright/test';

test.describe('Production Config E2E Smoke Flow', () => {
  test('app loads index cleanly without console errors', async ({ page }) => {
    const appErrors: string[] = [];
    page.on('pageerror', (error) => {
      appErrors.push(`pageerror: ${error.message}`);
    });
    page.on('console', (msg) => {
      if (msg.type() !== 'error') return;
      const text = msg.text();
      if (text.includes('favicon')) return;
      if (text.includes('Failed to load resource')) return;
      appErrors.push(text);
    });

    await page.goto('/');
    await expect(page).toHaveTitle(/Notrelix/i);
    expect(appErrors).toHaveLength(0);
  });
});
