import { describe, expect, it } from "vitest";
import {
  AUTH_ERROR_KEYS,
  PASSWORD_MIN_LENGTH,
  registerSchema,
  resetPasswordSchema,
} from "../core";

const register = (password: string) =>
  registerSchema.safeParse({
    firstName: "Minh",
    lastName: "Tuan",
    email: "minh@example.com",
    password,
    confirmPassword: password,
  });

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

  it("register refuses one character below the minimum and accepts the minimum", () => {
    const short = register("x".repeat(PASSWORD_MIN_LENGTH - 1));
    expect(short.success).toBe(false);
    expect(
      short.error?.issues.some(
        (issue) => issue.message === AUTH_ERROR_KEYS.PASSWORD_TOO_SHORT,
      ),
    ).toBe(true);
    expect(register("x".repeat(PASSWORD_MIN_LENGTH)).success).toBe(true);
  });

  it("reset password uses the same minimum", () => {
    expect(reset("x".repeat(PASSWORD_MIN_LENGTH - 1)).success).toBe(false);
    expect(reset("x".repeat(PASSWORD_MIN_LENGTH)).success).toBe(true);
  });
});
