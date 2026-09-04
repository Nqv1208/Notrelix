import { expect, test } from "@playwright/test";
import { storybookIframeUrl, uiEvidenceTargets } from "./support/ui-evidence";

for (const target of uiEvidenceTargets("purity")) {
  test(`network manifest: ${target.surfaceId} ${target.state}`, async ({
    page,
  }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));
    page.on("console", (message) => {
      const text = message.text();
      if (text.includes("PureUiNetworkAccessError")) errors.push(text);
    });

    await page.goto(storybookIframeUrl(target.storyId));
    await expect(page.locator("#storybook-root")).toBeVisible();
    await page.waitForTimeout(100);

    expect(errors).toEqual([]);
  });
}
