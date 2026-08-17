import { expect, test } from "@playwright/test";

test("renders a production Documents route through mock transport", async ({ page }) => {
  test.skip((process.env.VITE_MOCK_SCENARIO ?? "default") !== "default", "default scenario only");
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await page.goto("/workspaces/mock-workspace-primary/docs/mock-doc-product-spec");
  await expect(page.getByText("Product specification").first()).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Type '/' for commands..." }).first()).toHaveValue(
    "Notrelix mock runtime specification.",
  );
  expect(errors.filter((message) => message.includes("MockUnhandledOperationError"))).toEqual([]);
});
