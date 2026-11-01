import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

interface Child {
  readonly id: string;
  readonly title: string;
  readonly version: number;
  readonly position: string;
}
interface Children {
  readonly items: readonly Child[];
}

test("a member adds sub-items from the card popup, a sub-item has none of its own, and archiving the parent takes them and restoring brings them back", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const parent = await apiWrite<{ id: string; groupId: string }>(
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
      title: `Parent ${unique}`,
    },
  );
  expect(parent.status).toBeLessThan(300);
  const parentId = parent.data!.id;

  // The card popup lists no sub-items yet and adds one from its Add menu on Enter.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${parentId}`,
  );
  const popup = page.getByRole("dialog");
  await expect(popup.getByText("Sub-items", { exact: true })).toHaveCount(0);
  await popup.getByTitle("Add to card").click();
  await page.getByRole("option", { name: "Sub-item" }).click();
  const input = page.getByRole("textbox", { name: "Sub-item" });
  await expect(input).toBeVisible();
  const created = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().includes(`/board-items/${parentId}/child-items`),
  );
  await input.fill(`First ${unique}`);
  await input.press("Enter");
  expect((await created).status()).toBeLessThan(300);
  await expect(popup.getByText("Sub-items", { exact: true })).toBeVisible();
  await expect(popup.getByText(`First ${unique}`)).toBeVisible();

  // A second one goes after it, and moving it first reorders them.
  expect(
    (
      await apiWrite(page, "POST", `/board-items/${parentId}/child-items`, {
        title: `Second ${unique}`,
      })
    ).status,
  ).toBeLessThan(300);
  let children = await apiRead<Children>(
    page,
    `/board-items/${parentId}/child-items`,
  );
  expect(children.items.map((child) => child.title)).toEqual([
    `First ${unique}`,
    `Second ${unique}`,
  ]);
  const [first, second] = children.items as [Child, Child];
  const moved = await apiWrite(
    page,
    "POST",
    `/board-items/${second.id}/move-within-parent`,
    {
      expectedVersion: second.version,
      nextSiblingId: first.id,
    },
  );
  expect(moved.status).toBeLessThan(300);
  children = await apiRead<Children>(
    page,
    `/board-items/${parentId}/child-items`,
  );
  expect(children.items.map((child) => child.title)).toEqual([
    `Second ${unique}`,
    `First ${unique}`,
  ]);

  // One level only.
  const grandchild = await apiWrite(
    page,
    "POST",
    `/board-items/${first.id}/child-items`,
    {
      title: "Not allowed",
    },
  );
  expect(grandchild.status).toBeGreaterThanOrEqual(400);

  // A sub-item stays in its parent's group and is not a row of the group.
  const board_ = await apiRead<{
    lists: { id: string; boardItems: { id: string }[] }[];
  }>(page, `/boards/${board.id}/full`);
  expect(
    board_.lists.flatMap((list) => list.boardItems.map((item) => item.id)),
  ).not.toContain(first.id);

  // Archiving the parent archives its sub-items; restoring brings them back.
  const parentRead = await apiRead<{ version: number }>(
    page,
    `/board-items/${parentId}`,
  );
  expect(
    (
      await apiWrite(page, "POST", `/board-items/${parentId}/archive`, {
        expectedVersion: parentRead.version,
      })
    ).status,
  ).toBeLessThan(300);
  children = await apiRead<Children>(
    page,
    `/board-items/${parentId}/child-items`,
  );
  expect(children.items).toEqual([]);
  const archivedRead = await apiRead<{ version: number }>(
    page,
    `/board-items/${parentId}`,
  );
  const restored = await apiWrite(
    page,
    "POST",
    `/board-items/${parentId}/unarchive?expectedVersion=${archivedRead.version}`,
  );
  expect(restored.status, JSON.stringify(restored.data)).toBeLessThan(300);
  children = await apiRead<Children>(
    page,
    `/board-items/${parentId}/child-items`,
  );
  expect(children.items).toHaveLength(2);
});

test("a table row with sub-items expands to show them", async ({ page }) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const parentId = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId: (
        await apiRead<{ lists: { id: string }[] }>(
          page,
          `/boards/${board.id}/full`,
        )
      ).lists[0]!.id,
      title: `Expandable ${unique}`,
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

  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}?view=table`);
  const row = page.locator(`[data-row-id="${parentId}"]`);
  await expect(row).toBeVisible();
  await expect(page.getByText(`Alpha ${unique}`)).toHaveCount(0);
  await row.getByRole("button", { name: /^Show 2 sub-items/ }).click();
  await expect(page.getByText(`Alpha ${unique}`)).toBeVisible();
  await expect(page.getByText(`Beta ${unique}`)).toBeVisible();
  await row.getByRole("button", { name: /^Hide sub-items/ }).click();
  await expect(page.getByText(`Alpha ${unique}`)).toHaveCount(0);
});
