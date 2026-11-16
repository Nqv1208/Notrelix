import { describe, expect, it } from "vitest";
import {
  estimateLines,
  estimateRowHeight,
} from "../web/components/chat-row-estimate";

describe("chat row height estimate", () => {
  it("counts lines by hard breaks and by characters against the text width", () => {
    expect(estimateLines("short", 500)).toBe(1);
    expect(estimateLines("a\nb", 500)).toBe(2);
    expect(estimateLines("x".repeat(866), 498)).toBe(13);
  });

  it("gives separators their own fixed heights", () => {
    expect(estimateRowHeight({ kind: "day", key: "d", dayKey: "x" }, 700)).toBe(
      36,
    );
    expect(estimateRowHeight({ kind: "unread", key: "u" }, 700)).toBe(40);
  });
});
