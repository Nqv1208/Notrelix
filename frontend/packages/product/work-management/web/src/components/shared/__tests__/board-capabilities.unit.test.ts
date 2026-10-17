import { describe, expect, it } from "vitest";
import { canManageStatuses } from "../board-capabilities";

describe("canManageStatuses", () => {
  it("gives managers and editors the controls by default", () => {
    expect(canManageStatuses("manager", ["manager", "editor"])).toBe(true);
    expect(canManageStatuses("editor", ["manager", "editor"])).toBe(true);
  });

  it("hides them from an editor when only managers are allowed", () => {
    expect(canManageStatuses("editor", ["manager"])).toBe(false);
    expect(canManageStatuses("manager", ["manager"])).toBe(true);
  });

  it("never gives them to a commenter, a viewer or an unknown level", () => {
    for (const level of ["commenter", "viewer", null, undefined] as const) {
      expect(canManageStatuses(level, ["manager", "editor"])).toBe(false);
    }
  });
});
