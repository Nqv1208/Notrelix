import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.setTimeout(180_000);

type PageRow = { id: string; parentId: string | null; version: number };
type BlockRow = { id: string; version: number; position: string };

async function createDoc(
  page: Page,
  workspaceId: string,
  title: string,
): Promise<string> {
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspaceId}/pages`,
    { title },
  );
  expect(created.status).toBe(201);
  return created.data as string;
}

async function addBlock(page: Page, pageId: string, text: string) {
  const created = await apiWrite<BlockRow>(
    page,
    "POST",
    `/pages/${pageId}/blocks`,
    { type: "Text", content: { v: 1, runs: [{ text, marks: [] }] } },
  );
  expect(created.status).toBe(201);
  return created.data as BlockRow;
}

async function order(page: Page, pageId: string) {
  const blocks = await apiRead<BlockRow[]>(page, `/pages/${pageId}/blocks`);
  return [...blocks]
    .sort((a, b) => (a.position < b.position ? -1 : 1))
    .map((block) => block.id);
}

test("a page moves under another page with its version, a stale or circular move is refused, and trashing the parent takes the child along", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const parent = await createDoc(page, workspace.id, `Parent ${stamp}`);
  const child = await createDoc(page, workspace.id, `Child ${stamp}`);

  const before = await apiRead<PageRow>(page, `/pages/${child}`);
  const moved = await apiWrite(page, "POST", `/pages/${child}/move`, {
    parentId: parent,
    expectedVersion: before.version,
  });
  expect(moved.status).toBeLessThan(300);
  const after = await apiRead<PageRow>(page, `/pages/${child}`);
  expect(after.parentId).toBe(parent);
  expect(after.version).toBeGreaterThan(before.version);

  // The version the editor read is stale now: nothing moves.
  const stale = await apiWrite(page, "POST", `/pages/${child}/move`, {
    parentId: null,
    expectedVersion: before.version,
  });
  expect(stale.status).toBe(412);
  expect((await apiRead<PageRow>(page, `/pages/${child}`)).parentId).toBe(
    parent,
  );

  // A page cannot move under its own child.
  const parentRow = await apiRead<PageRow>(page, `/pages/${parent}`);
  const cycle = await apiWrite(page, "POST", `/pages/${parent}/move`, {
    parentId: child,
    expectedVersion: parentRow.version,
  });
  expect(cycle.status).toBe(400);

  // Trashing the parent trashes the child with it; restoring brings both back.
  expect(
    (await apiWrite(page, "POST", `/pages/${parent}/archive`, {})).status,
  ).toBeLessThan(300);
  const trashed = await apiRead<{ items: { id: string }[] }>(
    page,
    `/workspaces/${workspace.id}/pages/trash`,
  );
  expect(trashed.items.map((row) => row.id)).toEqual(
    expect.arrayContaining([parent, child]),
  );
  expect(
    (await apiWrite(page, "POST", `/pages/${parent}/restore`, {})).status,
  ).toBeLessThan(300);
  expect((await apiRead<PageRow>(page, `/pages/${child}`)).parentId).toBe(
    parent,
  );
});

test("a block moves after a sibling against its version, a stale move changes nothing, and a block of a trashed page cannot be moved", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const doc = await createDoc(page, workspace.id, `Order ${stamp}`);
  const first = await addBlock(page, doc, "one");
  const second = await addBlock(page, doc, "two");
  const third = await addBlock(page, doc, "three");
  expect(await order(page, doc)).toEqual([first.id, second.id, third.id]);

  const moved = await apiWrite<BlockRow[]>(page, "POST", "/blocks/reorder", {
    pageId: doc,
    items: [
      {
        blockId: first.id,
        expectedVersion: first.version,
        afterBlockId: second.id,
      },
    ],
  });
  expect(moved.status).toBe(200);
  expect(await order(page, doc)).toEqual([second.id, first.id, third.id]);

  // The block changed, so the version read earlier is stale: the order stays.
  const stale = await apiWrite(page, "POST", "/blocks/reorder", {
    pageId: doc,
    items: [
      {
        blockId: first.id,
        expectedVersion: first.version,
        afterBlockId: third.id,
      },
    ],
  });
  expect(stale.status).toBe(412);
  expect(await order(page, doc)).toEqual([second.id, first.id, third.id]);

  // Nothing moves in a page that is in the trash.
  expect(
    (await apiWrite(page, "POST", `/pages/${doc}/archive`, {})).status,
  ).toBeLessThan(300);
  const current = await apiRead<BlockRow[]>(page, `/pages/${doc}/blocks`).catch(
    () => [] as BlockRow[],
  );
  const trashedMove = await apiWrite(page, "POST", "/blocks/reorder", {
    pageId: doc,
    items: [
      {
        blockId: third.id,
        expectedVersion: current.find((b) => b.id === third.id)?.version ?? 1,
        beforeBlockId: second.id,
      },
    ],
  });
  expect(trashedMove.status).toBeGreaterThanOrEqual(400);
});

test("deleting a block needs the version read, and a stale delete keeps the block", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const doc = await createDoc(page, workspace.id, `Delete ${stamp}`);
  const block = await addBlock(page, doc, "to delete");

  const stale = await apiWrite(
    page,
    "DELETE",
    `/pages/${doc}/blocks/${block.id}?expectedVersion=${block.version + 5}`,
  );
  expect(stale.status).toBe(412);
  expect(await order(page, doc)).toEqual([block.id]);

  const deleted = await apiWrite(
    page,
    "DELETE",
    `/pages/${doc}/blocks/${block.id}?expectedVersion=${block.version}`,
  );
  expect(deleted.status).toBeLessThan(300);
  expect(await order(page, doc)).toEqual([]);
});
