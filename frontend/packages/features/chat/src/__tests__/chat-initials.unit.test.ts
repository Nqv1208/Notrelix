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

  it.each([
    ["(Admin) Ada Lovelace", "AL"],
    ["Ada (a) Love (b) lace", "LL"],
    ["Ada (open Lovelace", "(L"],
    ["Ada ) Lovelace", ")L"],
    ["(Admin)", "?"],
    ["Ada()Lovelace", "AL"],
  ])("handles brackets in %j", (input, expected) => {
    expect(initialsOf(input)).toBe(expected);
  });

  it("answers 50k adversarial inputs quickly", () => {
    const started = performance.now();
    for (const input of [
      "(".repeat(50_000),
      `a${"(a".repeat(25_000)}`,
      ")".repeat(50_000),
    ]) {
      initialsOf(input);
    }
    expect(performance.now() - started).toBeLessThan(200);
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
