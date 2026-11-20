import { describe, expect, it } from "vitest";
import { looksLikeEmail } from "../looks-like-email";

describe("looksLikeEmail", () => {
  it.each([
    ["a@b.c", true],
    ["first.last@mail.example.com", true],
    ["a@b..c", true],
    ["", false],
    ["a", false],
    ["@b.c", false],
    ["a@.c", false],
    ["a@b.", false],
    ["a@b", false],
    ["a@@b.c", false],
    ["a@b@c.d", false],
    ["a b@c.d", false],
    ["a@b .c", false],
    ["a@b.c\n", false],
  ])("%j -> %s", (input, expected) => {
    expect(looksLikeEmail(input)).toBe(expected);
  });

  it("answers 50k adversarial inputs quickly", () => {
    const started = performance.now();
    for (const input of [
      `a@${".".repeat(50_000)}`,
      `a@${"a.".repeat(25_000)}!`,
      `${"a".repeat(50_000)}@`,
    ]) {
      looksLikeEmail(input);
    }
    expect(performance.now() - started).toBeLessThan(200);
  });
});
