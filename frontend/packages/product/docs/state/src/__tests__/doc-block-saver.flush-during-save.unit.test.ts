import { describe, expect, it, vi } from "vitest";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import {
  DocBlockSaver,
  type BlockWrite,
  type BlockWriter,
} from "../editor/doc-block-saver";

const draft = (text: string, version: number): BlockDraft => ({
  id: "a",
  type: "paragraph",
  content: richText([{ text, marks: [] }]),
  properties: {},
  version,
});

describe("a flush that starts while a save is in flight", () => {
  it("answers true when the edits queued during that save are stored too", async () => {
    let release: (() => void) | null = null;
    const first = new Promise<void>((resolve) => (release = resolve));
    let calls = 0;
    const writer: BlockWriter = {
      write: vi.fn(async (ops: readonly BlockWrite[]) => {
        calls += 1;
        if (calls === 1) await first;
        return ops.map((op) => ({
          id: op.id,
          version: (op.expectedVersion ?? 0) + 1,
        }));
      }),
    };
    const saver = new DocBlockSaver(writer, {
      schedule: () => 1,
      cancel: () => undefined,
    });
    saver.reset([draft("one", 1)]);
    saver.change([draft("one two", 1)]);
    const running = saver.flush();
    // The member keeps typing while the first write is in flight, then asks for a flush.
    saver.change([draft("one two three", 1)]);
    const waiting = saver.flush();
    (release as unknown as () => void)();

    expect(await waiting).toBe(true);
    expect(await running).toBe(true);
    expect(writer.write).toHaveBeenCalledTimes(2);
    expect(saver.hasPending()).toBe(false);
  });
});
