import { describe, expect, it } from "vitest";
import { avatarToneOf, initialsOf } from "../web/components/chat-initials";

describe("initialsOf", () => {
  it("takes the first letter of the last two words", () => {
    expect(initialsOf("Nguyễn Thu Hà")).toBe("TH");
    expect(initialsOf("Taylor Morgan")).toBe("TM");
  });

  it("leaves a role in brackets out of the initials", () => {
    expect(initialsOf("Alex Rivera (Admin)")).toBe("AR");
  });

  it("falls back to a question mark for an empty name", () => {
    expect(initialsOf("   ")).toBe("?");
  });
});

describe("avatarToneOf", () => {
  it("gives the same person the same identity colour every time", () => {
    expect(avatarToneOf("user-42")).toBe(avatarToneOf("user-42"));
  });

  it("picks one of the theme identity colours", () => {
    for (const id of ["a", "user-1", "user-2", "c3000000-0000-4000-8000"]) {
      expect(avatarToneOf(id)).toMatch(
        /^bg-identity-\w+ text-identity-\w+-foreground$/,
      );
    }
  });
});
