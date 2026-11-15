import type { Block } from "@notrelix/docs-core";
import { describe, expect, it, vi } from "vitest";
import { loadBlockWindows } from "../query/hooks/load-block-windows";

const block = (id: string) => ({ id }) as Block;

describe("loadBlockWindows", () => {
  it("appends every window in order and answers null", async () => {
    const read = vi.fn(async (cursor: string) =>
      cursor === "c1"
        ? { blocks: [block("b")], nextCursor: "c2" }
        : { blocks: [block("c")], nextCursor: null },
    );
    const seen: string[] = [];
    const stalled = await loadBlockWindows(read, "c1", (blocks) =>
      seen.push(...blocks.map((b) => b.id)),
    );
    expect(stalled).toBeNull();
    expect(seen).toEqual(["b", "c"]);
  });

  it("tries a window three times, answers where it stopped, and a retry carries on from there", async () => {
    let down = true;
    const read = vi.fn(async (cursor: string) => {
      if (cursor === "c2" && down) throw new Error("down");
      return cursor === "c1"
        ? { blocks: [block("b")], nextCursor: "c2" }
        : { blocks: [block("c")], nextCursor: null };
    });
    const seen: string[] = [];
    const append = (blocks: Block[]) => seen.push(...blocks.map((b) => b.id));

    expect(await loadBlockWindows(read, "c1", append)).toBe("c2");
    expect(read).toHaveBeenCalledTimes(4);
    expect(seen).toEqual(["b"]);

    down = false;
    expect(await loadBlockWindows(read, "c2", append)).toBeNull();
    expect(seen).toEqual(["b", "c"]);
  });
});
