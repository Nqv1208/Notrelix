import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

/**
 * Marketing accessibility gate.
 *
 * Gate: 0 critical + 0 serious axe violations across every public
 * marketing page. No blanket rule disable is allowed.
 */

const PUBLIC_PAGES = ["/", "/contact", "/legal/privacy", "/legal/terms"];

for (const path of PUBLIC_PAGES) {
  test(`a11y: ${path} has no critical or serious violations`, async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.goto(path);
    await page.waitForLoadState("networkidle");
    await page.evaluate(() => document.fonts.ready);

    const results = await new AxeBuilder({ page }).analyze();

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
      `critical/serious axe violations on ${path}`,
    ).toEqual([]);
  });
}
