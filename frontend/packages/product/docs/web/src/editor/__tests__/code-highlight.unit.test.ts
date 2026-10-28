import { describe, expect, it } from "vitest";
import {
  CODE_LANGUAGES,
  highlightRanges,
  loadHighlighter,
} from "../code-highlight";

describe("code block highlighting", () => {
  it("colours tokens of a known language by their offsets in the text", async () => {
    const highlighter = await loadHighlighter();
    const text = 'const a = "x"; // note';
    const ranges = highlightRanges(highlighter, "javascript", text);
    const part = (from: number, to: number) => text.slice(from, to);
    const keyword = ranges.find(
      (range) => part(range.from, range.to) === "const",
    );
    const string = ranges.find((range) => part(range.from, range.to) === '"x"');
    const comment = ranges.find((range) =>
      part(range.from, range.to).startsWith("// note"),
    );
    expect(keyword?.class).toContain("text-brand-subtle-foreground");
    expect(string?.class).toContain("text-success-text");
    expect(comment?.class).toContain("text-muted-foreground");
  });

  it("leaves a block with no language or an unknown one plain", async () => {
    const highlighter = await loadHighlighter();
    expect(highlightRanges(highlighter, null, "const a = 1")).toEqual([]);
    expect(highlightRanges(highlighter, "klingon", "const a = 1")).toEqual([]);
    expect(highlightRanges(highlighter, "javascript", "")).toEqual([]);
  });

  it("offers only languages the highlighter has", async () => {
    const highlighter = await loadHighlighter();
    for (const { value } of CODE_LANGUAGES) {
      expect(highlighter.registered(value), value).toBe(true);
    }
  });
});
