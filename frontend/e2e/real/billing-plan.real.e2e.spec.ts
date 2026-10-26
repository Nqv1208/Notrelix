import { expect, test } from "@playwright/test";
import {
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

test("the account owner switches the plan and the limits follow, then returns to Pro", async ({
  page,
}) => {
  // The account owner owns Workspace 1, so they may also view its billing.
  await signIn(page, realUsers.admin);
  const workspace = await findWorkspace(page, "Workspace 1");
  await page.goto(`/workspaces/${workspace.id}/billing`);

  const limits = page.getByRole("list", {
    name: "Capabilities granted by billing",
  });
  await expect(limits.getByText(/^\d+ of 500 used$/)).toBeVisible();

  await page.getByRole("button", { name: "Switch to Free" }).click();
  await expect(limits.getByText(/^\d+ of 10 used$/)).toBeVisible();
  await expect(limits.getByText(/^\d+(\.\d+)? of 2 GB used$/)).toBeVisible();

  await page.getByRole("button", { name: "Switch to Enterprise" }).click();
  await expect(limits.getByText("Unlimited").first()).toBeVisible();

  await page.getByRole("button", { name: "Switch to Pro" }).click();
  await expect(limits.getByText(/^\d+ of 500 used$/)).toBeVisible();
});

test("a workspace admin who is not the account owner sees the plan but no switcher", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  await page.goto(`/workspaces/${workspace.id}/billing`);

  await expect(page.getByText("Current plan")).toBeVisible();
  await expect(page.getByRole("button", { name: /Switch to/ })).toHaveCount(0);
});
