import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

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
  // A write that carries no CSRF token is refused for that before the item is even looked at.
  const unprotected = await page.request.fetch(
    `${apiUrl}/board-items/${parentId}`,
    {
      method: "PATCH",
      headers: {
        "Idempotency-Key": crypto.randomUUID(),
        "Content-Type": "application/json",
      },
      data: JSON.stringify({
        descriptionMd: "Edited while archived",
        expectedVersion: archivedItem.version,
      }),
    },
  );
  expect(unprotected.status()).toBe(403);
  expect(await unprotected.text()).toContain("csrf-validation-failed");

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

test("the archive is read a page at a time and searched, a deleted item comes back with its sub-item, and a lone sub-item is refused while its parent is deleted", async ({
  page,
}) => {
  test.setTimeout(240_000);
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

  // 52 items archived by one bulk write: two more than a page.
  const ids: string[] = [];
  for (let n = 0; n < 52; n += 1) {
    const created = await apiWrite<{ id: string }>(
      page,
      "POST",
      `/boards/${board.id}/items`,
      { groupId, title: `Paged ${unique} ${n}` },
    );
    expect(created.status).toBeLessThan(300);
    ids.push(created.data!.id);
  }
  const expectedVersions: Record<string, number> = {};
  for (const id of ids)
    expectedVersions[id] = (
      await apiRead<ItemRead>(page, `/board-items/${id}`)
    ).version;
  expect(
    (
      await apiWrite(page, "POST", `/boards/${board.id}/items/bulk`, {
        operation: "archive",
        itemIds: ids,
        expectedVersions,
      })
    ).status,
  ).toBeLessThan(300);

  // The first page is 50 rows, says so, and the next page comes on request.
  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}`);
  await page.getByRole("button", { name: "More options" }).click();
  await page.getByRole("menuitem", { name: "Archived items" }).click();
  const sheet = page.getByRole("dialog", { name: /^Archived items/ });
  await sheet.getByRole("searchbox").fill(`Paged ${unique}`);
  const rows = sheet.getByRole("listitem");
  await expect(rows).toHaveCount(50);
  await expect(sheet.getByText("Showing 50 of 52")).toBeVisible();
  await expect(rows.first()).toContainText("Archived by");
  await sheet.getByRole("button", { name: "Show 2 more" }).click();
  await expect(rows).toHaveCount(52);
  await expect(
    sheet.getByRole("button", { name: /Show \d+ more/ }),
  ).toHaveCount(0);

  // A name search narrows the list; one row comes back to the board.
  await sheet.getByRole("searchbox").fill(`Paged ${unique} 7`);
  await expect(rows).toHaveCount(1);
  const back = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().includes(`/board-items/${ids[7]}/unarchive`),
  );
  await rows.first().getByRole("button", { name: "Unarchive" }).click();
  expect((await back).status()).toBeLessThan(300);
  await expect(rows).toHaveCount(0);
  expect(
    (await apiRead<ItemRead>(page, `/board-items/${ids[7]}`)).isArchived,
  ).not.toBe(true);
  await page.keyboard.press("Escape");

  // A parent with a sub-item is deleted from its card; the Deleted items list counts the sub-item.
  const parentId = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId,
      title: `Delete me ${unique}`,
    })
  ).data!.id;
  const childId = (
    await apiWrite<{ id: string }>(
      page,
      "POST",
      `/board-items/${parentId}/child-items`,
      { title: `Kid ${unique}` },
    )
  ).data!.id;
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${parentId}`,
  );
  await page.getByRole("button", { name: "Card actions" }).click();
  await page.getByRole("menuitem", { name: "Delete…" }).click();
  const deleted = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      response.url().includes(`/board-items/${parentId}`),
  );
  await page
    .getByRole("dialog", { name: /^Delete “/ })
    .getByRole("button", { name: "Delete item" })
    .click();
  expect((await deleted).status()).toBeLessThan(300);

  // A sub-item alone is refused while its parent is in the Trash, and nothing changes.
  const refused = await apiWrite<{ errorCode?: string }>(
    page,
    "POST",
    `/board-items/${childId}/restore`,
    {},
  );
  expect(refused.status).toBe(409);
  expect(refused.data?.errorCode).toBe(
    "work-management.item.restore-parent-deleted",
  );

  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}`);
  await page.getByRole("button", { name: "More options" }).click();
  await page.getByRole("menuitem", { name: "Deleted items" }).click();
  const trash = page.getByRole("dialog", { name: /^Deleted items/ });
  const row = trash.getByRole("listitem").filter({
    hasText: `Delete me ${unique}`,
  });
  // The 52 archive events are still being consumed, so the first read can take a while.
  await expect(row).toContainText("1 sub-item", { timeout: 30_000 });
  await expect(row).toContainText("Deleted by");
  await expect(row).toContainText("left");

  // The parent comes back with its sub-item.
  const restored = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().includes(`/board-items/${parentId}/restore`),
  );
  await row.getByRole("button", { name: "Restore" }).click();
  expect((await restored).status()).toBeLessThan(300);
  await expect(row).toHaveCount(0);
  const kids = await apiRead<{ items: { id: string }[] }>(
    page,
    `/board-items/${parentId}/child-items`,
  );
  expect(kids.items.map((kid) => kid.id)).toContain(childId);
});

test("a member duplicates an item with its sub-item from the dialog, the copy sits right after the original and the original is unchanged, and an archived item is refused", async ({
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
  const sourceId = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId,
      title: `Copy me ${unique}`,
    })
  ).data!.id;
  const kidId = (
    await apiWrite<{ id: string }>(
      page,
      "POST",
      `/board-items/${sourceId}/child-items`,
      { title: `Kid ${unique}` },
    )
  ).data!.id;

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${sourceId}`,
  );
  await page.getByRole("button", { name: "Card actions" }).click();
  await page.getByRole("menuitem", { name: /^Duplicate/ }).click();
  const dialog = page.getByRole("dialog", { name: "Duplicate item" });
  // The name is selected, the parts are ticked and the sub-item count is on its row.
  await expect(dialog.getByRole("textbox")).toBeFocused();
  await expect(dialog.getByText("1 sub-items")).toBeVisible();
  await dialog.getByRole("textbox").fill(`Copy of ${unique}`);
  const created = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/board-items/${sourceId}/duplicate`),
  );
  await dialog.getByRole("button", { name: "Duplicate", exact: true }).click();
  const response = await created;
  expect(response.status()).toBeLessThan(300);
  await expect(dialog).toBeHidden();

  // The copy has its own key and its sub-item; the original is as it was.
  const copyId = ((await response.json()) as { id?: string; data?: string }).id;
  const board2 = await apiRead<{
    lists: { boardItems: { id: string; name: string }[] }[];
  }>(page, `/boards/${board.id}/full`);
  const cards = board2.lists.flatMap((list) => list.boardItems);
  const copy = cards.find((card) => card.name === `Copy of ${unique}`);
  expect(copy).toBeTruthy();
  if (copyId) expect(copy!.id).toBe(copyId);
  expect(copy!.id).not.toBe(sourceId);
  const copyKids = await apiRead<{ items: { title: string }[] }>(
    page,
    `/board-items/${copy!.id}/child-items`,
  );
  expect(copyKids.items.map((kid) => kid.title)).toEqual([`Kid ${unique}`]);
  const originalKids = await apiRead<{ items: { id: string }[] }>(
    page,
    `/board-items/${sourceId}/child-items`,
  );
  expect(originalKids.items.map((kid) => kid.id)).toEqual([kidId]);
  expect(cards.find((card) => card.id === sourceId)?.name).toBe(
    `Copy me ${unique}`,
  );

  // An archived item is refused and nothing is copied.
  const archived = await apiRead<ItemRead>(page, `/board-items/${sourceId}`);
  const archive = await apiWrite(
    page,
    "POST",
    `/board-items/${sourceId}/archive`,
    { expectedVersion: archived.version },
  );
  expect(archive.status).toBeLessThan(300);
  const refused = await apiWrite(
    page,
    "POST",
    `/board-items/${sourceId}/duplicate`,
    { name: `Never ${unique}` },
  );
  expect(refused.status).toBe(400);
  const after = await apiRead<{
    lists: { boardItems: { name: string }[] }[];
  }>(page, `/boards/${board.id}/full`);
  expect(
    after.lists
      .flatMap((list) => list.boardItems)
      .some((card) => card.name === `Never ${unique}`),
  ).toBe(false);
});
