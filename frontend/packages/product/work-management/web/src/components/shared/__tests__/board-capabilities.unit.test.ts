import { describe, expect, it } from "vitest";
import { boardCapabilities, canManageStatuses } from "../board-capabilities";
import { canDeleteComment } from "../../card-detail/task-detail-types";

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

describe("comment moderation", () => {
  const author = "user-a";
  const viewer = "user-b";

  it("lets a Board manager delete someone else's comment, never edit it", () => {
    const caps = boardCapabilities("manager");
    expect(caps.canModerateComments).toBe(true);
    expect(canDeleteComment(caps, author, viewer)).toBe(true);
  });

  it("gives an editor and a commenter no moderation", () => {
    expect(boardCapabilities("editor").canModerateComments).toBeFalsy();
    expect(canDeleteComment(boardCapabilities("editor"), author, viewer)).toBe(
      false,
    );
    expect(
      canDeleteComment(boardCapabilities("commenter"), author, viewer),
    ).toBe(false);
  });

  it("keeps an archived board's manager read-only", () => {
    expect(
      canDeleteComment(boardCapabilities("manager", true), author, viewer),
    ).toBe(false);
  });

  it("still lets the author delete their own comment", () => {
    expect(
      canDeleteComment(boardCapabilities("commenter"), author, author),
    ).toBe(true);
    expect(canDeleteComment(boardCapabilities("viewer"), author, author)).toBe(
      false,
    );
  });
});
