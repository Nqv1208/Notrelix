import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";

test("renders a production Work Management route through mock transport", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));

  if (scenario !== "default") {
    // Work management board requires default world data.
    // For other scenarios, just verify home loads without mock errors.
    await page.goto("/home");
    expect(
      errors.filter((message) => message.includes("MockUnhandledOperationError")),
    ).toEqual([]);
    return;
  }

  await page.goto(
    "/workspaces/mock-workspace-primary/boards/mock-board-roadmap",
  );
  await expect(page.getByText("Product Roadmap").first()).toBeVisible();
  await expect(page.getByText("Ship mock runtime").first()).toBeVisible();
  expect(
    errors.filter((message) => message.includes("MockUnhandledOperationError")),
  ).toEqual([]);
});
