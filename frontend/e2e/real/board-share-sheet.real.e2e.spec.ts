import { expect, test } from "@playwright/test";
import { apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace, joinAs } from "./support/real-workspace";

test.use({ viewport: { width: 390, height: 844 } });
test.setTimeout(240_000);

/**
 * The share sheet at 390 px on the real stack, in a workspace made for the journey: the manager picks a
 * member in the search and the permission is given at once (no Invite button), the toast offers Undo and the
 * undo takes the permission back; a member who does not manage the board gets an add field that stays shut.
 */
test("a manager adds a member by picking them in the sheet and takes the add back with Undo; a member cannot add", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Share sheet");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const made = await apiWrite<string | { id: string }>(
    page,
    "POST",
    `/workspaces/${workspace.id}/boards`,
    { title: "Sheet board", visibility: "Workspace" },
  );
  expect(made.status).toBe(201);
  const boardId = typeof made.data === "string" ? made.data : made.data!.id;
  const board = `/workspaces/${workspace.id}/boards/${boardId}`;

  try {
    await page.goto(board);
    await page
      .getByRole("button", { name: /^Share/ })
      .first()
      .click();
    const sheet = page.locator('[data-f="dlg"]');
    await expect(sheet).toBeVisible();
    await expect(sheet.getByRole("button", { name: "Invite" })).toHaveCount(0);

    // Pick the member: the permission is given at once, with the level of the row (Can edit).
    const granted = page.waitForResponse(
      (response) =>
        response.request().method() === "POST" &&
        /\/permissions$/.test(response.url()),
    );
    await sheet.getByRole("combobox").fill("User 4");
    await sheet.getByRole("option", { name: /User 4/ }).click();
    expect((await granted).status()).toBeLessThan(300);
    const row = sheet.locator('[data-f="pr"]').filter({ hasText: "User 4" });
    await expect(row).toBeVisible();
    const toast = page.locator('[data-f="toast"]');
    await expect(toast).toContainText("Added User 4");

    // Undo takes the permission back; the row goes.
    const revoked = page.waitForResponse(
      (response) =>
        response.request().method() === "DELETE" &&
        /\/permissions\//.test(response.url()),
    );
    await toast.getByRole("button", { name: "Undo" }).click();
    expect((await revoked).status()).toBeLessThan(300);
    await expect(row).toHaveCount(0);

    // A member who does not manage the board: the add field stays shut.
    await member.page.setViewportSize({ width: 390, height: 844 });
    await member.page.goto(board);
    await member.page
      .getByRole("button", { name: /^Share/ })
      .first()
      .click();
    await expect(member.page.locator('[data-f="dlg"]')).toBeVisible();
    await expect(
      member.page.locator('[data-f="dlg"]').getByRole("combobox"),
    ).toHaveAttribute("aria-disabled", "true");
  } finally {
    await member.context.close();
  }
});
