import { describe, expect, it } from "vitest";

import {
  markQuotes,
  parseSegments,
  plainText,
  serializeSegments,
  taskIdsOf,
  taskToken,
} from "../doc-page-text";
import { TASK_144 } from "../../verification/docs-ui-fixtures";

describe("document inline text", () => {
  it("round-trips task references through the stored text", () => {
    const stored = `Theo dõi tại ${taskToken("t-1")} và ${taskToken("t-2")}.`;
    const segments = parseSegments(stored, (id) => ({ ...TASK_144, id }));

    expect(segments.map((segment) => segment.kind)).toEqual([
      "text",
      "task",
      "text",
      "task",
      "text",
    ]);
    expect(serializeSegments(segments)).toBe(stored);
    expect(taskIdsOf(stored)).toEqual(["t-1", "t-2"]);
  });

  it("marks the first occurrence of an anchored quote only", () => {
    const marked = markQuotes(
      [{ kind: "text", text: "trong 2 ngày, thật sự trong 2 ngày" }],
      ["trong 2 ngày"],
    );

    expect(marked.map((segment) => segment.kind)).toEqual(["mark", "text"]);
    expect(plainText(marked)).toBe("trong 2 ngày, thật sự trong 2 ngày");
  });

  it("drops removed text when serializing a compare view", () => {
    expect(
      serializeSegments([
        { kind: "text", text: "quá " },
        { kind: "del", text: "1%" },
        { kind: "ins", text: "0,5%" },
      ]),
    ).toBe("quá 0,5%");
  });
});
