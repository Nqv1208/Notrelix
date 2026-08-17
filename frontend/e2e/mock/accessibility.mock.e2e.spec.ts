import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

test("has no serious accessibility violations on the mock workspace home", async ({ page }) => {
  test.skip((process.env.VITE_MOCK_SCENARIO ?? "default") !== "default", "default scenario only");
  await page.goto("/workspaces/mock-workspace-primary");
  const results = await new AxeBuilder({ page }).analyze();
  expect(results.violations.filter((violation) => ["serious", "critical"].includes(violation.impact ?? ""))).toEqual([]);
});
