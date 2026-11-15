import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.setTimeout(240_000);

type BlockRow = {
  id: string;
  type: string;
  version: number;
  position: string;
  parentId: string | null;
};

const text = (value: string, marks: unknown[] = []) => ({
  v: 1,
  runs: [{ text: value, marks }],
});

async function createDoc(page: Page, title: string): Promise<string> {
  const workspace = await findWorkspace(page, "Workspace 3");
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title },
  );
  expect(created.status).toBe(201);
  return created.data as string;
}

async function blocks(page: Page, pageId: string): Promise<BlockRow[]> {
  const { items: rows } = await apiRead<{ items: BlockRow[] }>(
    page,
    `/pages/${pageId}/blocks`,
  );
  return [...rows].sort((a, b) => (a.position < b.position ? -1 : 1));
}

const ids = async (page: Page, pageId: string) =>
  (await blocks(page, pageId)).map((b) => b.id);

test("a block is added with its own id, replayed safely, inserted between two, edited against its version, typed, and deleted against its version", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const doc = await createDoc(page, `Blocks ${stamp}`);
  const create = (body: object) =>
    apiWrite<BlockRow>(page, "POST", `/pages/${doc}/blocks`, body);

  // Add with an id of the editor's choosing; the same body again answers the stored block and adds none.
  const id = crypto.randomUUID();
  const body = { id, type: "Text", content: text("first") };
  const first = await create(body);
  expect(first.status).toBe(201);
  expect(first.data?.version).toBe(1);
  const replay = await create(body);
  expect(replay.status).toBeLessThan(300);
  expect(replay.data?.id).toBe(id);
  expect(await ids(page, doc)).toEqual([id]);

  // The same id with another body is a conflict and changes nothing.
  const clash = await create({ ...body, type: "Heading1" });
  expect(clash.status).toBe(409);
  expect((await blocks(page, doc))[0].type).toBe(first.data?.type);

  // Insert between two blocks; a block that is not on the page is not found.
  const last = (await create({ type: "Text", content: text("last") })).data!;
  const middle = await create({
    type: "Text",
    content: text("middle"),
    afterBlockId: id,
  });
  expect(middle.status).toBe(201);
  expect(await ids(page, doc)).toEqual([id, middle.data!.id, last.id]);
  const missing = await create({
    type: "Text",
    afterBlockId: crypto.randomUUID(),
  });
  expect(missing.status).toBe(404);

  // Edit against the version read: version +1; stale is 412 and the block is unchanged; no-op keeps the version.
  const patch = (blockId: string, data: object) =>
    apiWrite<BlockRow>(page, "PATCH", `/pages/${doc}/blocks/${blockId}`, data);
  const edited = await patch(id, {
    expectedVersion: 1,
    content: text("first, edited", [{ type: "bold" }]),
  });
  expect(edited.status).toBe(200);
  expect(edited.data?.version).toBe(2);
  const stale = await patch(id, {
    expectedVersion: 1,
    content: text("lost update"),
  });
  expect(stale.status).toBe(412);
  const noop = await patch(id, {
    expectedVersion: 2,
    content: text("first, edited", [{ type: "bold" }]),
  });
  expect(noop.status).toBe(200);
  expect(noop.data?.version).toBe(2);
  const noVersion = await patch(id, { content: text("x") });
  expect(noVersion.status).toBe(400);

  // Rich text: an unknown mark and a script link are refused and nothing is stored.
  const badMark = await patch(id, {
    expectedVersion: 2,
    content: text("x", [{ type: "sparkle" }]),
  });
  expect(badMark.status).toBe(400);
  const badLink = await patch(id, {
    expectedVersion: 2,
    content: text("x", [{ type: "link", href: "javascript:alert(1)" }]),
  });
  expect(badLink.status).toBe(400);
  const badProps = await patch(id, {
    expectedVersion: 2,
    properties: { notAProperty: true },
  });
  expect(badProps.status).toBe(400);

  // Change the type in place; an unknown type is refused.
  const typed = await patch(id, { expectedVersion: 2, type: "Heading1" });
  expect(typed.status).toBe(200);
  expect(typed.data?.version).toBe(3);
  expect(typed.data?.type).not.toBe(first.data?.type);
  const unknown = await patch(id, { expectedVersion: 3, type: "Hologram" });
  expect(unknown.status).toBe(400);
  expect((await blocks(page, doc)).find((b) => b.id === id)?.version).toBe(3);

  // Delete against the version read.
  const staleDelete = await apiWrite(
    page,
    "DELETE",
    `/pages/${doc}/blocks/${last.id}?expectedVersion=${last.version + 4}`,
  );
  expect(staleDelete.status).toBe(412);
  const deleted = await apiWrite(
    page,
    "DELETE",
    `/pages/${doc}/blocks/${last.id}?expectedVersion=${last.version}`,
  );
  expect(deleted.status).toBeLessThan(300);
  const gone = await apiWrite(
    page,
    "DELETE",
    `/pages/${doc}/blocks/${last.id}?expectedVersion=${last.version}`,
  );
  expect(gone.status).toBe(404);

  // A page in the trash takes no block write; restored, the blocks are as they were.
  const before = await blocks(page, doc);
  expect(
    (await apiWrite(page, "POST", `/pages/${doc}/archive`, {})).status,
  ).toBeLessThan(300);
  expect((await create({ type: "Text" })).status).toBeGreaterThanOrEqual(400);
  expect(
    (await patch(id, { expectedVersion: 3, content: text("late") })).status,
  ).toBeGreaterThanOrEqual(400);
  expect(
    (await apiWrite(page, "POST", `/pages/${doc}/restore`, {})).status,
  ).toBeLessThan(300);
  expect(await blocks(page, doc)).toEqual(before);
});

test("blocks nest and un-nest, a cycle or a foreign parent is refused, and the order stays strict through hundreds of inserts at one spot", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const doc = await createDoc(page, `Tree ${stamp}`);
  const other = await createDoc(page, `Other ${stamp}`);
  const add = async (pageId: string, value: string, afterBlockId?: string) =>
    (
      await apiWrite<BlockRow>(page, "POST", `/pages/${pageId}/blocks`, {
        type: "Text",
        content: text(value),
        afterBlockId,
      })
    ).data!;
  const reorder = (
    pageId: string,
    item: { blockId: string; expectedVersion: number } & object,
  ) =>
    apiWrite<BlockRow[]>(page, "POST", "/blocks/reorder", {
      pageId,
      items: [item],
    });

  const parent = await add(doc, "parent");
  const child = await add(doc, "child");
  const foreign = await add(other, "elsewhere");

  // Nest the second block under the first.
  const nested = await reorder(doc, {
    blockId: child.id,
    expectedVersion: child.version,
    parentId: parent.id,
  });
  expect(nested.status).toBe(200);
  const afterNest = await blocks(page, doc);
  expect(afterNest.find((b) => b.id === child.id)?.parentId).toBe(parent.id);

  // The parent cannot go under its own child; a parent of another page is refused; the tree is unchanged.
  const parentNow = afterNest.find((b) => b.id === parent.id)!;
  const cycle = await reorder(doc, {
    blockId: parent.id,
    expectedVersion: parentNow.version,
    parentId: child.id,
  });
  expect(cycle.status).toBe(400);
  const crossPage = await reorder(doc, {
    blockId: parent.id,
    expectedVersion: parentNow.version,
    parentId: foreign.id,
  });
  expect(crossPage.status).toBeGreaterThanOrEqual(400);
  expect(await blocks(page, doc)).toEqual(afterNest);

  // Un-nest: the child returns to the page root, after its former parent.
  const childNow = afterNest.find((b) => b.id === child.id)!;
  const unnested = await reorder(doc, {
    blockId: child.id,
    expectedVersion: childNow.version,
    parentId: null,
    afterBlockId: parent.id,
  });
  expect(unnested.status).toBe(200);
  const afterUnnest = await blocks(page, doc);
  expect(afterUnnest.find((b) => b.id === child.id)?.parentId).toBeNull();
  expect(afterUnnest.map((b) => b.id)).toEqual([parent.id, child.id]);

  // Many inserts right after one block: each new block lands first, keys stay short, no version moves.
  const dense = await add(doc, "dense head", parent.id);
  // A batch is one change: a failing operation stores none of it.
  const before = await blocks(page, doc);
  const refused = await apiWrite(page, "POST", `/pages/${doc}/blocks/batch`, {
    blocks: [
      {
        op: "upsert",
        id: crypto.randomUUID(),
        type: "Text",
        content: text("a"),
      },
      { op: "upsert", id: crypto.randomUUID(), type: "Hologram" },
    ],
  });
  expect(refused.status).toBe(400);
  expect(await blocks(page, doc)).toEqual(before);

  // Batches keep the run inside the per-user request limit; each upsert is placed against the blocks before it.
  const expected: string[] = [];
  for (let batch = 0; batch < 3; batch += 1) {
    const items = Array.from({ length: 80 }, (_, i) => {
      const id = crypto.randomUUID();
      expected.unshift(id);
      return {
        op: "upsert",
        id,
        type: "Text",
        content: text(`dense ${batch}-${i}`),
        afterBlockId: dense.id,
      };
    });
    const saved = await apiWrite(page, "POST", `/pages/${doc}/blocks/batch`, {
      blocks: items,
    });
    expect(saved.status).toBeLessThan(300);
  }
  const all = await blocks(page, doc);
  expect(all.map((b) => b.id)).toEqual([
    parent.id,
    dense.id,
    ...expected,
    child.id,
  ]);
  expect(all.every((b) => b.position.length <= 32)).toBe(true);
  expect(new Set(all.map((b) => b.position)).size).toBe(all.length);
  expect(all.find((b) => b.id === dense.id)?.version).toBe(1);
});
