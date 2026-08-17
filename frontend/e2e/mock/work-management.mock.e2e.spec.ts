import { expect, test } from "@playwright/test";

test("renders a production Work Management route through mock transport", async ({
  page,
}) => {
  test.skip(
    (process.env.VITE_MOCK_STATE ?? "default") !== "default",
    "default scenario only",
  );
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await page.goto(
    "/workspaces/mock-workspace-primary/boards/mock-board-roadmap",
  );
  await expect(page.getByText("Product Roadmap").first()).toBeVisible();
  await expect(page.getByText("Ship mock runtime").first()).toBeVisible();
  expect(
    errors.filter((message) => message.includes("MockUnhandledOperationError")),
  ).toEqual([]);
});
