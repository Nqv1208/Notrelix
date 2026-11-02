import { expect, test, type Locator, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

/** A column row reads "Name" then its count: the name is the first line. */
const laneName = (text: string) => text.split("\n")[0]!.trim();
/** The row of a column by its name; the row's own name goes on with the count. */
const laneRow = (list: Locator, name: string) =>
  list.getByRole("option", {
    name: new RegExp(`^${name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}(\\s|$)`),
  });

/** A new card of Engineering Tasks that starts in a real lane, made as the Workspace 3 admin. */
async function scene(page: Page) {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    {
      groupId: (
        await apiRead<{ lists: { id: string }[] }>(
          page,
          `/boards/${board.id}/full`,
        )
      ).lists[0]!.id,
      title: `Move ${unique}`,
    },
  );
  expect(created.status).toBeLessThan(300);
  const itemId = created.data!.id;
  // A new card has no Status and "No status" is not a column to pick, so it starts in a real one.
  const status = (
    await apiRead<{
      columns: { id: string; fieldType: string; options?: { id: string }[] }[];
    }>(page, `/boards/${board.id}/full`)
  ).columns.find((column) => column.fieldType.toLowerCase() === "status")!;
  const seen = await apiRead<{ version: number }>(
    page,
    `/board-items/${itemId}`,
  );
  const started = await apiWrite(
    page,
    "PATCH",
    `/board-items/${itemId}/status`,
    { status: status.options![0]!.id, expectedVersion: seen.version },
  );
  expect(started.status, JSON.stringify(started.data)).toBeLessThan(300);

  return { workspace, board, itemId, status };
}

/**
 * The card popup's column picker on the real stack. The board is grouped by Status, so the picker is the
 * Status: picking a lane writes the Status (the same write a drag between lanes makes), the picker is busy
 * while the write is out, and the card is in the new lane after a reload. A member who may not edit sees the
 * column as a label.
 */
test("a member moves a card to another column from its popup and the move survives a reload", async ({
  page,
}) => {
  const { workspace, board, itemId } = await scene(page);
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${itemId}`,
  );
  const popup = page.getByRole("dialog");
  const picker = popup.locator('button[aria-haspopup="listbox"]').first();
  await expect(picker).toBeVisible();
  const before = (await picker.innerText()).trim();

  await picker.click();
  const list = page.getByRole("listbox", { name: "Move to column" });
  const options = await list.getByRole("option").allInnerTexts();
  expect(options.length).toBeGreaterThan(1);
  const target = options.map(laneName).find((name) => name !== before)!;
  await expect(laneRow(list, before)).toHaveAttribute("aria-selected", "true");

  const written = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      response.url().includes(`/board-items/${itemId}/status`),
  );
  await laneRow(list, target).click();
  expect((await written).status()).toBeLessThan(300);
  await expect(picker).toContainText(target);
  await expect(picker).toHaveAttribute("aria-busy", "false");

  await page.reload();
  await expect(
    page.getByRole("dialog").locator('button[aria-haspopup="listbox"]').first(),
  ).toContainText(target);
});

test("a move that lost the race is refused, the card keeps its column, and a viewer sees the column as a label", async ({
  browser,
  page,
}) => {
  const { workspace, board, itemId } = await scene(page);
  const url = `/workspaces/${workspace.id}/boards/${board.id}?item=${itemId}`;
  await page.goto(url);
  const picker = page
    .getByRole("dialog")
    .locator('button[aria-haspopup="listbox"]')
    .first();
  await expect(picker).toBeVisible();
  const before = (await picker.innerText()).trim();
  const read = await apiRead<{
    version: number;
    position?: unknown;
    listId?: unknown;
  }>(page, `/board-items/${itemId}`);

  // Someone else saves the card first; the popup still holds the version it read.
  const other = await browser.newContext();
  const writer = await other.newPage();
  await signIn(writer, realUsers.workspaceOwner);
  const touched = await apiWrite(writer, "PATCH", `/board-items/${itemId}`, {
    title: `Touched ${Date.now().toString(36)}`,
    expectedVersion: read.version,
  });
  expect(touched.status).toBeLessThan(300);
  await other.close();
  await page.route(
    new RegExp(`/board-items/${itemId}/status`),
    async (route) => {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      return route.continue({
        postData: JSON.stringify({ ...body, expectedVersion: read.version }),
      });
    },
  );

  await picker.click();
  const list = page.getByRole("listbox", { name: "Move to column" });
  const options = await list.getByRole("option").allInnerTexts();
  const target = options.map(laneName).find((name) => name !== before)!;
  const refused = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      response.url().includes(`/board-items/${itemId}/status`),
  );
  await laneRow(list, target).click();
  expect((await refused).status()).toBe(412);
  await expect(picker).toHaveAttribute("aria-busy", "false");
  await expect(picker).toContainText(before);
  await page.reload();
  await expect(
    page.getByRole("dialog").locator('button[aria-haspopup="listbox"]').first(),
  ).toContainText(before);
  const kept = await apiRead<{
    version: number;
    position?: unknown;
    listId?: unknown;
  }>(page, `/board-items/${itemId}`);
  // A placed move writes the order first and puts it back when the status is refused: the version moves on, the place does not.
  expect(kept.version).toBeGreaterThanOrEqual(read.version + 1);
  expect(kept.listId).toEqual(read.listId);
  expect(kept.position).toEqual(read.position);

  // A viewer of the board sees the column as a label, with the notice that says why, and no picker to open.
  const guestContext = await browser.newContext();
  const guest = await guestContext.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  const me = await apiRead<{ id: string }>(guest, "/auth/me");
  const grant = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/resources/work-management.board/${board.id}/permissions`,
    { subjectType: "User", subjectId: me.id, level: "Viewer" },
  );
  expect(grant.status).toBeLessThan(300);
  try {
    await guest.goto(url);
    const popup = guest.getByRole("dialog");
    await expect(popup).toBeVisible();
    await expect(
      popup.getByText(before, { exact: true }).first(),
    ).toBeVisible();
    await expect(popup.locator('button[aria-haspopup="listbox"]')).toHaveCount(
      0,
    );
    await expect(popup.getByText("You can only view this card")).toBeVisible();
  } finally {
    // Leave the board as found: the grant would break the next spec.
    const rows = await apiRead<{ id: string; subjectId: string }[]>(
      page,
      `/resources/work-management.board/${board.id}/permissions`,
    );
    for (const row of rows.filter((entry) => entry.subjectId === me.id))
      await apiWrite(
        page,
        "DELETE",
        `/resources/work-management.board/${board.id}/permissions/${row.id}`,
      );
    await guestContext.close();
  }
});

/**
 * The position choice: the select shows where another column holds a card of the same group, and the card is
 * placed at the top of its group in that column, or at the bottom by default.
 */
test("a member chooses the top or the bottom of a column and the order survives a reload", async ({
  page,
}) => {
  const { workspace, board, itemId, status } = await scene(page);
  const unique = Date.now().toString(36);
  const listId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const makeCard = async (title: string, option: string) => {
    const created = await apiWrite<{ id: string }>(
      page,
      "POST",
      `/boards/${board.id}/items`,
      { groupId: listId, title },
    );
    expect(created.status).toBeLessThan(300);
    const id = created.data!.id;
    const seen = await apiRead<{ version: number }>(page, `/board-items/${id}`);
    const set = await apiWrite(page, "PATCH", `/board-items/${id}/status`, {
      status: option,
      expectedVersion: seen.version,
    });
    expect(set.status).toBeLessThan(300);
    return id;
  };
  // An anchor of the same group already sits in the second column; the moved cards start in the first.
  const anchor = await makeCard(`Anchor ${unique}`, status.options![1]!.id);
  const later = await makeCard(`Later ${unique}`, status.options![0]!.id);
  const boardUrl = `/workspaces/${workspace.id}/boards/${board.id}`;

  const move = async (id: string, position?: "top") => {
    await page.goto(`${boardUrl}?item=${id}`);
    const picker = page
      .getByRole("dialog")
      .locator('button[aria-haspopup="listbox"]')
      .first();
    await expect(picker).toBeVisible();
    await picker.click();
    const select = page.getByRole("combobox", {
      name: "Position in the column",
    });
    await expect(select).toBeVisible();
    await expect(select).toHaveValue("bottom");
    if (position) await select.selectOption(position);
    const written = page.waitForResponse(
      (response) =>
        response.request().method() !== "GET" &&
        response.url().includes(`/board-items/${id}`),
    );
    await page
      .getByRole("listbox", { name: "Move to column" })
      .getByRole("option")
      .nth(1)
      .click();
    expect((await written).status()).toBeLessThan(300);
    await expect(picker).toHaveAttribute("aria-busy", "false");
  };
  const laneOrder = async () => {
    await page.goto(boardUrl);
    const lane = page
      .getByRole("region")
      .filter({ has: page.locator(`button[data-card-id="${anchor}"]`) })
      .last();
    await expect(
      lane.locator(`button[data-card-id="${anchor}"]`),
    ).toBeVisible();
    return lane
      .locator("button[data-card-id]")
      .evaluateAll((nodes) =>
        nodes.map((node) => node.getAttribute("data-card-id")),
      );
  };

  await move(itemId, "top");
  let order = await laneOrder();
  expect(order.indexOf(itemId)).toBeGreaterThan(-1);
  expect(order.indexOf(itemId)).toBeLessThan(order.indexOf(anchor));

  await move(later);
  order = await laneOrder();
  expect(order.indexOf(later)).toBeGreaterThan(order.indexOf(anchor));
  expect(order.indexOf(later)).toBeGreaterThan(order.indexOf(itemId));
});
