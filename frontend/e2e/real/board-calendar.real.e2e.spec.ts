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
  lists: {
    id: string;
    boardItems: { id: string; name?: string; dueAt?: string | null }[];
  }[];
}

/** The month the Calendar opens on: the real clock's UTC month. */
const now = new Date();
const month = `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(2, "0")}`;
const day = (n: number) => `${month}-${String(n).padStart(2, "0")}`;

async function itemsOf(page: Page, boardId: string) {
  const full = await apiRead<FullBoard>(page, `/boards/${boardId}/full`);
  return full.lists.flatMap((list) => list.boardItems);
}

async function dueOf(page: Page, boardId: string, itemId: string) {
  const item = (await itemsOf(page, boardId)).find((c) => c.id === itemId);
  return item?.dueAt?.slice(0, 10) ?? null;
}

test("the Calendar moves an item's due date by dragging its handle, makes an item on a clicked day, and a guest cannot edit", async ({
  page,
  browser,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const title = `Cal ${Date.now().toString(36)}`;
  const full = await apiRead<FullBoard>(page, `/boards/${board.id}/full`);
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId: full.lists[0]!.id, title },
  );
  expect(created.status).toBeLessThan(300);
  const itemId = created.data!.id;
  const dated = await apiWrite(
    page,
    "PATCH",
    `/board-items/${itemId}/due-date`,
    {
      startDate: `${day(10)}T09:00:00.000Z`,
      dueDate: `${day(11)}T09:00:00.000Z`,
      expectedVersion: 1,
    },
  );
  expect(dated.status).toBeLessThan(300);

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?view=calendar`,
  );
  await expect(page.getByText(title).first()).toBeVisible();

  // Drag: the due handle goes to the 13th; the server keeps it.
  const handle = page.locator(
    `[data-calendar-handle="due"][data-entry="${itemId}"]`,
  );
  await handle.scrollIntoViewIfNeeded();
  const from = (await handle.boundingBox())!;
  const to = (await page.locator(`[data-day="${day(13)}"]`).boundingBox())!;
  await page.mouse.move(from.x + from.width / 2, from.y + from.height / 2);
  await page.mouse.down();
  await page.mouse.move(to.x + to.width / 2, to.y + to.height / 2, {
    steps: 10,
  });
  await page.mouse.up();
  await expect
    .poll(() => dueOf(page, board.id, itemId), { timeout: 15_000 })
    .toBe(day(13));
  await page.reload();
  await expect(page.getByText(title).first()).toBeVisible();
  expect(await dueOf(page, board.id, itemId)).toBe(day(13));

  // A day click opens the quick add for that day; the new item keeps its due day.
  const clicked = `Click ${Date.now().toString(36)}`;
  const target = (await page.locator(`[data-day="${day(20)}"]`).boundingBox())!;
  await page.mouse.click(target.x + target.width / 2, target.y + 6);
  const input = page.getByRole("textbox", {
    name: new RegExp(`Name of the new item, due 20/${month.slice(5)}`),
  });
  await expect(input).toBeVisible();
  await input.fill(clicked);
  await input.press("Enter");
  await expect(page.getByText(clicked).first()).toBeVisible();
  await expect
    .poll(
      async () =>
        (await itemsOf(page, board.id))
          .find((c) => c.name === clicked)
          ?.dueAt?.slice(0, 10),
      { timeout: 15_000 },
    )
    .toBe(day(20));

  // A guest of the workspace gets no Add item on the Calendar (or no board at all).
  const guestContext = await browser.newContext();
  const guest = await guestContext.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  await guest.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?view=calendar`,
  );
  await guest.waitForLoadState("networkidle");
  await expect(guest.getByRole("button", { name: "Add item" })).toHaveCount(0);
  await expect(
    guest.locator("[data-calendar-handle]:not([disabled])"),
  ).toHaveCount(0);
  await guestContext.close();
});
