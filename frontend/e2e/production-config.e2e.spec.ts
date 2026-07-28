import { test, expect } from '@playwright/test';

test.describe('Production Config E2E Smoke Flow', () => {
  test('app loads index cleanly without console errors', async ({ page }) => {
    const consoleErrors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    await page.goto('/');
    expect(consoleErrors.filter((e) => !e.includes('favicon'))).toHaveLength(0);
  });
});
