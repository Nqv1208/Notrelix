import { describe, expect, it, vi } from "vitest";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import {
  DocBlockSaver,
  type BlockWrite,
  type BlockWriter,
} from "../editor/doc-block-saver";

const draft = (text: string, version?: number): BlockDraft => ({
  id: "a",
  type: "paragraph",
  content: richText([{ text, marks: [] }]),
  properties: {},
  ...(version !== undefined ? { version } : {}),
});

describe("flush answers whether everything is stored", () => {
  it("is false while the write fails and true once a retry stores it", async () => {
    let fail = true;
    const writer: BlockWriter = {
      write: vi.fn(async (ops: readonly BlockWrite[]) => {
        if (fail) throw Object.assign(new Error("down"), { status: 500 });
        return ops.map((op) => ({ id: op.id, version: 2 }));
      }),
    };
    const saver = new DocBlockSaver(writer, {
      schedule: () => 1,
      cancel: () => undefined,
    });
    saver.reset([draft("one", 1)]);
    saver.change([draft("one two", 1)]);

    expect(await saver.flush()).toBe(false);
    fail = false;
    expect(await saver.retry()).toBe(true);
    expect(await saver.flush()).toBe(true);
  });
});
