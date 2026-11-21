import { describe, expect, it, vi } from "vitest";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import {
  DocBlockSaver,
  type BlockWrite,
  type BlockWriter,
} from "../editor/doc-block-saver";

const draft = (id: string, text: string, version?: number): BlockDraft => ({
  id,
  type: "paragraph",
  content: richText([{ text, marks: [] }]),
  properties: {},
  ...(version !== undefined ? { version } : {}),
});

describe("a refetch that started before a save landed", () => {
  it("does not put the older stored block back over the text that was just saved", async () => {
    const writer: BlockWriter = {
      write: vi.fn(async (ops: readonly BlockWrite[]) =>
        ops.map((op) => ({
          id: op.id,
          version: (op.expectedVersion ?? 0) + 1,
        })),
      ),
    };
    const onRebased = vi.fn();
    const saver = new DocBlockSaver(writer, {
      onRebased,
      schedule: () => 1,
      cancel: () => undefined,
    });
    saver.reset([draft("a", "one", 1)]);
    saver.change([draft("a", "one two", 1)]);
    await saver.flush();

    // The refetch was read at version 1, before the save made version 2.
    expect(saver.mergeStored([draft("a", "one", 1)])).toBe(true);

    expect(onRebased).not.toHaveBeenCalled();
    // The next edit still quotes the stored version 2, so it is not refused as stale.
    saver.change([draft("a", "one two three", 2)]);
    await saver.flush();
    expect(writer.write).toHaveBeenLastCalledWith([
      expect.objectContaining({ id: "a", expectedVersion: 2 }),
    ]);
  });
});
