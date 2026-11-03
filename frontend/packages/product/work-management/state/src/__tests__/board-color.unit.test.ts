import { describe, expect, it } from "vitest";
import { boardColor } from "../api/board-settings.api";

describe("the board colour on the General page", () => {
  it("reads the stored colour of a new board and of a saved one", () => {
    expect(boardColor('{"type":"color","value":"#4E8574"}')).toBe("#4E8574");
    expect(boardColor("#C2410C")).toBe("#C2410C");
  });

  it("falls back to the default for anything else", () => {
    for (const value of [
      "",
      null,
      undefined,
      "var(--background)",
      '{"type":"image","value":"x.png"}',
    ]) {
      expect(boardColor(value)).toBe("#1E6FD0");
    }
  });
});
