import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import {
  createBlockApi,
  createTemplateApi,
  createVersionApi,
} from "../../../../product/docs/state/src/index";
import { defaultConfig } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { INVOICE_DOC_ID } from "../state/mock-docs-invoice";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  const api = client.api as never;
  const docs = endpoints as never;
  return {
    store,
    versions: createVersionApi(api, docs),
    templates: createTemplateApi(api, docs),
    blocks: createBlockApi(api, docs),
  };
}

const WORKSPACE = mockIds.workspaces.primary;

describe("Documents.Versions on the mock backend", () => {
  it("lists the invoice document's history newest first with names and kinds", async () => {
    const { versions } = setup();

    const list = await versions.list(INVOICE_DOC_ID);

    expect(list.length).toBeGreaterThanOrEqual(5);
    expect(list.map((v) => v.versionNumber)).toEqual(
      [...list.map((v) => v.versionNumber)].sort((a, b) => b - a),
    );
    expect(list.find((v) => v.kind === "named")?.name).toBe(
      "Bản trình Ban giám đốc",
    );
    expect(list.some((v) => v.kind === "restore")).toBe(true);
  });

  it("serves the blocks as they stood in each entry, read-only", async () => {
    const { store, versions } = setup();
    const current = store.getPageBlocks(INVOICE_DOC_ID);
    const list = await versions.list(INVOICE_DOC_ID);

    // The newest entry is the page itself.
    expect(
      (await versions.get(INVOICE_DOC_ID, list[0]!.id)).blocks.length,
    ).toBe(current.length);
    // 10/10 15:42: step 3 as it read, and without the two lines added later.
    const older = await versions.get(INVOICE_DOC_ID, list[2]!.id);
    expect(older.blocks.length).toBe(current.length - 2);
    expect(
      older.blocks.find((block) =>
        String(block.properties.text).startsWith("Hoá đơn sai lệch"),
      )?.properties.text,
    ).toBe("Hoá đơn sai lệch quá 1% chuyển cho kế toán trưởng duyệt.");
    await expect(versions.get(INVOICE_DOC_ID, "nope")).rejects.toMatchObject({
      kind: "not_found",
    });
  });

  it("saves a named version of the page as it is and refuses a blank name", async () => {
    const { store, versions } = setup();
    const before = (await versions.list(INVOICE_DOC_ID)).length;

    const saved = await versions.create(INVOICE_DOC_ID, "  Draft for review ");

    expect(saved).toMatchObject({
      kind: "named",
      name: "Draft for review",
      versionNumber: before + 1,
    });
    const content = await versions.get(INVOICE_DOC_ID, saved.id);
    expect(content.blocks.length).toBe(
      store.getPageBlocks(INVOICE_DOC_ID).length,
    );
    await expect(versions.create(INVOICE_DOC_ID, "   ")).rejects.toMatchObject({
      kind: "validation",
    });
  });

  it("restores an earlier entry: new blocks, a restore entry, and what was replaced kept", async () => {
    const { store, versions, blocks } = setup();
    const list = await versions.list(INVOICE_DOC_ID);
    const target = list[2]!;
    // The page moved on since its newest entry.
    const first = store.getPageBlocks(INVOICE_DOC_ID)[0]!;
    store.updateBlock(first.id, {
      properties: JSON.stringify({ text: "edited since" }),
    });
    const live = await blocks.getList(INVOICE_DOC_ID);

    const restored = await versions.restore(
      INVOICE_DOC_ID,
      target.id,
      live.map((block) => ({ blockId: block.id, version: block.version ?? 0 })),
    );

    expect(restored.version.kind).toBe("restore");
    expect(restored.version.name).toBe(
      `Restored from version ${target.versionNumber}`,
    );
    expect(restored.blocks.length).toBe(live.length - 2);
    expect(store.getPageBlocks(INVOICE_DOC_ID).length).toBe(live.length - 2);
    const after = await versions.list(INVOICE_DOC_ID);
    // The content that was replaced is its own entry, then the restore entry.
    expect(after.slice(0, 2).map((v) => v.kind)).toEqual([
      "restore",
      "automatic",
    ]);
    expect(after[1]!.name).toBe(
      `Before restoring version ${target.versionNumber}`,
    );
    // Nothing was rewritten: the entry restored from is as it was.
    expect((await versions.get(INVOICE_DOC_ID, target.id)).blocks.length).toBe(
      live.length - 2,
    );
  });

  it("answers 412 with the current blocks when a block changed since it was read", async () => {
    const { store, versions, blocks } = setup();
    const list = await versions.list(INVOICE_DOC_ID);
    const live = await blocks.getList(INVOICE_DOC_ID);
    const seen = live.map((block) => ({
      blockId: block.id,
      version: block.version ?? 0,
    }));
    store.updateBlock(live[0]!.id, { position: live[0]!.position });

    await expect(
      versions.restore(INVOICE_DOC_ID, list[2]!.id, seen),
    ).rejects.toMatchObject({ status: 412 });
    expect(store.getPageBlocks(INVOICE_DOC_ID).length).toBe(live.length);
    expect((await versions.list(INVOICE_DOC_ID)).length).toBe(list.length);
  });
});

describe("Documents.Templates on the mock backend", () => {
  it("saves a page as a template and lists it by name", async () => {
    const { templates } = setup();

    const saved = await templates.create(WORKSPACE, {
      pageId: INVOICE_DOC_ID,
      name: "Quy trình",
      description: "Mẫu quy trình",
    });

    expect(saved).toMatchObject({
      name: "Quy trình",
      description: "Mẫu quy trình",
      title: "Quy trình nhập hoá đơn đầu vào",
    });
    expect(saved.blockCount).toBeGreaterThan(5);
    expect((await templates.list(WORKSPACE)).map((t) => t.id)).toEqual([
      saved.id,
    ]);
    await expect(
      templates.create(WORKSPACE, { pageId: INVOICE_DOC_ID, name: " " }),
    ).rejects.toMatchObject({ kind: "validation" });
  });

  it("creates an ordinary page from a template with new block ids", async () => {
    const { store, templates } = setup();
    const template = await templates.create(WORKSPACE, {
      pageId: INVOICE_DOC_ID,
      name: "T",
    });
    const source = store.getPageBlocks(INVOICE_DOC_ID);

    const made = await templates.createPage(WORKSPACE, {
      templateId: template.id,
      title: "Quy trình tháng 11",
    });

    expect(store.getPage(made.pageId)?.title).toBe("Quy trình tháng 11");
    const copied = store.getPageBlocks(made.pageId);
    expect(copied.length).toBe(source.length - made.droppedBlockCount);
    expect(copied.map((b) => b.id)).not.toEqual(
      expect.arrayContaining([source[0]!.id]),
    );
    expect(copied.map((b) => b.properties)).toContain(source[0]!.properties);
  });

  it("leaves images, files and videos out and counts them", async () => {
    const { store, templates } = setup();
    const page = store.createPage(WORKSPACE, { title: "Media" });
    store.createBlock(page.id, {
      type: "paragraph",
      properties: JSON.stringify({ text: "kept" }),
      position: 1000,
    });
    const image = store.createBlock(page.id, {
      type: "image",
      properties: "{}",
      position: 2000,
    });
    store.createBlock(page.id, {
      type: "paragraph",
      properties: JSON.stringify({ text: "under" }),
      position: 1000,
      parentBlockId: image.id,
    });
    const template = await templates.create(WORKSPACE, {
      pageId: page.id,
      name: "Media",
    });

    const made = await templates.createPage(WORKSPACE, {
      templateId: template.id,
    });

    expect(made.droppedBlockCount).toBe(2);
    expect(store.getPageBlocks(made.pageId).map((b) => b.type)).toEqual([
      "paragraph",
    ]);
    expect(store.getPage(made.pageId)?.title).toBe("Media");
  });

  it("does not create a page from a template that is not there", async () => {
    const { templates } = setup();

    await expect(
      templates.createPage(WORKSPACE, { templateId: "nope" }),
    ).rejects.toMatchObject({ kind: "not_found" });
  });
});
