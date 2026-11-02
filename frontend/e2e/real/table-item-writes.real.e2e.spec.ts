import { expect, test, type Browser, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

/**
 * The single-item writes of the board table on the real stack: rename (UC-WM-B04), assignee (UC-WM-B05), due date
 * (UC-WM-D04), status (UC-WM-D05) and priority (UC-WM-D06). Each journey makes the write through the page and
 * checks the server; the rejection is a write that lost the race: another member saves first, the page still sends
 * the version it read, the server answers 412, nothing of the page's write is kept and the toast names who won.
 */
test.use({ viewport: { width: 1440, height: 900 } });

interface ItemRead {
  readonly id: string;
  readonly version: number;
  readonly itemKey: string;
  readonly name: string;
  readonly dueAt: string | null;
  readonly completedAt: string | null;
  readonly statusOptionId: string | null;
  readonly priorityOptionId: string | null;
  readonly members: readonly { readonly userId: string }[];
}

interface Column {
  readonly id: string;
  readonly fieldType: string;
  readonly name: string;
  readonly options:
    | readonly {
        readonly id: string;
        readonly name: string;
        readonly category?: string | null;
      }[]
    | null;
}

async function otherMember(browser: Browser) {
  const context = await browser.newContext();
  const other = await context.newPage();
  await signIn(other, realUsers.workspaceOwner);
  const me = await apiRead<{ id: string; name: string }>(other, "/auth/me");
  return { other, id: me.id, name: me.name, close: () => context.close() };
}

async function scene(page: Page, label: string) {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const columns = (
    await apiRead<{ columns: Column[] }>(page, `/boards/${board.id}/full`)
  ).columns;
  const lists = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists;
  const unique = `${label} ${Date.now().toString(36)}`;
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId: lists[0]!.id, title: unique },
  );
  expect(created.status).toBeLessThan(300);
  const item = await apiRead<ItemRead>(
    page,
    `/board-items/${created.data!.id}`,
  );
  return { workspace, board, columns, item, unique };
}

/** The table, narrowed to the one item by its unique title, and the item's row. */
async function openRow(page: Page, s: Awaited<ReturnType<typeof scene>>) {
  await page.goto(
    `/workspaces/${s.workspace.id}/boards/${s.board.id}?view=table`,
  );
  await page
    .getByRole("searchbox", { name: "Search the table" })
    .fill(s.unique);
  const row = page.locator(`[data-row-id="${s.item.id}"]`);
  await expect(row).toBeVisible();
  return row;
}

/**
 * The page sends the version it read before another member's change, as a page that missed the update would; the
 * server is the real one. Covers the JSON bodies and the query string the item writes carry.
 */
async function sendOldVersion(page: Page, itemId: string, version: number) {
  await page.route(
    new RegExp(`/board-items/${itemId}(/|\\?|$)`),
    async (route) => {
      const request = route.request();
      if (request.method() === "GET") return route.continue();
      const url = new URL(request.url());
      if (url.searchParams.has("expectedVersion")) {
        url.searchParams.set("expectedVersion", String(version));
        return route.continue({ url: url.toString() });
      }
      const body = request.postDataJSON() as Record<string, unknown> | null;
      if (body && "expectedVersion" in body)
        return route.continue({
          postData: JSON.stringify({ ...body, expectedVersion: version }),
        });
      return route.continue();
    },
  );
}

/**
 * The first answer of the item's write that is not the CSRF retry the API calls above provoke, once the board has
 * been read again after it: the next write of the page names the version that read brings.
 */
function itemWriteAnswer(page: Page, itemId: string, method: string) {
  const answer = page.waitForResponse(
    (response) =>
      response.request().method() === method &&
      response.url().includes(`/board-items/${itemId}`) &&
      response.status() !== 403,
  );
  const reread = page
    .waitForResponse(
      (response) =>
        response.request().method() === "GET" &&
        /\/boards\/[^/]+\/full/.test(response.url()),
      { timeout: 8_000 },
    )
    .catch(() => undefined);
  return answer.then(async (response) => {
    await reread;
    // The page takes the answer and the re-read into its cache a moment later; a person cannot click that fast.
    await page.waitForTimeout(600);
    return response;
  });
}

/** Any day of the month the calendar shows: the due date is not what these journeys are about. */
const pickADay = (page: Page) =>
  page.getByRole("grid").getByRole("button").nth(15).click();

const conflictToast = (page: Page, who: string) =>
  page.getByText(new RegExp(`${who} changed “[^”]*” first`));

test("a member renames an item in the table, Escape keeps the old title, and a rename that lost the race is refused and names who won", async ({
  browser,
  page,
}) => {
  const s = await scene(page, "Row rename");
  const row = await openRow(page, s);
  const title = row.getByRole("button", { name: s.unique });

  // Escape: nothing is sent and the title stays.
  await title.dblclick();
  const draft = page.getByRole("textbox", {
    name: `Edit title of ${s.item.itemKey}`,
  });
  await draft.fill("Not saved");
  await draft.press("Escape");
  await expect(row.getByRole("button", { name: s.unique })).toBeVisible();
  expect(
    (await apiRead<ItemRead>(page, `/board-items/${s.item.id}`)).name,
  ).toBe(s.unique);

  // Enter: one write with the version the page read.
  await title.dblclick();
  const renamed = itemWriteAnswer(page, s.item.id, "PATCH");
  const input = page.getByRole("textbox", {
    name: `Edit title of ${s.item.itemKey}`,
  });
  await input.fill(`${s.unique} renamed`);
  await input.press("Enter");
  const answer = await renamed;
  expect(answer.status()).toBeLessThan(300);
  expect(answer.request().postDataJSON()).toMatchObject({
    title: `${s.unique} renamed`,
    expectedVersion: s.item.version,
  });
  const after = await apiRead<ItemRead>(page, `/board-items/${s.item.id}`);
  expect(after.name).toBe(`${s.unique} renamed`);
  expect(after.version).toBe(s.item.version + 1);

  // The rejection: someone else renamed it, the page still holds the old version.
  const writer = await otherMember(browser);
  expect(
    (
      await apiWrite(writer.other, "PATCH", `/board-items/${s.item.id}`, {
        title: `${s.unique} by them`,
        expectedVersion: after.version,
      })
    ).status,
  ).toBeLessThan(300);
  await writer.close();
  await sendOldVersion(page, s.item.id, after.version);
  await page
    .getByRole("button", { name: new RegExp(s.unique) })
    .first()
    .dblclick();
  const lost = itemWriteAnswer(page, s.item.id, "PATCH");
  const second = page.getByRole("textbox", {
    name: `Edit title of ${s.item.itemKey}`,
  });
  await second.fill(`${s.unique} by me`);
  await second.press("Enter");
  expect((await lost).status()).toBe(412);
  await expect(conflictToast(page, writer.name)).toBeVisible();
  expect(
    (await apiRead<ItemRead>(page, `/board-items/${s.item.id}`)).name,
  ).toBe(`${s.unique} by them`);
});

test("a member changes an item's status in the table, Done completes it, and a status change that lost the race is refused and names who won", async ({
  browser,
  page,
}) => {
  const s = await scene(page, "Row status");
  const status = s.columns.find((column) => column.fieldType === "Status")!;
  const done = status.options!.find((option) => option.category === "done")!;
  const open = status.options!.find((option) => option.category !== "done")!;
  const row = await openRow(page, s);

  await row
    .getByRole("button", { name: `Change status of ${s.item.itemKey}` })
    .click();
  const set = itemWriteAnswer(page, s.item.id, "PATCH");
  await page.getByRole("option", { name: new RegExp(done.name) }).click();
  expect((await set).status()).toBeLessThan(300);
  let after = await apiRead<ItemRead>(page, `/board-items/${s.item.id}`);
  expect(after.statusOptionId).toBe(done.id);
  expect(after.completedAt).not.toBeNull();

  // The rejection: someone reopens it first; the page's write carries the version from before.
  const writer = await otherMember(browser);
  expect(
    (
      await apiWrite(
        writer.other,
        "PATCH",
        `/board-items/${s.item.id}/status`,
        {
          status: open.id,
          expectedVersion: after.version,
        },
      )
    ).status,
  ).toBeLessThan(300);
  await writer.close();
  after = await apiRead<ItemRead>(page, `/board-items/${s.item.id}`);
  expect(after.statusOptionId).toBe(open.id);
  await sendOldVersion(page, s.item.id, after.version - 2);
  await page.reload();
  await page
    .getByRole("searchbox", { name: "Search the table" })
    .fill(s.unique);
  await page
    .locator(`[data-row-id="${s.item.id}"]`)
    .getByRole("button", { name: `Change status of ${s.item.itemKey}` })
    .click();
  const lost = itemWriteAnswer(page, s.item.id, "PATCH");
  await page.getByRole("option", { name: new RegExp(done.name) }).click();
  expect((await lost).status()).toBe(412);
  await expect(conflictToast(page, writer.name)).toBeVisible();
  const kept = await apiRead<ItemRead>(page, `/board-items/${s.item.id}`);
  expect(kept.statusOptionId).toBe(open.id);
  expect(kept.completedAt).toBeNull();
});

test("a member sets an item's priority in the table, and a priority change that lost the race is refused and names who won", async ({
  browser,
  page,
}) => {
  const s = await scene(page, "Row priority");
  const priority = s.columns.find(
    (column) => column.fieldType === "Select" && column.name === "Priority",
  )!;
  const [first, second] = priority.options!;
  const row = await openRow(page, s);

  await row
    .getByRole("button", { name: `Change priority of ${s.item.itemKey}` })
    .press("Enter");
  const set = itemWriteAnswer(page, s.item.id, "PATCH");
  await page.getByRole("menuitem", { name: first!.name, exact: true }).click();
  expect((await set).status()).toBeLessThan(300);
  const after = await apiRead<ItemRead>(page, `/board-items/${s.item.id}`);
  expect(after.priorityOptionId).toBe(first!.id);

  const writer = await otherMember(browser);
  expect(
    (
      await apiWrite(
        writer.other,
        "PATCH",
        `/board-items/${s.item.id}/values/${priority.id}`,
        {
          value: second!.id,
          expectedVersion: after.version,
        },
      )
    ).status,
  ).toBeLessThan(300);
  await writer.close();
  await sendOldVersion(page, s.item.id, after.version);
  await page.reload();
  await page
    .getByRole("searchbox", { name: "Search the table" })
    .fill(s.unique);
  await page
    .locator(`[data-row-id="${s.item.id}"]`)
    .getByRole("button", { name: `Change priority of ${s.item.itemKey}` })
    .press("Enter");
  const lost = itemWriteAnswer(page, s.item.id, "PATCH");
  await page.getByRole("menuitem", { name: first!.name, exact: true }).click();
  expect((await lost).status()).toBe(412);
  await expect(conflictToast(page, writer.name)).toBeVisible();
  expect(
    (await apiRead<ItemRead>(page, `/board-items/${s.item.id}`))
      .priorityOptionId,
  ).toBe(second!.id);
});

test("a member assigns and clears an assignee in the table, and an assignment that lost the race is refused and names who won", async ({
  browser,
  page,
}) => {
  const s = await scene(page, "Row assignee");
  const writer = await otherMember(browser);
  const row = await openRow(page, s);

  await row
    .getByRole("button", { name: `Change assignee of ${s.item.itemKey}` })
    .click();
  const assigned = itemWriteAnswer(page, s.item.id, "POST");
  await page.getByRole("button", { name: new RegExp(writer.name) }).click();
  expect((await assigned).status()).toBeLessThan(300);
  let after = await apiRead<ItemRead>(page, `/board-items/${s.item.id}`);
  expect(after.members.map((member) => member.userId)).toEqual([writer.id]);

  await row
    .getByRole("button", { name: `Change assignee of ${s.item.itemKey}` })
    .click();
  const cleared = itemWriteAnswer(page, s.item.id, "DELETE");
  await page.getByRole("button", { name: "Unassigned" }).click();
  expect((await cleared).status()).toBeLessThan(300);
  after = await apiRead<ItemRead>(page, `/board-items/${s.item.id}`);
  expect(after.members).toEqual([]);

  // The rejection: the other member changes the title first; the page still sends the version before it.
  expect(
    (
      await apiWrite(writer.other, "PATCH", `/board-items/${s.item.id}`, {
        title: `${s.unique} touched`,
        expectedVersion: after.version,
      })
    ).status,
  ).toBeLessThan(300);
  await sendOldVersion(page, s.item.id, after.version);
  await page.reload();
  await page
    .getByRole("searchbox", { name: "Search the table" })
    .fill(s.unique);
  await page
    .locator(`[data-row-id="${s.item.id}"]`)
    .getByRole("button", { name: `Change assignee of ${s.item.itemKey}` })
    .click();
  const lost = itemWriteAnswer(page, s.item.id, "POST");
  await page.getByRole("button", { name: new RegExp(writer.name) }).click();
  expect((await lost).status()).toBe(412);
  await expect(conflictToast(page, writer.name)).toBeVisible();
  await writer.close();
  expect(
    (await apiRead<ItemRead>(page, `/board-items/${s.item.id}`)).members,
  ).toEqual([]);
});

test("a member sets and clears a due date in the table, and a due date that lost the race is refused and names who won", async ({
  browser,
  page,
}) => {
  const s = await scene(page, "Row due");
  const row = await openRow(page, s);

  await row
    .getByRole("button", { name: `Change due date of ${s.item.itemKey}` })
    .click();
  const set = itemWriteAnswer(page, s.item.id, "PATCH");
  await pickADay(page);
  expect((await set).status()).toBeLessThan(300);
  let after = await apiRead<ItemRead>(page, `/board-items/${s.item.id}`);
  expect(after.dueAt).not.toBeNull();

  await row
    .getByRole("button", { name: `Change due date of ${s.item.itemKey}` })
    .click();
  const cleared = itemWriteAnswer(page, s.item.id, "PATCH");
  await page.getByRole("button", { name: "Clear due date" }).click();
  const clearedAnswer = await cleared;
  expect(clearedAnswer.status()).toBeLessThan(300);
  after = await apiRead<ItemRead>(page, `/board-items/${s.item.id}`);
  expect(after.dueAt).toBeNull();

  // An inverted range never reaches the table (it sets one date), so the server's own refusal is proved here.
  const inverted = await apiWrite(page, "PATCH", `/board-items/${s.item.id}`, {
    startDate: "2026-12-10T00:00:00Z",
    dueDate: "2026-12-01T00:00:00Z",
    expectedVersion: after.version,
  });
  expect(inverted.status).toBe(400);

  const writer = await otherMember(browser);
  expect(
    (
      await apiWrite(writer.other, "PATCH", `/board-items/${s.item.id}`, {
        title: `${s.unique} touched`,
        expectedVersion: after.version,
      })
    ).status,
  ).toBeLessThan(300);
  await writer.close();
  await sendOldVersion(page, s.item.id, after.version);
  await page.reload();
  await page
    .getByRole("searchbox", { name: "Search the table" })
    .fill(s.unique);
  await page
    .locator(`[data-row-id="${s.item.id}"]`)
    .getByRole("button", { name: `Change due date of ${s.item.itemKey}` })
    .click();
  const lost = itemWriteAnswer(page, s.item.id, "PATCH");
  await pickADay(page);
  expect((await lost).status()).toBe(412);
  await expect(conflictToast(page, writer.name)).toBeVisible();
  expect(
    (await apiRead<ItemRead>(page, `/board-items/${s.item.id}`)).dueAt,
  ).toBeNull();
});
