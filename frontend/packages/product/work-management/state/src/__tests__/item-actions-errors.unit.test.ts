import { describe, expect, it } from "vitest";
import {
  REMIND_TOO_SOON_CODE,
  classifyMoveError,
  classifyRemindError,
} from "../api/item-extras.api";

describe("classifyMoveError", () => {
  it("names the refusals the dialog explains before it moves anything", () => {
    expect(
      classifyMoveError({ code: "work-management.item.has-dependencies" }),
    ).toBe("deps");
    expect(classifyMoveError({ message: "The item is archived." })).toBe(
      "archived",
    );
    expect(classifyMoveError({ status: 409 })).toBe("conflict");
    expect(classifyMoveError({ status: 500 })).toBe("error");
    expect(classifyMoveError(null)).toBe("error");
  });
});

describe("classifyRemindError", () => {
  it("reads the 24 hour limit and when the next reminder is possible", () => {
    expect(
      classifyRemindError({
        status: 409,
        code: REMIND_TOO_SOON_CODE,
        message: "Already reminded; possible again at 2026-10-30T09:00:00Z.",
      }),
    ).toEqual({ kind: "tooSoon", nextRemindAt: "2026-10-30T09:00:00Z" });
    expect(
      classifyRemindError({ status: 409, code: REMIND_TOO_SOON_CODE }),
    ).toEqual({ kind: "tooSoon", nextRemindAt: null });
  });

  it("tells nobody-in-charge from a failure", () => {
    expect(classifyRemindError({ status: 400 })).toEqual({ kind: "nobody" });
    expect(classifyRemindError({ status: 503 })).toEqual({ kind: "error" });
  });
});
