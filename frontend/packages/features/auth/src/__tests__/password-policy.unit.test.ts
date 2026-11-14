import { describe, expect, it } from "vitest";
import { PASSWORD_MIN_LENGTH, resetPasswordSchema } from "../core";

const reset = (newPassword: string) =>
  resetPasswordSchema.safeParse({
    email: "minh@example.com",
    code: "123456",
    newPassword,
  });

describe("password minimum", () => {
  it("is the backend policy minimum of 8", () => {
    expect(PASSWORD_MIN_LENGTH).toBe(8);
  });

  it("reset password uses the same minimum", () => {
    expect(reset("x".repeat(PASSWORD_MIN_LENGTH - 1)).success).toBe(false);
    expect(reset("x".repeat(PASSWORD_MIN_LENGTH)).success).toBe(true);
  });
});
