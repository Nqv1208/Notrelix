import { test, expect } from "@playwright/test";

/**
 * UI freeze gate: Foundation Gallery visual smoke baselines.
 *
 * Screenshots are taken in the isolated story iframe (no Storybook chrome),
 * with animations disabled by the preview stylesheet and deterministic
 * static data. Committed baselines change only through an intentional
 * UI change PR.
 */

const VIEWPORTS = [
  { name: "mobile", width: 375, height: 812 },
  { name: "tablet", width: 768, height: 1024 },
  { name: "desktop", width: 1440, height: 900 },
] as const;

for (const viewport of VIEWPORTS) {
  test(`visual: foundation gallery matches baseline at ${viewport.name}`, async ({
    page,
  }) => {
    await page.setViewportSize({
      width: viewport.width,
      height: viewport.height,
    });
    await page.goto("/iframe.html?id=foundation-gallery--primitives");
    await page.waitForLoadState("networkidle");

    await expect(page).toHaveScreenshot(
      `foundation-gallery-${viewport.name}.png`,
      {
        animations: "disabled",
        fullPage: true,
      },
    );
  });
}
