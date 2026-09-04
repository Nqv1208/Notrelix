import { expect, test } from "@playwright/test";
import { storybookIframeUrl, uiEvidenceTargets } from "./support/ui-evidence";

const VIEWPORTS = [{ name: "desktop", width: 1440, height: 900 }] as const;

for (const target of uiEvidenceTargets("visual")) {
  for (const viewport of VIEWPORTS) {
    test(`visual manifest: ${target.surfaceId} ${target.state} ${viewport.name}`, async ({
      page,
    }) => {
      await page.setViewportSize({
        width: viewport.width,
        height: viewport.height,
      });
      await page.goto(storybookIframeUrl(target.storyId));
      await page.waitForLoadState("networkidle");

      await expect(page.locator("#storybook-root")).toHaveScreenshot(
        `${target.storyId}-${viewport.name}.png`,
        { animations: "disabled" },
      );
    });
  }
}
