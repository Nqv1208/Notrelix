import { describe, expect, it } from "vitest";
import { myWorkSearchSchema } from "../router/schemas/my-work-search-schema";

describe("myWorkSearchSchema", () => {
  it("has no item by default and keeps the item the sheet is open on", () => {
    expect(myWorkSearchSchema.parse({})).toEqual({});
    expect(myWorkSearchSchema.parse({ item: "item-1" })).toEqual({
      item: "item-1",
    });
  });

  it("drops what it does not own and rejects a non-string item", () => {
    expect(myWorkSearchSchema.parse({ item: "a", view: "table" })).toEqual({
      item: "a",
    });
    expect(myWorkSearchSchema.safeParse({ item: 3 }).success).toBe(false);
  });
});
