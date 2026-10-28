import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { INVOICE_DOC_ID } from "../state/mock-docs-invoice";
import { createMockFetch } from "../transport/create-mock-fetch";

interface BlockDto {
  id: string;
  type: string;
  content: { v?: number; runs?: { text: string; marks: unknown[] }[] };
  properties: Record<string, unknown>;
  position: string;
  version: number;
}

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, api: client.api };
}

const BLOCKS = `/pages/${INVOICE_DOC_ID}/blocks`;
const text = (value: string) => ({
  v: 1,
  runs: [{ text: value, marks: [] }],
});

describe("Documents blocks keep the producer's final shapes in the mock world", () => {
  it("lists blocks with rich-text content, properties objects, a version and a string position", async () => {
    const { api } = setup();
    const blocks = await api.get<BlockDto[]>(BLOCKS);
    const todo = blocks.find((block) => block.type === "ToDo")!;
    expect(todo.content.v).toBe(1);
    expect(todo.content.runs?.length).toBeGreaterThan(0);
    expect(typeof todo.properties).toBe("object");
    expect(todo.version).toBe(1);
    expect(typeof todo.position).toBe("string");
  });

  it("creates a block with a client id, refuses a taken id, and refuses an unknown property", async () => {
    const { api } = setup();
    const id = "11111111-1111-4111-8111-111111111111";
    const block = await api.post<BlockDto>(BLOCKS, {
      id,
      type: "code",
      content: text("let a = 1"),
      properties: { language: "TypeScript" },
    });
    expect(block).toMatchObject({ id, type: "Code", version: 1 });
    expect(block.properties.language).toBe("typescript");
    await expect(
      api.post(BLOCKS, { id, type: "paragraph", content: text("x") }),
    ).rejects.toMatchObject({
      status: 409,
      details: { errorCode: "documents.block.id-taken" },
    });
    await expect(
      api.post(BLOCKS, { type: "paragraph", properties: { nope: 1 } }),
    ).rejects.toMatchObject({ status: 400 });
  });

  it("edits against the version read and answers 412 with the stored block otherwise", async () => {
    const { api } = setup();
    const [first] = await api.get<BlockDto[]>(BLOCKS);
    const saved = await api.patch<BlockDto>(`${BLOCKS}/${first!.id}`, {
      expectedVersion: first!.version,
      content: text("Changed"),
    });
    expect(saved.version).toBe(first!.version + 1);
    expect(saved.content.runs?.[0]?.text).toBe("Changed");
    await expect(
      api.patch(`${BLOCKS}/${first!.id}`, {
        expectedVersion: first!.version,
        content: text("Late"),
      }),
    ).rejects.toMatchObject({
      status: 412,
      details: {
        errorCode: "documents.block.version-conflict",
        extensions: { currentBlock: { id: first!.id, version: saved.version } },
      },
    });
  });

  it("applies a batch all or nothing", async () => {
    const { api } = setup();
    const before = await api.get<BlockDto[]>(BLOCKS);
    const [first] = before;
    const fresh = "22222222-2222-4222-8222-222222222222";
    await expect(
      api.post(`${BLOCKS}/batch`, {
        blocks: [
          { id: fresh, type: "paragraph", content: text("new") },
          { id: first!.id, expectedVersion: 99, content: text("stale") },
        ],
      }),
    ).rejects.toMatchObject({ status: 412 });
    expect(await api.get<BlockDto[]>(BLOCKS)).toEqual(before);

    const saved = await api.post<BlockDto[]>(`${BLOCKS}/batch`, {
      blocks: [
        {
          id: fresh,
          type: "paragraph",
          content: text("new"),
          afterBlockId: first!.id,
        },
        {
          id: first!.id,
          expectedVersion: first!.version,
          content: text("kept"),
        },
      ],
    });
    expect(saved.map((block) => block.id)).toEqual([fresh, first!.id]);
    const after = await api.get<BlockDto[]>(BLOCKS);
    expect(after[1]!.id).toBe(fresh);

    await api.post(`${BLOCKS}/batch`, {
      blocks: [{ id: fresh, op: "delete", expectedVersion: 1 }],
    });
    expect(
      (await api.get<BlockDto[]>(BLOCKS)).some((block) => block.id === fresh),
    ).toBe(false);
  });
});

describe("Documents image and file blocks in the mock world", () => {
  it("stores an image and a file block that point at an attachment and reads them back", async () => {
    const { api } = setup();
    const image = await api.post<BlockDto>(BLOCKS, {
      type: "Image",
      content: {},
      properties: {
        attachmentId: "a1",
        width: 320,
        alt: "Sơ đồ",
        caption: [{ text: "Hình 1", marks: [] }],
      },
    });
    const file = await api.post<BlockDto>(BLOCKS, {
      type: "File",
      content: {},
      properties: { attachmentId: "f1" },
    });
    expect(image.type).toBe("Image");
    expect(file.type).toBe("File");
    const blocks = await api.get<BlockDto[]>(BLOCKS);
    expect(blocks.find((block) => block.id === image.id)?.properties).toEqual({
      attachmentId: "a1",
      width: 320,
      alt: "Sơ đồ",
      caption: [{ text: "Hình 1", marks: [] }],
    });
    expect(blocks.find((block) => block.id === file.id)?.properties).toEqual({
      attachmentId: "f1",
    });
  });
});

describe("Page comments anchored to text in the mock world", () => {
  const COMMENTS = `/pages/${INVOICE_DOC_ID}/comments`;
  interface CommentDto {
    id: string;
    contentMd: string;
    anchor?: {
      blockId: string;
      quote: string;
      start: number;
      end: number;
    } | null;
  }

  it("stores an anchor on a block of the page and lists it back", async () => {
    const { api } = setup();
    const blocks = await api.get<BlockDto[]>(BLOCKS);
    const paragraph = blocks.find((block) => block.type === "Text")!;
    const anchor = {
      blockId: paragraph.id,
      quote: "ab",
      start: 1,
      end: 3,
    };
    await api.post(COMMENTS, { contentMd: "why?", anchor });
    const comments = await api.get<CommentDto[]>(COMMENTS);
    expect(comments.find((c) => c.contentMd === "why?")?.anchor).toEqual(
      anchor,
    );
    // The world's own comments: one is anchored to its quoted text, the reply is not.
    expect(
      comments
        .filter((c) => c.contentMd !== "why?" && c.anchor)
        .map((c) => c.anchor?.quote),
    ).toEqual(["trong tối đa 2 ngày làm việc"]);
  });

  it("refuses an anchor on a block that is not on the page, a range that disagrees with its quote, and a reply", async () => {
    const { api } = setup();
    const blocks = await api.get<BlockDto[]>(BLOCKS);
    const blockId = blocks[0]!.id;
    const attempt = (anchor: object, parentCommentId?: string) =>
      api.post(COMMENTS, { contentMd: "x", anchor, parentCommentId });
    const rejected = {
      status: 400,
      details: { errorCode: "Collaboration_Comment_AnchorInvalid" },
    };
    await expect(
      attempt({
        blockId: "22222222-2222-4222-8222-222222222222",
        quote: "ab",
        start: 0,
        end: 2,
      }),
    ).rejects.toMatchObject(rejected);
    await expect(
      attempt({ blockId, quote: "ab", start: 0, end: 9 }),
    ).rejects.toMatchObject(rejected);
    await expect(
      attempt({ blockId, quote: "ab", start: 0, end: 2 }, "pcomment-1"),
    ).rejects.toMatchObject(rejected);
  });
});
