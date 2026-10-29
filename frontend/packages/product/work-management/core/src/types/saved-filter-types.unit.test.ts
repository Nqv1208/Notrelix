import { describe, expect, it } from "vitest";
import { criteriaTooLarge, MAX_CRITERIA_BYTES } from "./saved-filter-types";

describe("criteriaTooLarge", () => {
  it("counts UTF-8 bytes of the serialized JSON, as the server does", () => {
    expect(criteriaTooLarge({ filter: "a".repeat(100) })).toBe(false);
    // 3-byte characters: 2800 characters are 8400 bytes, over the cap.
    expect(criteriaTooLarge({ filter: "€".repeat(2800) })).toBe(true);
    expect(criteriaTooLarge({ filter: "a".repeat(MAX_CRITERIA_BYTES) })).toBe(
      true,
    );
  });
});
