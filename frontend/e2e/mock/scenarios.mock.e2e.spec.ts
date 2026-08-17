import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";

test("renders the configured deterministic scenario", async ({ page }) => {
  test.skip(
    !["new-user", "empty", "error", "large", "permissions"].includes(scenario),
    "non-default scenario only",
  );
  await page.goto("/home");
  if (scenario === "new-user") {
    await expect(
      page.getByText("No workspaces are available for this account."),
    ).toBeVisible();
  } else if (scenario === "error") {
    await expect(page.getByText("Unable to load workspaces")).toBeVisible();
  } else {
    await expect(page.getByText("Notrelix Product Lab")).toBeVisible();
  }
});
