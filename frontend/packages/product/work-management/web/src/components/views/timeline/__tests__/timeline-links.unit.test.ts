import { describe, expect, it } from "vitest";
import { findLinkCycle, hasLink } from "../timeline-model";

const links = [
  { from: "a", to: "b" },
  { from: "b", to: "c" },
  { from: "x", to: "y" },
];

describe("findLinkCycle", () => {
  it("is null for an unrelated pair", () => {
    expect(findLinkCycle(links, "c", "d")).toBeNull();
    expect(findLinkCycle(links, "a", "y")).toBeNull();
  });

  it("allows a second path to the same item", () => {
    expect(findLinkCycle(links, "a", "c")).toBeNull();
  });

  it("refuses a link to itself", () => {
    expect(findLinkCycle(links, "a", "a")).toEqual(["a", "a"]);
  });

  it("refuses a direct reverse and names the chain", () => {
    expect(findLinkCycle(links, "b", "a")).toEqual(["b", "a", "b"]);
  });

  it("names the whole chain of a longer cycle", () => {
    expect(findLinkCycle(links, "c", "a")).toEqual(["c", "a", "b", "c"]);
  });

  it("terminates on links that already contain a cycle", () => {
    const looped = [...links, { from: "c", to: "a" }];
    expect(findLinkCycle(looped, "q", "r")).toBeNull();
    expect(findLinkCycle(looped, "c", "b")).not.toBeNull();
  });
});

describe("hasLink", () => {
  it("matches the direction only", () => {
    expect(hasLink(links, "a", "b")).toBe(true);
    expect(hasLink(links, "b", "a")).toBe(false);
  });
});
