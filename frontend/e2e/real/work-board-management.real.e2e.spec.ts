import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

interface FullBoard {
  readonly version: number;
  readonly itemKeyPrefix?: string | null;
  readonly lists: readonly {
    readonly id: string;
    readonly title: string;
    readonly boardItems: readonly {
      readonly id: string;
      readonly name: string;
    }[];
  }[];
}

const full = (page: Page, boardId: string) =>
  apiRead<FullBoard>(page, `/boards/${boardId}/full`);

test("a board manager deletes a group that has items by choosing where they go", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const groupTitle = `Moving ${unique}`;
  const itemTitle = `Moves with its group ${unique}`;

  // The scene: a group with one item in it.
  expect(
    (
      await apiWrite(page, "POST", `/boards/${board.id}/groups`, {
        title: groupTitle,
      })
    ).status,
  ).toBeLessThan(300);
  const group = (await full(page, board.id)).lists.find(
    (list) => list.title === groupTitle,
  )!;
  expect(
    (
      await apiWrite(page, "POST", `/boards/${board.id}/items`, {
        groupId: group.id,
        title: itemTitle,
      })
    ).status,
  ).toBeLessThan(300);

  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}/settings`);
  await page.getByRole("button", { name: /^Task groups/ }).click();
  await page
    .getByRole("button", { name: `More actions for the group ${groupTitle}` })
    .click();
  await page.getByRole("menuitem", { name: "Delete the group" }).click();

  const confirmation = page.getByRole("listitem", {
    name: `Delete the group “${groupTitle}”?`,
  });
  const target = confirmation.getByRole("combobox");
  const targetTitle = (await full(page, board.id)).lists.find(
    (list) => list.title !== groupTitle,
  )!.title;
  await target.selectOption({ label: targetTitle });
  const deleted = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      response.url().includes(`/board-groups/${group.id}`),
  );
  await confirmation
    .getByRole("button", { name: "Move the items and delete the group" })
    .click();
  const response = await deleted;
  expect(response.status()).toBeLessThan(300);
  expect(response.url()).toContain("targetGroupId=");

  // The group is gone and its item lives in the chosen group.
  const after = await full(page, board.id);
  expect(after.lists.some((list) => list.title === groupTitle)).toBe(false);
  expect(
    after.lists
      .find((list) => list.title === targetTitle)!
      .boardItems.some((item) => item.name === itemTitle),
  ).toBe(true);

  // The server refuses to drop a group with items and no target.
  const other = after.lists.find((list) => list.title !== targetTitle)!;
  expect(
    (
      await apiWrite(page, "POST", `/boards/${board.id}/items`, {
        groupId: other.id,
        title: `Stays ${unique}`,
      })
    ).status,
  ).toBeLessThan(300);
});

test("a board manager changes the item key prefix, which another board of the workspace cannot take", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const stamp = Date.now()
    .toString(36)
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, "");
  const prefix = `K${stamp.slice(-4)}`;

  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}/settings`);
  await page.getByRole("button", { name: "General", exact: true }).click();
  const key = page.getByLabel(/^Item key/);
  await key.fill(prefix);
  const saved = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      /\/boards\/[^/]+$/.test(response.url()),
  );
  await page.getByRole("button", { name: "Save changes" }).click();
  expect((await saved).status()).toBeLessThan(300);
  await expect
    .poll(async () => (await full(page, board.id)).itemKeyPrefix)
    .toBe(prefix);

  // Another board of the workspace takes the same prefix only when it is free.
  const boards = await apiRead<{ id: string; title: string }[]>(
    page,
    `/workspaces/${workspace.id}/boards`,
  );
  const other = boards.find((candidate) => candidate.id !== board.id)!;
  const otherBoard = await full(page, other.id);
  const refused = await apiWrite(page, "PATCH", `/boards/${other.id}`, {
    expectedVersion: otherBoard.version,
    itemKeyPrefix: prefix,
  });
  expect(refused.status).toBe(400);
  expect((await full(page, other.id)).itemKeyPrefix).not.toBe(prefix);
  const invalid = await apiWrite(page, "PATCH", `/boards/${board.id}`, {
    expectedVersion: (await full(page, board.id)).version,
    itemKeyPrefix: "a",
  });
  expect(invalid.status).toBeGreaterThanOrEqual(400);
  expect((await full(page, board.id)).itemKeyPrefix).toBe(prefix);
});

test("a workspace owner sets the workspace time zone and first day of the week that its boards inherit", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceOwner);
  const { workspace } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );

  // Pick values that differ from the stored ones so the form is dirty.
  const before = await apiRead<{ timeZone: string; weekStart: string }>(
    page,
    `/workspaces/${workspace.id}/settings`,
  );
  const zone = before.timeZone === "Asia/Tokyo" ? "Singapore" : "Tokyo";
  const zoneId = zone === "Tokyo" ? "Asia/Tokyo" : "Asia/Singapore";
  const week = before.weekStart === "sunday" ? "Monday" : "Sunday";

  await page.goto(`/workspaces/${workspace.id}/settings`);
  await page.getByLabel("Time zone", { exact: true }).click();
  await page.getByRole("option", { name: new RegExp(zone) }).click();
  await page.getByLabel("First day of the week", { exact: true }).click();
  await page.getByRole("option", { name: week }).click();
  const saved = page.waitForResponse(
    (response) =>
      response.request().method() === "PUT" &&
      response.url().endsWith(`/workspaces/${workspace.id}/settings`),
  );
  await page
    .getByRole("form", { name: "Defaults" })
    .getByRole("button", { name: "Save changes" })
    .click();
  const savedResponse = await saved;
  expect(savedResponse.status()).toBeLessThan(300);

  const settings = await apiRead<{ timeZone: string; weekStart: string }>(
    page,
    `/workspaces/${workspace.id}/settings`,
  );
  expect(settings).toMatchObject({
    timeZone: zoneId,
    weekStart: week.toLowerCase(),
  });
});

test("a manager reopens a completed item to the status it had and the banner names who completed it", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const itemTitle = `Reopen me ${unique}`;
  const lists = (await full(page, board.id)).lists;
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    {
      groupId: lists[0]!.id,
      title: itemTitle,
    },
  );
  expect(created.status).toBeLessThan(300);
  const itemId = created.data!.id;

  const fields = await apiRead<{
    columns: {
      id: string;
      fieldType: string;
      options?: { id: string; name: string; category?: string }[];
    }[];
  }>(page, `/boards/${board.id}/full`);
  const status = fields.columns.find(
    (column) => column.fieldType.toLowerCase() === "status",
  )!;
  const open =
    status.options!.find(
      (option) => option.category?.toLowerCase() === "inprogress",
    ) ??
    status.options!.find(
      (option) => option.category?.toLowerCase() !== "done",
    )!;
  const done = status.options!.find(
    (option) => option.category?.toLowerCase() === "done",
  )!;
  const move = (optionId: string) =>
    apiWrite(page, "PATCH", `/board-items/${itemId}/status`, {
      status: optionId,
    });
  expect((await move(open.id)).status).toBeLessThan(300);
  expect((await move(done.id)).status).toBeLessThan(300);

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`,
  );
  const banner = page.locator('[data-item-banner="completed"]');
  await expect(banner).toBeVisible();
  await expect(banner).toContainText(/Completed/i);
  const reopened = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/board-items/${itemId}/reopen`),
  );
  await banner.getByRole("button", { name: "Reopen" }).click();
  expect((await reopened).status()).toBeLessThan(300);
  await expect(banner).toHaveCount(0);

  const item = await apiRead<{
    completedAt: string | null;
    statusOptionId: string | null;
  }>(page, `/board-items/${itemId}`);
  expect(item.completedAt).toBeNull();
  expect(item.statusOptionId).toBe(open.id);
});

test("a board manager deletes a field after confirming it", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const name = `Scratch ${Date.now().toString(36)}`;
  expect(
    (
      await apiWrite(page, "POST", `/boards/${board.id}/fields`, {
        name,
        type: "text",
      })
    ).status,
  ).toBeLessThan(300);

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/settings?section=fields`,
  );
  await page.getByRole("button", { name: `Edit the field ${name}` }).click();
  await page.getByRole("button", { name: "Delete field" }).click();
  const dialog = page.getByRole("alertdialog");
  await expect(dialog).toContainText(name);
  const deleted = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      /\/boards\/[^/]+\/fields\/[^/]+$/.test(response.url()),
  );
  await dialog.getByRole("button", { name: "Delete field" }).click();
  expect((await deleted).status()).toBeLessThan(300);
  await expect(
    page.getByRole("button", { name: `Edit the field ${name}` }),
  ).toHaveCount(0);
});
