import { describe, expect, it } from "vitest";
import { cn } from "../lib/cn";

describe("cn", () => {
  it("keeps a semantic text role next to a text color", () => {
    expect(cn("text-metadata", "text-muted-foreground")).toBe(
      "text-metadata text-muted-foreground",
    );
    expect(cn("size-5 text-caption", "text-identity-blue-foreground")).toBe(
      "size-5 text-caption text-identity-blue-foreground",
    );
  });

  it("still lets a later text role replace an earlier one", () => {
    expect(cn("text-body", "text-metadata")).toBe("text-metadata");
    expect(cn("text-sm", "text-label")).toBe("text-label");
  });
});
