import { describe, expect, it } from "vitest";
import type { Run } from "@notrelix/docs-core";
import { propertiesFromWire, propertiesToWire } from "../model/block-wire";

const RUNS: Run[] = [
  { text: "Hình ", marks: [{ type: "bold" }] },
  { text: "1", marks: [{ type: "link", href: "https://example.com/" }] },
];

describe("image caption wire shape", () => {
  it("keeps a marked caption as runs next to its plain text", () => {
    const properties = propertiesFromWire("image", {
      attachmentId: "a1",
      caption: RUNS,
    });
    expect(properties.caption).toBe("Hình 1");
    expect(properties.captionRuns).toEqual(RUNS);
  });

  it("keeps an unmarked caption as plain text only", () => {
    const properties = propertiesFromWire("image", {
      caption: [{ text: "Hình 1", marks: [] }],
    });
    expect(properties.caption).toBe("Hình 1");
    expect(properties.captionRuns).toBeUndefined();
  });

  it("writes the runs, and a plain caption as one unmarked run", () => {
    expect(
      propertiesToWire("image", { attachmentId: "a1", captionRuns: RUNS })
        .caption,
    ).toEqual(RUNS);
    expect(propertiesToWire("image", { caption: "Hình 1" }).caption).toEqual([
      { text: "Hình 1", marks: [] },
    ]);
  });

  it("drops marks a caption does not keep and unsafe links", () => {
    const wire = propertiesToWire("image", {
      captionRuns: [
        { text: "a", marks: [{ type: "underline" }, { type: "bold" }] },
        {
          text: "b",
          marks: [{ type: "link", href: "javascript:alert(1)" }],
        },
        { text: "c\nd", marks: [{ type: "color", token: "primary" }] },
      ] as Run[],
    });
    expect(wire.caption).toEqual([
      { text: "a", marks: [{ type: "bold" }] },
      { text: "bc d", marks: [] },
    ]);
  });
});
