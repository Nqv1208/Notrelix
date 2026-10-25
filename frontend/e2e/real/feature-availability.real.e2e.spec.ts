import { expect, test, type Page } from "@playwright/test";
import { openBoard, realUsers, signIn } from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

/** Console errors and page errors raised while a page is open. */
function watchErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on("console", (message) => {
    if (message.type() === "error") errors.push(message.text());
  });
  page.on("pageerror", (error) => errors.push(error.message));
  return errors;
}

test("billing shows what the workspace's billing grants, with the seeded limit and use, and says the rest is not available yet", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const errors = watchErrors(page);

  await page.goto(`/workspaces/${workspace.id}/billing`);

  await expect(
    page.getByRole("heading", { level: 2, name: "Plans and billing" }),
  ).toBeVisible();
  const row = page.getByRole("list", {
    name: "Capabilities granted by billing",
  });
  await expect(row.getByText("Automation rules")).toBeVisible();
  await expect(row.getByText(/^\d+ of 50 used$/)).toBeVisible();
  await expect(
    row.getByRole("meter", { name: "Automation rules" }),
  ).toBeVisible();
  await expect(
    page.getByText(/invoices and payment methods are not available yet/),
  ).toBeVisible();
  expect(errors).toEqual([]);
});

test("integrations and reports say they are not available yet instead of an error", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const errors = watchErrors(page);

  await page.goto(`/workspaces/${workspace.id}/integrations`);
  await expect(
    page.getByRole("heading", { name: "Integrations are not available yet" }),
  ).toBeVisible();

  await page.goto(`/workspaces/${workspace.id}/reports`);
  await expect(
    page.getByRole("heading", { name: "Reports are not available yet" }),
  ).toBeVisible();

  expect(errors).toEqual([]);
});
