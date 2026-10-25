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

test("a member adds sub-items on the item page, a sub-item has none of its own, and archiving the parent takes them and restoring brings them back", async ({
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

  // The page lists no sub-items yet and adds one on Enter.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/items/${parentId}`,
  );
  await page.getByRole("button", { name: "Add a sub-item" }).click();
  const input = page.getByRole("textbox", { name: "Sub-items" });
  await expect(input).toBeVisible();
  const created = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().includes(`/board-items/${parentId}/child-items`),
  );
  await input.fill(`First ${unique}`);
  await input.press("Enter");
  expect((await created).status()).toBeLessThan(300);
  await expect(page.getByText(`First ${unique}`)).toBeVisible();

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
  expect(
    (await apiWrite(page, "POST", `/board-items/${parentId}/archive`)).status,
  ).toBeLessThan(300);
  children = await apiRead<Children>(
    page,
    `/board-items/${parentId}/child-items`,
  );
  expect(children.items).toEqual([]);
  expect(
    (await apiWrite(page, "POST", `/board-items/${parentId}/unarchive`)).status,
  ).toBeLessThan(300);
  children = await apiRead<Children>(
    page,
    `/board-items/${parentId}/child-items`,
  );
  expect(children.items).toHaveLength(2);
});
