import { expect, test } from "@playwright/test";
import { requireViewportSize } from "../../tooling/testing/src/ui-visual-registry";
import {
  storybookIframeUrl,
  uiEvidenceVisualTargets,
} from "./support/ui-evidence";

for (const target of uiEvidenceVisualTargets()) {
  const size = requireViewportSize(target.viewport);

  test(`visual manifest: ${target.storyId} ${target.viewport}/${target.theme}`, async ({
    page,
  }) => {
    await page.setViewportSize({ width: size.width, height: size.height });
    await page.goto(
      `${storybookIframeUrl(target.storyId)}&fui-theme=${target.theme}`,
    );
    await page.waitForLoadState("networkidle");

    const rootClass = await page.evaluate(
      () => document.documentElement.className,
    );
    expect(rootClass, `theme class for ${target.theme}`).toContain(
      target.theme,
    );

    await expect(page.locator("#storybook-root")).toHaveScreenshot(
      `${target.storyId}--${target.viewport}--${target.theme}.png`,
      { animations: "disabled" },
    );
  });
}
