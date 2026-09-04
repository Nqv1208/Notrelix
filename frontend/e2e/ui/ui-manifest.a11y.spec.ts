import { expect, test } from "@playwright/test";
import { analyzeA11y } from "./support/a11y";
import { storybookIframeUrl, uiEvidenceTargets } from "./support/ui-evidence";

test.describe.configure({ mode: "serial" });

for (const target of uiEvidenceTargets("a11y")) {
  test(`a11y manifest: ${target.surfaceId} ${target.state}`, async ({
    page,
  }) => {
    await page.goto(storybookIframeUrl(target.storyId));
    await expect(page.locator("#storybook-root")).toBeVisible();

    const results = await analyzeA11y(page);
    const blocking = results.violations.filter(
      (violation) =>
        violation.impact === "critical" || violation.impact === "serious",
    );

    expect(
      blocking.map((violation) => ({
        id: violation.id,
        impact: violation.impact,
        nodes: violation.nodes.map((node) => node.target.join(" ")),
      })),
      `critical/serious axe violations in ${target.storyId}`,
    ).toEqual([]);
  });
}
