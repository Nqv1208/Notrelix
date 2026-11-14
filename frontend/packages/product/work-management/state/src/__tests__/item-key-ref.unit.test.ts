import { describe, expect, it } from "vitest";
import { isItemKeyRef } from "../queries/use-item-by-key";
import { limitOf } from "../queries/use-board-settings";

describe("a link's item reference (UC-WM-A10)", () => {
  it("a key is not an item id", () => {
    expect(isItemKeyRef("ERP-142")).toBe(true);
    expect(isItemKeyRef(" ERP-142 ")).toBe(true);
    expect(isItemKeyRef("12")).toBe(true);
    expect(isItemKeyRef("3f2a1b4c-5d6e-4f70-8a9b-0c1d2e3f4a5b")).toBe(false);
    expect(isItemKeyRef("optimistic-42")).toBe(false);
    expect(isItemKeyRef("")).toBe(false);
    expect(isItemKeyRef(undefined)).toBe(false);
  });
});

describe("a published limit refusal (UC-WM-T03)", () => {
  it("names the limit and its max", () => {
    expect(
      limitOf({
        code: "work-management.limit.items-per-board",
        details: { limit: "items", current: 500, max: 500 },
      }),
    ).toEqual({ name: "items", max: 500 });
  });

  it("is null for any other error", () => {
    expect(limitOf({ code: "work-management.conflict" })).toBeNull();
    expect(limitOf(new Error("network"))).toBeNull();
  });
});
