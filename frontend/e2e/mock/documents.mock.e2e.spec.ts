import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";

test("renders a production Documents route through mock transport", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));

  if (scenario !== "default") {
    // Documents page requires default world data (mock-doc-product-spec).
    // For other scenarios, just verify home loads without mock errors.
    await page.goto("/home");
    expect(
      errors.filter((message) =>
        message.includes("MockUnhandledOperationError"),
      ),
    ).toEqual([]);
    return;
  }

  await page.goto(
    "/workspaces/mock-workspace-primary/docs/mock-doc-product-spec",
  );
  await expect(page.getByText("Product specification").first()).toBeVisible();
  await expect(
    page.getByRole("textbox", { name: "Type '/' for commands..." }).first(),
  ).toHaveValue("Block 1");
  expect(
    errors.filter((message) => message.includes("MockUnhandledOperationError")),
  ).toEqual([]);
});
