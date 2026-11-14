import { describe, expect, it } from "vitest";
import {
  threadEn as en,
  threadVi as vi,
} from "../../comment-thread/comment-thread.copy";
import { threadRefusalMessage } from "../collaboration-refusal";

describe("threadRefusalMessage (UC-COL-A07, A08)", () => {
  it.each([
    ["en", en],
    ["vi", vi],
  ])("says the item is gone on a 404 (%s)", (_name, copy) => {
    expect(threadRefusalMessage({ status: 404 }, copy)).toBe(
      copy.resolveNotFound,
    );
  });

  it.each([
    ["en", en],
    ["vi", vi],
  ])("says who may change the thread on a 403 (%s)", (_name, copy) => {
    expect(threadRefusalMessage({ status: 403 }, copy)).toBe(
      copy.resolveRefused,
    );
  });

  it("falls back to the refusal words for an error without a status", () => {
    expect(threadRefusalMessage(new Error("x"), en)).toBe(en.resolveRefused);
    expect(threadRefusalMessage(null, en)).toBe(en.resolveRefused);
  });
});
