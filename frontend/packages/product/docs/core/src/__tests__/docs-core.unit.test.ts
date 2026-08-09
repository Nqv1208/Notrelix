import { describe, it, expect } from "vitest";
import { docsQueryKeys } from "../query/keys";

function validateBlockContent(
  blockType: "paragraph" | "heading" | "code" | "to_do",
  content: string,
): boolean {
  if (blockType === "heading" && content.length > 200) return false;
  return true;
}

describe("Docs Core Invariants", () => {
  it("validates heading length limits", () => {
    expect(validateBlockContent("heading", "Short Heading")).toBe(true);
    expect(validateBlockContent("heading", "a".repeat(250))).toBe(false);
  });

  it("docsQueryKeys should format page detail key", () => {
    expect(docsQueryKeys.detail("page-777")).toEqual([
      "pages",
      "detail",
      "page-777",
    ]);
  });
});
