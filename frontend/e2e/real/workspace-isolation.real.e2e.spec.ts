import { expect, test } from "@playwright/test";
import { openWorkspace, realUsers, signIn } from "./support/real-session";

test("workspace transition does not retain the previous workspace board projection", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  await openWorkspace(page, "Workspace 1");
  await expect(page.getByText(/^Product Roadmap \(/).first()).toBeVisible();

  await page.getByRole("button", { name: /Workspace 1/ }).click();
  await page.getByRole("menuitem", { name: /Workspace 2/ }).click();
  await expect(page).toHaveURL(/\/workspaces\/[^/]+$/);
  await expect(page.getByText(/^Bug Tracker \(/).first()).toBeVisible();
  await expect(page.getByText(/^Product Roadmap \(/)).toHaveCount(0);

  await page.getByRole("button", { name: /Workspace 2/ }).click();
  await page.getByRole("menuitem", { name: /Workspace 1/ }).click();
  await expect(page.getByText(/^Product Roadmap \(/).first()).toBeVisible();
  await expect(page.getByText(/^Bug Tracker \(/)).toHaveCount(0);
});
