import { describe, expect, it } from "vitest";
import {
  MARK_TONE_IDS,
  identityToneFor,
  markToneFor,
} from "../core/model/navigation";
import { MARK_TONE_CLASS } from "../web/shell/shell-models";

/**
 * WX-DEC-014: workspace and department (Space) initials chips use six mark
 * tones; until Spaces store a chosen colour the tone is derived from the id.
 */
describe("markToneFor (WX-DEC-014)", () => {
  const letters = Array.from({ length: 26 }, (_, index) =>
    String.fromCharCode(97 + index),
  );

  it("names the six mark tones", () => {
    expect(MARK_TONE_IDS).toEqual([
      "blue",
      "gold",
      "coral",
      "sage",
      "crimson",
      "violet",
    ]);
  });

  it("returns the same tone for the same id across calls", () => {
    for (const id of ["ws-product", "space-ke-toan", "", "Đội ngũ"]) {
      expect(markToneFor(id)).toBe(markToneFor(id));
      expect(MARK_TONE_IDS).toContain(markToneFor(id));
    }
  });

  it("covers all six tones over the ids a..z", () => {
    expect(new Set(letters.map(markToneFor))).toEqual(new Set(MARK_TONE_IDS));
  });

  it("uses the identity hash modulo six (stable across releases)", () => {
    // "a" hashes to 97: 97 % 6 = 1 → gold; "b" 98 % 6 = 2 → coral; "f" 102 % 6 = 0 → blue.
    expect(markToneFor("a")).toBe("gold");
    expect(markToneFor("b")).toBe("coral");
    expect(markToneFor("f")).toBe("blue");
    // The identity palette keeps its own modulo-five mapping.
    expect(identityToneFor("b")).toBe("sage");
  });
});

describe("MARK_TONE_CLASS", () => {
  it("maps every mark tone to its token fill and foreground classes", () => {
    for (const id of MARK_TONE_IDS) {
      expect(MARK_TONE_CLASS[id]).toBe(
        `bg-mark-${id} text-mark-${id}-foreground`,
      );
    }
  });
});
