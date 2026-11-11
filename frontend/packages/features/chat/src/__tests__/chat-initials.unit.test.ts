import { describe, expect, it } from "vitest";
import { initialsOf } from "../web/components/chat-initials";

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
