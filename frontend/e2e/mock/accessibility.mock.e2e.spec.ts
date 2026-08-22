import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

test("renders accessible interactive landmarks under mock mode", async ({
  page,
}) => {
  test.skip(
    (process.env.VITE_MOCK_STATE ?? "default") !== "default",
    "default scenario only",
  );
  await page.goto("/workspaces/mock-workspace-primary");
  const results = await new AxeBuilder({ page })
    .disableRules(["color-contrast", "scrollable-region-focusable"])
    .analyze();
  expect(
    results.violations.filter((violation) =>
      ["serious", "critical"].includes(violation.impact ?? ""),
    ),
  ).toEqual([]);
});
