import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(120_000);

interface Full {
  lists: { id: string }[];
  columns: { id: string; fieldType: string; options?: { id: string; name: string }[] }[];
}
interface ItemRead {
  readonly id: string;
  readonly version: number;
  readonly itemKey: string;
}

/** A scratch item in the first group of the board, with a Status option when asked. */
async function makeItem(
  page: Page,
  boardId: string,
  title: string,
  statusIndex?: number,
) {
  const full = await apiRead<Full>(page, `/boards/${boardId}/full`);
  const id = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${boardId}/items`, {
      groupId: full.lists[0]!.id,
      title,
    })
  ).data!.id;
  if (statusIndex !== undefined) {
    const status = full.columns.find(
      (column) => column.fieldType.toLowerCase() === "status",
    )!;
    const seen = await apiRead<ItemRead>(page, `/board-items/${id}`);
    const set = await apiWrite(page, "PATCH", `/board-items/${id}/status`, {
      status: status.options![statusIndex]!.id,
      expectedVersion: seen.version,
    });
    expect(set.status).toBeLessThan(300);
  }
  return apiRead<ItemRead>(page, `/board-items/${id}`);
}

test("a member reminds the assignee and a second reminder inside 24 hours is refused with the time", async ({
  browser,
  page,
}) => {
  // The second member the reminder reaches: seeded in Workspace 3 and assignable.
  const context = await browser.newContext();
  const other = await context.newPage();
  await signIn(other, realUsers.admin);
  const otherWorkspace = await findWorkspace(other, "Workspace 3");
  const { userId } = await apiRead<{ userId: string }>(
    other,
    `/workspaces/${otherWorkspace.id}/members/me`,
  );

  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const item = await makeItem(page, board.id, `Remind ${unique}`);
  expect(
    (
      await apiWrite(page, "POST", `/board-items/${item.id}/assignees`, {
        userId,
        expectedVersion: item.version,
      })
    ).status,
  ).toBeLessThan(300);

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${item.id}`,
  );
  const remind = async () => {
    const popup = page.getByRole("dialog").first();
    await popup.getByRole("button", { name: "Card actions" }).click();
    await page.getByRole("menuitem", { name: /^Remind assignees/ }).click();
    const confirm = page.getByRole("dialog", { name: "Remind assignees?" });
    await expect(confirm).toBeVisible();
    await confirm.getByRole("button", { name: "Send reminder" }).click();
  };

  await remind();
  await expect(
    page.getByText(/^Reminded .*You can remind again after /),
  ).toBeVisible();

  // The second one within 24 hours: the server refuses and the page names the time.
  await remind();
  await expect(
    page.getByText(/You reminded about this item at .*You can remind again after /),
  ).toBeVisible();
  const again = await apiWrite<{ code?: string }>(
    page,
    "POST",
    `/board-items/${item.id}/remind`,
    {},
  );
  expect(again.status).toBe(409);
  expect(JSON.stringify(again.data)).toContain("work.item.remind-too-soon");
  await context.close();
});

test("a member moves an item to another board and the toast opens it there", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const item = await makeItem(page, board.id, `Move me ${unique}`, 0);
  const boards = await apiRead<{ id: string; title: string }[]>(
    page,
    `/workspaces/${workspace.id}/boards`,
  );
  const targets = boards.filter((candidate) => candidate.id !== board.id);
  expect(targets.length).toBeGreaterThan(0);

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${item.id}`,
  );
  await page
    .getByRole("dialog")
    .first()
    .getByRole("button", { name: "Card actions" })
    .click();
  await page.getByRole("menuitem", { name: /^Move board/ }).click();
  const picker = page.getByRole("dialog", { name: /^Move “/ });
  const target = picker.getByRole("option").first();
  const targetName = ((await target.innerText()) ?? "").split("\n")[0]!.trim();
  await target.click();
  await picker.getByRole("button", { name: /^Move to / }).click();

  const toast = page.getByText(
    new RegExp(`^“Move me ${unique}” moved to `),
  );
  await expect(toast).toBeVisible({ timeout: 20_000 });
  await expect(toast).toContainText(targetName);
  const moved = await apiRead<{ boardId: string; itemKey: string }>(
    page,
    `/board-items/${item.id}`,
  );
  expect(moved.boardId).not.toBe(board.id);
  // The toast's own action opens the item on the board it went to.
  await page.getByRole("button", { name: `Open ${moved.itemKey}` }).click();
  await expect(page).toHaveURL(
    new RegExp(`/boards/${moved.boardId}\\?.*item=${item.id}`),
  );
  await expect(
    page.getByRole("dialog").getByText(`Move me ${unique}`).first(),
  ).toBeVisible();
});

test("a bulk status change on three items applies to all three and Undo puts them back", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const items = [] as ItemRead[];
  for (const n of [1, 2, 3])
    items.push(await makeItem(page, board.id, `Bulk ${n} ${unique}`, 0));
  const statusOf = async (id: string) =>
    (await apiRead<{ statusOptionId: string | null }>(page, `/board-items/${id}`))
      .statusOptionId;

  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}?view=table`);
  for (const item of items)
    await page
      .getByRole("checkbox", { name: `Select ${item.itemKey}`, exact: true })
      .check();
  const bar = page.getByRole("toolbar", { name: /Actions for 3 selected/ });
  await bar.getByRole("button", { name: "Status" }).click();
  const full = await apiRead<Full>(page, `/boards/${board.id}/full`);
  const options = full.columns.find(
    (column) => column.fieldType.toLowerCase() === "status",
  )!.options!;
  await page
    .getByRole("menuitem", { name: new RegExp(`^${options[1]!.name}`) })
    .or(page.getByRole("option", { name: new RegExp(`^${options[1]!.name}`) }))
    .first()
    .click();
  await expect(page.getByText("Changed the status of 3 items")).toBeVisible();
  await expect
    .poll(() => Promise.all(items.map((item) => statusOf(item.id))))
    .toEqual(items.map(() => options[1]!.id));

  await page.getByRole("button", { name: "Undo" }).click();
  await expect
    .poll(() => Promise.all(items.map((item) => statusOf(item.id))))
    .toEqual(items.map(() => options[0]!.id));
});
