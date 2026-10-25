import { expect, test } from "@playwright/test";
import { openBoard, realUsers, signIn } from "./support/real-session";

// The people list scrolls below 820 px of height; the permission menu must still open whole, outside it.
const VIEWPORT_HEIGHT = 700;
test.use({ viewport: { width: 1440, height: VIEWPORT_HEIGHT } });

test("a board manager adds a workspace member, changes the permission and removes the access", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  await openBoard(page, "Workspace 3", "Engineering Tasks");

  await page.getByRole("button", { name: "Whole workspace" }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();

  const add = dialog.getByRole("combobox");
  await expect(add).toBeEnabled();
  await add.fill("User 6");
  const granted = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/permissions$/.test(response.url()),
  );
  await dialog.getByRole("option", { name: /User 6/ }).click();
  await dialog.getByRole("button", { name: "Invite", exact: true }).click();
  expect((await granted).status()).toBeLessThan(300);

  // The list shows five rows; the new person may be past them.
  const more = dialog.getByRole("button", { name: /^Show \d+ more$/ });
  const row = dialog.locator('[data-f="pr"]').filter({ hasText: "User 6" });
  await expect(more.or(row)).toBeVisible();
  if (await more.count()) await more.click();
  await expect(row).toBeVisible();
  await expect(row.getByRole("button", { name: /Can edit/ })).toBeVisible();

  // Change the permission to Can view.
  await row.getByRole("button", { name: /Can edit/ }).click();
  const changed = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/permissions$/.test(response.url()),
  );
  const menu = page.getByRole("menu");
  await expect(menu).toBeVisible();
  const box = (await menu.boundingBox())!;
  expect(box.y).toBeGreaterThanOrEqual(0);
  expect(box.y + box.height).toBeLessThanOrEqual(VIEWPORT_HEIGHT);
  await page.getByRole("menuitemradio", { name: /Can view/ }).click();
  expect((await changed).status()).toBeLessThan(300);
  await expect(row.getByRole("button", { name: /Can view/ })).toBeVisible();

  // Remove the access.
  await row.getByRole("button", { name: /Can view/ }).click();
  const revoked = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      /\/permissions\//.test(response.url()),
  );
  await page.getByRole("menuitem", { name: /Remove access/ }).click();
  expect((await revoked).status()).toBeLessThan(300);
  await expect(
    dialog.locator('[data-f="pr"]').filter({ hasText: "User 6" }),
  ).toHaveCount(0);
});
