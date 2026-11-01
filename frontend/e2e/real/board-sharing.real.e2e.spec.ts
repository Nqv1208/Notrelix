import { expect, test } from "@playwright/test";
import { openBoard, realUsers, signIn } from "./support/real-session";

// The people list scrolls below 820 px of height; the permission menu must still open whole, outside it.
const VIEWPORT_HEIGHT = 700;
test.use({ viewport: { width: 1440, height: VIEWPORT_HEIGHT } });

test("a board manager adds a workspace guest, sees the Viewer cap, and removes the access", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  await openBoard(page, "Workspace 3", "Engineering Tasks");

  // The board header opens the sharing dialog from its Share button.
  await page.getByRole("button", { name: "Share", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  const row = dialog.locator('[data-f="pr"]').filter({ hasText: "User 6" });
  // The list shows five rows; the new person may be past them.
  const more = dialog.getByRole("button", { name: /^Show \d+ more$/ });
  const showRow = async () => {
    await expect(more.or(row)).toBeVisible();
    if (await more.count()) await more.click();
    await expect(row).toBeVisible();
  };

  try {
    const add = dialog.getByRole("combobox");
    await expect(add).toBeEnabled();
    await add.fill("User 6");
    const granted = page.waitForResponse(
      (response) =>
        response.request().method() === "POST" &&
        /\/permissions$/.test(response.url()),
    );
    await dialog.getByRole("option", { name: /User 6/ }).click();
    // The invite asks for Can edit; a Guest is capped at Viewer (GOV-030), so the row says Can view.
    await dialog.getByRole("button", { name: "Invite", exact: true }).click();
    expect((await granted).status()).toBeLessThan(300);
    await showRow();
    await expect(row.getByRole("button", { name: /Can view/ })).toBeVisible();
    await expect(row.getByRole("button", { name: /Can edit/ })).toHaveCount(0);

    // The permission menu opens whole, outside the scrolling list.
    await row.getByRole("button", { name: /Can view/ }).click();
    const menu = page.getByRole("menu");
    await expect(menu).toBeVisible();
    const box = (await menu.boundingBox())!;
    expect(box.y).toBeGreaterThanOrEqual(0);
    expect(box.y + box.height).toBeLessThanOrEqual(VIEWPORT_HEIGHT);

    // Remove the access.
    const revoked = page.waitForResponse(
      (response) =>
        response.request().method() === "DELETE" &&
        /\/permissions\//.test(response.url()),
    );
    await page.getByRole("menuitem", { name: /Remove access/ }).click();
    expect((await revoked).status()).toBeLessThan(300);
    await expect(row).toHaveCount(0);
  } finally {
    // Leave the board as found even when a step above failed: the grant would break the next spec.
    if (!(await dialog.isVisible())) {
      await page.getByRole("button", { name: "Share", exact: true }).click();
    }
    if ((await row.count()) > 0 || (await more.count()) > 0) {
      if (await more.count()) await more.click();
      if ((await row.count()) > 0) {
        await row.getByRole("button", { name: /^Can / }).click();
        await page.getByRole("menuitem", { name: /Remove access/ }).click();
        await expect(row).toHaveCount(0);
      }
    }
  }
});
