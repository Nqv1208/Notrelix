import { test, expect } from "@playwright/test";

/**
 * Marketing visual regression baselines.
 *
 * Snapshots for the marketing homepage and contact page at desktop and
 * mobile viewports. Baselines are checked in; regenerate intentionally
 * with: npx playwright test --config playwright.marketing.config.ts
 * --update-snapshots
 */

const FULL_PAGE = { fullPage: true } as const;

test.describe("Marketing Visual", () => {
  test("homepage desktop snapshot", async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 900 });
    await page.goto("/");
    await expect(page.locator("#hero")).toBeVisible();
    await expect(page).toHaveScreenshot("homepage-desktop.png", FULL_PAGE);
  });

  test("homepage mobile snapshot", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/");
    await expect(page.locator("#hero")).toBeVisible();
    await expect(page).toHaveScreenshot("homepage-mobile.png", FULL_PAGE);
  });

  test("contact page desktop snapshot", async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 900 });
    await page.goto("/contact");
    await expect(
      page.getByRole("heading", { level: 1, name: /Contact Notrelix/ }),
    ).toBeVisible();
    await expect(page).toHaveScreenshot("contact-desktop.png", FULL_PAGE);
  });
});
