import { expect, test, type Browser, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

const api = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface ItemRead {
  readonly id: string;
  readonly version: number;
  readonly itemKey: string;
  readonly dueAt: string | null;
}

/** A second member of Workspace 3 in its own browser context: the "someone else" of a notice, and the name it must show. */
async function otherMember(browser: Browser) {
  const context = await browser.newContext();
  const other = await context.newPage();
  await signIn(other, realUsers.workspaceOwner);
  const { name } = await apiRead<{ name: string }>(other, "/auth/me");
  return { other, name, close: () => context.close() };
}

/** A scratch item in the first lane of the board: its id, key and read version. */
async function makeItem(page: Page, boardId: string, title: string) {
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${boardId}/full`)
  ).lists[0]!.id;
  const id = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${boardId}/items`, {
      groupId,
      title,
    })
  ).data!.id;
  return apiRead<ItemRead>(page, `/board-items/${id}`);
}

async function deleteItem(page: Page, item: ItemRead) {
  return apiWrite(
    page,
    "DELETE",
    `/board-items/${item.id}?expectedVersion=${item.version}`,
  );
}

test("the card actions menu lists one ordered set and an item deleted by someone else while its card is open says who deleted it", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const item = await makeItem(page, board.id, `Open then gone ${unique}`);
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${item.id}`,
  );
  const dialog = page.getByRole("dialog", {
    name: `Open then gone ${unique}`,
  });
  await expect(dialog).toBeVisible();

  await dialog.getByRole("button", { name: "Card actions" }).click();
  const entries = await page.getByRole("menuitem").allTextContents();
  const order = [
    "Copy link",
    "Mark complete",
    "Remind assignees",
    "Duplicate",
    "Move board",
    "Archive",
    "Delete",
  ];
  const positions = order.map((name) =>
    entries.findIndex((entry) => entry.startsWith(name)),
  );
  expect(positions.every((position) => position >= 0)).toBe(true);
  expect(positions).toEqual([...positions].sort((a, b) => a - b));
  await page.keyboard.press("Escape");

  // Another member deletes the item while the card is open: the card names them and shows nothing of the item.
  const deleter = await otherMember(browser);
  expect((await deleteItem(deleter.other, item)).status).toBe(204);
  await deleter.close();
  const gone = page.getByRole("dialog", { name: "Card" });
  await expect(gone.getByText(`${deleter.name} deleted this card`)).toBeVisible(
    { timeout: 15_000 },
  );
  await expect(
    gone.getByText("The card is gone, or you no longer have access"),
  ).toHaveCount(0);
  await expect(gone.getByText(`Open then gone ${unique}`)).toHaveCount(0);
  await gone.getByRole("button", { name: "Back to the board" }).click();
  await expect(gone).toHaveCount(0);
  await expect(
    page.getByRole("button", { name: `Open then gone ${unique}` }),
  ).toHaveCount(0);
});

test("a card opens with its sections, Escape returns focus to the card, and a missing card reads like one the person cannot see", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const opener = page.getByRole("button", { name: /Task \d+/ }).first();
  const openerName = (await opener.getAttribute("aria-label")) ?? undefined;
  await opener.click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  for (const region of [
    "Description",
    "Custom fields",
    "Attachments",
    "Links and dependencies",
  ])
    await expect(dialog.getByRole("region", { name: region })).toBeVisible();
  await expect(
    dialog.getByRole("complementary", { name: "Comments and activity" }),
  ).toBeVisible();

  await page.keyboard.press("Escape");
  await expect(dialog).toHaveCount(0);
  await expect(page).not.toHaveURL(/[?&]item=/);
  if (openerName)
    await expect(
      page.getByRole("button", { name: openerName }).first(),
    ).toBeFocused();

  // A card that does not exist: one generic message, nothing about any card.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${crypto.randomUUID()}`,
  );
  await expect(
    page
      .getByRole("dialog")
      .getByText("The card is gone, or you no longer have access"),
  ).toBeVisible();
});

test("copy link puts a card address on the clipboard and the same address reads as not found to a person with no access", async ({
  page,
  browser,
}) => {
  await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const item = await makeItem(page, board.id, `Share me ${unique}`);
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${item.id}`,
  );
  const dialog = page.getByRole("dialog", { name: `Share me ${unique}` });
  await dialog.getByRole("button", { name: "Card actions" }).click();
  await page.getByRole("menuitem", { name: /^Copy link/ }).click();
  await expect(page.getByText("Item link copied")).toBeVisible();
  const link = await page.evaluate(() => navigator.clipboard.readText());
  expect(link).toContain(`item=${item.id}`);

  // A Guest with no grant on the board: the real card and a card that never existed answer the same way.
  const baseURL = process.env.REAL_E2E_BASE_URL ?? "http://127.0.0.1:5173";
  const outsider = await browser.newContext({ baseURL });
  try {
    const other = await outsider.newPage();
    await signIn(other, realUsers.workspaceGuest);
    const real = await other.request.get(`${api}/board-items/${item.id}`);
    const missing = await other.request.get(
      `${api}/board-items/${crypto.randomUUID()}`,
    );
    expect(real.status()).toBe(404);
    expect(missing.status()).toBe(404);
    const strip = (body: Record<string, unknown>) => ({
      type: body.type,
      title: body.title,
      status: body.status,
      errorCode: body.errorCode,
    });
    expect(strip(await real.json())).toEqual(strip(await missing.json()));
  } finally {
    await outsider.close();
  }
});

test("a bulk change with one stale item changes nothing, names that item, and the table keeps the pick", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const first = await makeItem(page, board.id, `Bulk first ${unique}`);
  const second = await makeItem(page, board.id, `Bulk second ${unique}`);

  // The server alone: someone changes the second item after the caller read both.
  expect(
    (
      await apiWrite(page, "PATCH", `/board-items/${second.id}`, {
        title: `Bulk second ${unique} edited`,
        expectedVersion: second.version,
      })
    ).status,
  ).toBeLessThan(300);
  const refused = await apiWrite<{ itemId?: string }>(
    page,
    "POST",
    `/boards/${board.id}/items/bulk`,
    {
      operation: "set-due-date",
      itemIds: [first.id, second.id],
      expectedVersions: {
        [first.id]: first.version,
        [second.id]: second.version,
      },
      dueDate: "2026-12-01T10:00:00Z",
    },
  );
  expect(refused.status).toBe(412);
  expect(refused.data?.itemId).toBe(second.id);
  const afterFirst = await apiRead<ItemRead>(page, `/board-items/${first.id}`);
  expect(afterFirst.dueAt).toBeNull();
  expect(afterFirst.version).toBe(first.version);

  // The table: the pick is made, the second item changes behind it, the bulk change is refused.
  const current = await apiRead<ItemRead>(page, `/board-items/${second.id}`);
  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}?view=table`);
  await page
    .getByRole("checkbox", { name: `Select ${first.itemKey}`, exact: true })
    .check();
  await page
    .getByRole("checkbox", { name: `Select ${current.itemKey}`, exact: true })
    .check();
  expect(
    (
      await apiWrite(page, "PATCH", `/board-items/${second.id}`, {
        title: `Bulk second ${unique} again`,
        expectedVersion: current.version,
      })
    ).status,
  ).toBeLessThan(300);
  // The board refreshes on its own through realtime, so the request is sent with the version read before
  // the change, as a page that missed the update would send it; the server is the real one.
  await page.route(`**/boards/${board.id}/items/bulk`, async (route) => {
    const body = route.request().postDataJSON() as {
      expectedVersions: Record<string, number>;
    };
    body.expectedVersions[second.id] = second.version;
    await route.continue({ postData: JSON.stringify(body) });
  });
  const bar = page.getByRole("toolbar", { name: /Actions for 2 selected/ });
  await bar.getByRole("button", { name: "Set due date" }).click();
  await page.getByLabel("Due date", { exact: true }).fill("2026-12-01");
  // The API calls above renewed the CSRF token, so the page first meets a 403 and retries with a fresh token.
  const answered = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/boards/${board.id}/items/bulk`) &&
      response.status() !== 403,
  );
  await page.getByRole("button", { name: "Apply" }).click();
  const bulkAnswer = await answered;
  expect(bulkAnswer.status()).toBe(412);
  await expect(
    page.getByText(
      new RegExp(
        `Couldn't change “Bulk second ${unique}[^”]*”, so nothing was changed. Your selection is still there.`,
      ),
    ),
  ).toBeVisible();
  await expect(bar).toBeVisible();
  const unchanged = await apiRead<ItemRead>(page, `/board-items/${first.id}`);
  expect(unchanged.dueAt).toBeNull();
});

test("two boards keep their own Status options", async ({ page }) => {
  await signIn(page, realUsers.workspaceAdmin);
  const workspace = await findWorkspace(page, "Workspace 3");
  const boards = await apiRead<{ id: string; title: string }[]>(
    page,
    `/workspaces/${workspace.id}/boards`,
  );
  const [one, two] = boards;
  interface Column {
    id: string;
    fieldType: string;
    version: number;
    options: { id: string; name: string }[] | null;
  }
  const status = async (boardId: string) =>
    (
      await apiRead<{ columns: Column[] }>(page, `/boards/${boardId}/full`)
    ).columns.find((column) => column.fieldType === "Status")!;

  const before = await status(two!.id);
  const unique = Date.now().toString(36);
  const own = await status(one!.id);
  expect(own.id).not.toBe(before.id);
  const added = await apiWrite(
    page,
    "POST",
    `/boards/${one!.id}/fields/${own.id}/options`,
    {
      name: `Review ${unique}`,
      color: "#8E24AA",
      expectedVersion: own.version,
      category: "in_progress",
    },
  );
  expect(added.status).toBeLessThan(300);

  const afterOne = await status(one!.id);
  const afterTwo = await status(two!.id);
  expect(afterOne.options!.map((option) => option.name)).toContain(
    `Review ${unique}`,
  );
  expect(afterTwo.options!.map((option) => option.name)).not.toContain(
    `Review ${unique}`,
  );
  expect(afterTwo.options).toEqual(before.options);
  const shared = afterOne.options!.filter((option) =>
    afterTwo.options!.some((candidate) => candidate.id === option.id),
  );
  expect(shared).toEqual([]);
});

test("a link is removed once, a second removal is not found, and a trashed target cannot be linked", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const unique = Date.now().toString(36);
  const source = await makeItem(page, board.id, `Link source ${unique}`);
  const target = await makeItem(page, board.id, `Link target ${unique}`);

  const created = await apiWrite<string>(
    page,
    "POST",
    `/board-items/${source.id}/links`,
    { itemId: target.id, relation: "blocks" },
  );
  expect(created.status).toBeLessThan(300);
  const listed = await apiRead<{ links?: { id: string }[] } | { id: string }[]>(
    page,
    `/board-items/${source.id}/links`,
  );
  const links = Array.isArray(listed) ? listed : (listed.links ?? []);
  expect(links.length).toBe(1);
  const linkId = links[0]!.id;

  const removed = await apiWrite(
    page,
    "DELETE",
    `/board-items/${source.id}/links/${linkId}`,
  );
  expect(removed.status).toBe(204);
  const again = await apiWrite(
    page,
    "DELETE",
    `/board-items/${source.id}/links/${linkId}`,
  );
  expect(again.status).toBe(404);

  // The target is trashed before the link is written: the write fails and no link exists.
  expect((await deleteItem(page, target)).status).toBe(204);
  const late = await apiWrite(page, "POST", `/board-items/${source.id}/links`, {
    itemId: target.id,
    relation: "blocks",
  });
  expect(late.status).toBe(404);
  const after = await apiRead<{ links?: unknown[] } | unknown[]>(
    page,
    `/board-items/${source.id}/links`,
  );
  expect((Array.isArray(after) ? after : (after.links ?? [])).length).toBe(0);
});

test("a card save that carries a stale version is refused, changes nothing and the conflict notice names who saved first", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const item = await makeItem(page, board.id, `Stale save ${unique}`);
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${item.id}`,
  );
  const dialog = page.getByRole("dialog", { name: `Stale save ${unique}` });
  const title = dialog.getByRole("textbox", { name: "Card title" });
  await expect(title).toBeVisible();

  // Another member saved first: the item moves on while the page still holds the version it read.
  const writer = await otherMember(browser);
  expect(
    (
      await apiWrite(writer.other, "PATCH", `/board-items/${item.id}`, {
        title: `Stale save ${unique} by someone else`,
        expectedVersion: item.version,
      })
    ).status,
  ).toBeLessThan(300);
  await writer.close();
  await page.route(`**/board-items/${item.id}`, async (route) => {
    if (route.request().method() !== "PATCH") return route.continue();
    const body = route.request().postDataJSON() as Record<string, unknown>;
    await route.continue({
      postData: JSON.stringify({ ...body, expectedVersion: item.version }),
    });
  });
  const refused = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      response.url().endsWith(`/board-items/${item.id}`) &&
      response.status() !== 403,
  );
  await title.fill(`Stale save ${unique} by me`);
  await title.blur();
  expect((await refused).status()).toBe(412);
  await expect(
    dialog.getByText(`${writer.name} just edited this card`),
  ).toBeVisible();
  await page.unroute(`**/board-items/${item.id}`);
  const after = await apiRead<{ name: string }>(
    page,
    `/board-items/${item.id}`,
  );
  expect(after.name).toBe(`Stale save ${unique} by someone else`);
});

test("a description is saved from the card, the same text changes nothing and one over 20,000 characters is refused", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const item = await makeItem(page, board.id, `Describe me ${unique}`);
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${item.id}`,
  );
  const dialog = page.getByRole("dialog", { name: `Describe me ${unique}` });
  await dialog.getByRole("button", { name: "Add a description…" }).click();
  const saved = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      response.url().endsWith(`/board-items/${item.id}`) &&
      response.status() !== 403,
  );
  await page.keyboard.type(`Written in the card ${unique}`);
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  expect((await saved).status()).toBeLessThan(300);
  const written = await apiRead<{
    descriptionMd?: string;
    description?: string;
    version: number;
  }>(page, `/board-items/${item.id}`);
  expect(written.descriptionMd ?? written.description).toBe(
    `Written in the card ${unique}`,
  );

  // The same text again: nothing changes, so the version stays.
  const same = await apiWrite(page, "PATCH", `/board-items/${item.id}`, {
    descriptionMd: `Written in the card ${unique}`,
    expectedVersion: written.version,
  });
  expect(same.status).toBeLessThan(300);
  const afterSame = await apiRead<{ version: number }>(
    page,
    `/board-items/${item.id}`,
  );
  expect(afterSame.version).toBe(written.version);

  // 20,001 characters are refused and the text stays.
  const tooLong = await apiWrite(page, "PATCH", `/board-items/${item.id}`, {
    descriptionMd: "x".repeat(20_001),
    expectedVersion: afterSame.version,
  });
  expect(tooLong.status).toBe(400);
  const afterLong = await apiRead<{ version: number }>(
    page,
    `/board-items/${item.id}`,
  );
  expect(afterLong.version).toBe(afterSame.version);
});
