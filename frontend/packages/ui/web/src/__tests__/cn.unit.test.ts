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

describe("cn z-index roles", () => {
  it("lets a later z role replace an earlier numeric or role z-index", () => {
    expect(cn("z-50", "z-modal")).toBe("z-modal");
    expect(cn("z-modal", "z-tooltip")).toBe("z-tooltip");
    expect(cn("relative z-dropdown", "z-sticky")).toBe("relative z-sticky");
  });

  it("does not drop a z role next to other utilities", () => {
    expect(cn("fixed inset-0 z-backdrop", "bg-black/40")).toBe(
      "fixed inset-0 z-backdrop bg-black/40",
    );
  });
});
