import { describe, expect, it, vi } from "vitest";
import { richText, type BlockDraft, type BlockType } from "@notrelix/docs-core";
import {
  BlockVersionConflict,
  DocBlockSaver,
  planBlockWrites,
  type BlockWrite,
  type BlockWriter,
} from "../editor/doc-block-saver";
import {
  conflictBlockOf,
  createBatchBlockWriter,
  toBatchItem,
} from "../editor/batch-block-writer";
import {
  decodeBlockBody,
  encodeContent,
  encodeProperties,
} from "../model/block-wire";

function draft(
  id: string,
  text: string,
  version?: number,
  type: BlockType = "paragraph",
): BlockDraft {
  return {
    id,
    type,
    content: richText(text ? [{ text, marks: [] }] : []),
    properties: {},
    ...(version !== undefined ? { version } : {}),
  };
}

/** A producer that bumps every written block's version. */
function writer(overrides: Partial<BlockWriter> = {}) {
  const batches: BlockWrite[][] = [];
  const w: BlockWriter = {
    write: vi.fn(async (ops: readonly BlockWrite[]) => {
      batches.push([...ops]);
      return ops
        .filter((op: BlockWrite) => op.op === "upsert")
        .map((op: BlockWrite) => ({
          id: op.id,
          version: (op.expectedVersion ?? 0) + 1,
        }));
    }),
    ...overrides,
  };
  return { w, batches };
}

function saverWith(w: BlockWriter, extra = {}) {
  const statuses: string[] = [];
  const saver = new DocBlockSaver(w, {
    onStatus: (s) => statuses.push(s),
    schedule: () => 1,
    cancel: () => undefined,
    ...extra,
  });
  return { saver, statuses };
}

const summary = (ops: readonly BlockWrite[]) =>
  ops.map((op: BlockWrite) =>
    op.op === "delete"
      ? `delete ${op.id}@${op.expectedVersion}`
      : `${op.patch ? "edit" : "create"} ${op.id}${
          op.expectedVersion ? `@${op.expectedVersion}` : ""
        }${op.afterId ? ` after ${op.afterId}` : ""}`,
  );

describe("planBlockWrites", () => {
  it("edits only the changed block against its version", () => {
    const ops = planBlockWrites(
      [draft("a", "x", 1), draft("b", "y", 2)],
      [draft("a", "x!"), draft("b", "y")],
    );
    expect(summary(ops)).toEqual(["edit a@1"]);
  });

  it("creates after the predecessor with the client id and deletes with the version", () => {
    const ops = planBlockWrites(
      [draft("a", "x", 1), draft("b", "y", 2)],
      [draft("a", "x"), draft("n1", "1"), draft("n2", "2")],
    );
    expect(summary(ops)).toEqual([
      "delete b@2",
      "create n1 after a",
      "create n2 after n1",
    ]);
  });

  it("stores a block that has to be first last and places the rest behind it", () => {
    const ops = planBlockWrites(
      [draft("a", "x", 1), draft("b", "y", 2)],
      [draft("n", "new"), draft("a", "x"), draft("b", "y")],
    );
    expect(summary(ops)).toEqual([
      "create n",
      "edit a@1 after n",
      "edit b@2 after a",
    ]);
  });

  it("moves a block by placing it after its new neighbour", () => {
    const ops = planBlockWrites(
      [draft("a", "1", 1), draft("b", "2", 1), draft("c", "3", 1)],
      [draft("a", "1"), draft("c", "3"), draft("b", "2")],
    );
    expect(summary(ops)).toEqual(["edit c@1 after a"]);
  });

  it("sends a retype with the type, content and properties", () => {
    const [op] = planBlockWrites(
      [draft("a", "x", 1)],
      [draft("a", "x", undefined, "heading_1")],
    );
    expect(toBatchItem(op as BlockWrite)).toEqual({
      id: "a",
      op: "upsert",
      type: "Heading1",
      content: { v: 1, runs: [{ text: "x", marks: [] }] },
      properties: {},
      expectedVersion: 1,
    });
  });
});

describe("DocBlockSaver", () => {
  it("writes the changes as one batch and reports saving then saved", async () => {
    const { w, batches } = writer();
    const saved = vi.fn();
    const { saver, statuses } = saverWith(w, { onSaved: saved });
    saver.reset([draft("a", "x", 1), draft("b", "y", 1)]);
    saver.change([draft("a", "x!"), draft("b", "y"), draft("n", "z")]);
    await saver.flush();
    expect(batches).toHaveLength(1);
    expect(summary(batches[0] as BlockWrite[])).toEqual([
      "edit a@1",
      "create n after b",
    ]);
    expect(statuses).toEqual(["saving", "saved"]);
    // The answered versions are the next writes' expected versions.
    const stored = saved.mock.calls[0]?.[0] as BlockDraft[];
    expect(stored.map((d) => [d.id, d.version])).toEqual([
      ["a", 2],
      ["b", 1],
      ["n", 1],
    ]);
    saver.change([draft("a", "x!!"), draft("b", "y"), draft("n", "z")]);
    await saver.flush();
    expect(summary(batches[1] as BlockWrite[])).toEqual(["edit a@2"]);
  });

  it("keeps the text pending after a failure and retries the same writes", async () => {
    let fail = true;
    const { w, batches } = writer({
      write: vi.fn(async (ops: readonly BlockWrite[]) => {
        if (fail) throw new Error("offline");
        batches.push([...ops]);
        return ops.map((op: BlockWrite) => ({ id: op.id, version: 2 }));
      }),
    });
    const { saver, statuses } = saverWith(w);
    saver.reset([draft("a", "1", 1)]);
    saver.change([draft("a", "1!")]);
    await saver.flush();
    expect(saver.getStatus()).toBe("error");
    fail = false;
    await saver.retry();
    expect(summary(batches[0] as BlockWrite[])).toEqual(["edit a@1"]);
    expect(statuses.at(-1)).toBe("saved");
  });

  it("takes the stored block on a 412, hands over the member's text and retries the rest", async () => {
    const server = draft("a", "theirs", 5);
    let calls = 0;
    const seen: string[][] = [];
    const { w } = writer({
      write: vi.fn(async (ops: readonly BlockWrite[]) => {
        calls += 1;
        seen.push(summary(ops));
        if (calls === 1) throw new BlockVersionConflict(server);
        return ops.map((op: BlockWrite) => ({ id: op.id, version: 9 }));
      }),
    });
    const onConflict = vi.fn();
    const onRebased = vi.fn();
    const { saver, statuses } = saverWith(w, { onConflict, onRebased });
    saver.reset([draft("a", "mine", 1), draft("b", "y", 1)]);
    saver.change([draft("a", "mine, edited"), draft("b", "y!")]);
    await saver.flush();

    expect(onConflict).toHaveBeenCalledTimes(1);
    const conflict = onConflict.mock.calls[0]?.[0];
    expect(conflict.blockId).toBe("a");
    expect(conflict.local.content.runs[0].text).toBe("mine, edited");
    expect(conflict.server.content.runs[0].text).toBe("theirs");
    // The editor shows the stored block; b keeps the member's edit.
    const shown = onRebased.mock.calls[0]?.[0] as BlockDraft[];
    expect(shown.map((d) => d.content.runs[0]?.text)).toEqual(["theirs", "y!"]);
    // The whole batch was refused, so the retry carries only b.
    expect(seen).toEqual([["edit a@1", "edit b@1"], ["edit b@1"]]);
    expect(statuses.at(-1)).toBe("saved");
  });

  it("re-adds a block the member removed when it changed meanwhile", async () => {
    const server = draft("b", "theirs", 4);
    let first = true;
    const { w } = writer({
      write: vi.fn(async (ops: readonly BlockWrite[]) => {
        if (first) {
          first = false;
          throw new BlockVersionConflict(server);
        }
        return ops.map((op: BlockWrite) => ({ id: op.id, version: 2 }));
      }),
    });
    const onConflict = vi.fn();
    const onRebased = vi.fn();
    const { saver } = saverWith(w, { onConflict, onRebased });
    saver.reset([draft("a", "1", 1), draft("b", "2", 1)]);
    saver.change([draft("a", "1")]);
    await saver.flush();
    expect(onConflict.mock.calls[0]?.[0].local).toBeNull();
    expect(
      (onRebased.mock.calls[0]?.[0] as BlockDraft[]).map((d) => d.id),
    ).toEqual(["a", "b"]);
  });

  it("merges stored changes only into blocks the member is not editing", () => {
    const { w } = writer();
    const onRebased = vi.fn();
    const { saver } = saverWith(w, { onRebased });
    saver.reset([draft("a", "1", 1), draft("b", "2", 1)]);
    saver.change([draft("a", "1 mine"), draft("b", "2")]);
    const merged = saver.mergeStored([
      draft("a", "1 theirs", 2),
      draft("b", "2 theirs", 2),
      draft("c", "new", 1),
    ]);
    expect(merged).toBe(false);
    const shown = onRebased.mock.calls[0]?.[0] as BlockDraft[];
    expect(shown.map((d) => d.content.runs[0]?.text)).toEqual([
      "1 mine",
      "2 theirs",
    ]);
  });

  it("takes the whole stored document when nothing is pending", () => {
    const { w } = writer();
    const onRebased = vi.fn();
    const { saver } = saverWith(w, { onRebased });
    saver.reset([draft("a", "1", 1)]);
    expect(saver.mergeStored([draft("a", "1", 2)])).toBe(true);
    expect(onRebased).not.toHaveBeenCalled();
    expect(saver.mergeStored([draft("a", "1", 2), draft("c", "new", 1)])).toBe(
      true,
    );
    expect(onRebased).toHaveBeenCalledTimes(1);
  });
});

describe("batch writer", () => {
  it("posts the operations and answers the new versions", async () => {
    const batch = vi.fn().mockResolvedValue([{ id: "a", version: 3 }]);
    const saved = await createBatchBlockWriter({ batch }, "pg").write([
      {
        op: "upsert",
        id: "a",
        draft: draft("a", "x"),
        patch: { content: richText() },
        expectedVersion: 2,
      },
    ]);
    expect(saved).toEqual([{ id: "a", version: 3 }]);
    expect(batch.mock.calls[0]?.[0]).toBe("pg");
    expect(batch.mock.calls[0]?.[1]).toEqual([
      {
        id: "a",
        op: "upsert",
        content: { v: 1, runs: [{ text: "x", marks: [] }] },
        expectedVersion: 2,
      },
    ]);
  });

  it("turns a 412 with the current block into a conflict", async () => {
    const error = Object.assign(new Error("stale"), {
      status: 412,
      code: "documents.block.version-conflict",
      details: {
        errorCode: "documents.block.version-conflict",
        extensions: {
          currentBlock: {
            id: "a",
            pageId: "pg",
            type: "Heading1",
            content: { v: 1, runs: [{ text: "T", marks: [] }] },
            properties: {},
            position: "a0",
            version: 7,
            createdAt: "2026-10-01T00:00:00Z",
          },
        },
      },
    });
    const batch = vi.fn().mockRejectedValue(error);
    const failure = await createBatchBlockWriter({ batch }, "pg")
      .write([{ op: "delete", id: "a", expectedVersion: 1 }])
      .catch((e: unknown) => e);
    expect(failure).toBeInstanceOf(BlockVersionConflict);
    expect((failure as BlockVersionConflict).server).toMatchObject({
      id: "a",
      type: "heading_1",
      version: 7,
    });
    expect(conflictBlockOf(new Error("x"))).toBeNull();
  });
});

describe("block wire", () => {
  it("round-trips rich text beside the block properties", () => {
    const d: BlockDraft = {
      id: "t",
      type: "todo",
      content: richText([{ text: "Do", marks: [{ type: "bold" }] }]),
      properties: { checked: true },
    };
    const decoded = decodeBlockBody("todo", {
      content: encodeContent(d),
      properties: encodeProperties(d),
    });
    expect(decoded.content).toEqual(d.content);
    expect(decoded.properties).toEqual({ checked: true, text: "Do" });
  });

  it("sends a page reference as pageId and a divider as empty objects", () => {
    expect(
      encodeProperties({
        type: "page_reference",
        properties: { linkedPageId: "p" },
      }),
    ).toEqual({ pageId: "p" });
    expect(encodeContent({ type: "divider", content: richText() })).toEqual({});
  });

  it("upgrades a legacy flat body", () => {
    const decoded = decodeBlockBody("paragraph", {
      content: { text: "Hi", bold: true, align: "right" },
    });
    expect(decoded.content.runs).toEqual([
      { text: "Hi", marks: [{ type: "bold" }] },
    ]);
    expect(decoded.properties).toEqual({ align: "right", text: "Hi" });
  });
});
