import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

test("a member archives an item from its card, sees it archived and restores it", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );

  // Open an item's card to learn its id.
  await page
    .getByRole("button", { name: /Task \d+/ })
    .first()
    .click();
  await expect(page).toHaveURL(/[?&]item=/);
  const itemId = new URL(page.url()).searchParams.get("item")!;
  const card = `/workspaces/${workspace.id}/boards/${board.id}?item=${itemId}`;

  await page.getByRole("button", { name: "Card actions" }).click();
  const archived = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/board-items/${itemId}/archive`) &&
      response.status() !== 403,
  );
  await page.getByRole("menuitem", { name: /^Archive/ }).click();
  // Archiving asks first; nothing is sent until the person confirms.
  const confirm = page.getByRole("dialog", { name: "Archive this item?" });
  await expect(confirm).toBeVisible();
  await confirm.getByRole("button", { name: "Archive item" }).click();
  const response = await archived;
  expect(response.status()).toBeLessThan(300);
  expect(response.request().postDataJSON()).toEqual({
    expectedVersion: expect.any(Number),
  });

  // An archived item still opens from its address and offers Unarchive.
  await page.goto(card);
  await expect(
    page.getByRole("dialog").getByText("User 4 archived this card"),
  ).toBeVisible();
  await page.getByRole("button", { name: "Card actions" }).click();
  const restored = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().includes(`/board-items/${itemId}/unarchive`) &&
      response.status() !== 403,
  );
  await page.getByRole("menuitem", { name: "Unarchive", exact: true }).click();
  expect((await restored).status()).toBeLessThan(300);
  expect(
    (await apiRead<{ isArchived?: boolean }>(page, `/board-items/${itemId}`))
      .isArchived,
  ).not.toBe(true);
});

interface ItemRead {
  readonly version: number;
  readonly isArchived?: boolean;
}

test("archiving an item with two sub-items asks first, takes all three off the board, and the archived item refuses edits", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const parentId = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId,
      title: `Archive me ${unique}`,
    })
  ).data!.id;
  for (const title of ["Alpha", "Beta"]) {
    expect(
      (
        await apiWrite(page, "POST", `/board-items/${parentId}/child-items`, {
          title: `${title} ${unique}`,
        })
      ).status,
    ).toBeLessThan(300);
  }

  // Before: the row is on the board and shows its two sub-items.
  const table = `/workspaces/${workspace.id}/boards/${board.id}?view=table`;
  await page.goto(table);
  const row = page.locator(`[data-row-id="${parentId}"]`);
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: /^Show 2 sub-items/ }).click();
  await expect(page.getByText(`Alpha ${unique}`)).toBeVisible();

  // The dialog names the sub-items and counts everything it archives.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${parentId}`,
  );
  await page.getByRole("button", { name: "Card actions" }).click();
  await page.getByRole("menuitem", { name: /^Archive/ }).click();
  const confirm = page.getByRole("dialog", { name: "Archive this item?" });
  await expect(confirm.getByText("2 sub-items")).toBeVisible();
  const archived = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/board-items/${parentId}/archive`),
  );
  await confirm.getByRole("button", { name: "Archive 3 items" }).click();
  expect((await archived).status()).toBeLessThan(300);

  // After: all three have left the board and the sub-item list.
  await page.goto(table);
  await expect(page.locator(`[data-row-id="${parentId}"]`)).toHaveCount(0);
  await expect(page.getByText(`Alpha ${unique}`)).toHaveCount(0);
  await expect(page.getByText(`Beta ${unique}`)).toHaveCount(0);
  const children = await apiRead<{ items: unknown[] }>(
    page,
    `/board-items/${parentId}/child-items`,
  );
  expect(children.items).toEqual([]);

  // An archived item refuses an edit and says why.
  const archivedItem = await apiRead<ItemRead>(
    page,
    `/board-items/${parentId}`,
  );
  expect(archivedItem.isArchived).toBe(true);
  const edit = await apiWrite<unknown>(
    page,
    "PATCH",
    `/board-items/${parentId}`,
    {
      descriptionMd: "Edited while archived",
      expectedVersion: archivedItem.version,
    },
  );
  expect(edit.status).toBeGreaterThanOrEqual(400);
  expect(edit.status).toBeLessThan(500);
  expect(JSON.stringify(edit.data)).toMatch(/archived/i);
});

test("an archive that carries a stale version is refused and changes nothing", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const unique = Date.now().toString(36);
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const itemId = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId,
      title: `Stale archive ${unique}`,
    })
  ).data!.id;
  const seen = await apiRead<ItemRead>(page, `/board-items/${itemId}`);

  // Someone else edits the item after the person read it.
  expect(
    (
      await apiWrite(page, "PATCH", `/board-items/${itemId}`, {
        title: `Stale archive ${unique} edited`,
        expectedVersion: seen.version,
      })
    ).status,
  ).toBeLessThan(300);

  const stale = await apiWrite(page, "POST", `/board-items/${itemId}/archive`, {
    expectedVersion: seen.version,
  });
  expect(stale.status).toBe(412);
  const after = await apiRead<ItemRead>(page, `/board-items/${itemId}`);
  expect(after.isArchived).not.toBe(true);

  // No version at all is a client error, not a silent archive.
  const missing = await apiWrite(
    page,
    "POST",
    `/board-items/${itemId}/archive`,
    {},
  );
  expect(missing.status).toBe(400);
});
