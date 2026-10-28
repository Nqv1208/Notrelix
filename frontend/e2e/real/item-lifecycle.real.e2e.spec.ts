import { expect, test } from "@playwright/test";
import { openBoard, realUsers, signIn } from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

test("a member archives an item from its page, sees it archived and restores it", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );

  // Open an item in the side sheet to learn its id, then its own page.
  await page
    .getByRole("button", { name: /Task \d+/ })
    .first()
    .click();
  await expect(page).toHaveURL(/[?&]item=/);
  const itemId = new URL(page.url()).searchParams.get("item")!;
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`,
  );

  await page.getByRole("button", { name: "More", exact: true }).click();
  const archived = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/board-items/${itemId}/archive`),
  );
  await page.getByRole("menuitem", { name: "Archive" }).click();
  expect((await archived).status()).toBeLessThan(300);

  // The page closes on archive; an archived item still opens from its address.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`,
  );
  const banner = page.locator('[data-item-banner="archived"]');
  await expect(banner).toBeVisible();

  const restored = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/board-items/${itemId}/unarchive`),
  );
  await banner.getByRole("button").click();
  expect((await restored).status()).toBeLessThan(300);
  await expect(banner).toHaveCount(0);
});
