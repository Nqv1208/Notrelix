import { describe, expect, it } from "vitest";
import {
  PASSWORD_MIN_LENGTH,
  checkNewPassword,
  passwordChangeFailureOf,
} from "../core/password";

describe("checkNewPassword", () => {
  const current = "old-password-1";

  it("accepts a new password of the producer's minimum length that differs and matches", () => {
    const next = "x".repeat(PASSWORD_MIN_LENGTH);
    expect(checkNewPassword({ current, next, confirm: next })).toBeNull();
  });

  it("uses the producer's minimum of 8 characters", () => {
    expect(PASSWORD_MIN_LENGTH).toBe(8);
    expect(
      checkNewPassword({ current, next: "1234567", confirm: "1234567" }),
    ).toBe("too-short");
  });

  it("refuses the current password again and a confirmation that differs", () => {
    expect(checkNewPassword({ current, next: current, confirm: current })).toBe(
      "same-as-current",
    );
    expect(
      checkNewPassword({ current, next: "new-password-2", confirm: "other" }),
    ).toBe("mismatch");
  });
});

describe("passwordChangeFailureOf", () => {
  it("reads a wrong current password from the dotted error code", () => {
    expect(
      passwordChangeFailureOf(
        { status: 400, code: "identity.auth.invalid-current-password" },
        "change",
      ),
    ).toEqual({ kind: "wrong-current" });
  });

  it("tells a wrong second-factor code from a wrong password at the proof", () => {
    expect(
      passwordChangeFailureOf(
        { status: 400, code: "identity.security.step-up-invalid-code" },
        "proof",
      ),
    ).toEqual({ kind: "wrong-code" });
    expect(
      passwordChangeFailureOf(
        { status: 400, code: "identity.security.step-up-invalid-password" },
        "proof",
      ),
    ).toEqual({ kind: "wrong-current" });
  });

  it("reads the seconds to wait from a throttled answer", () => {
    expect(
      passwordChangeFailureOf(
        { status: 429, details: { retryAfterSeconds: 42 } },
        "change",
      ),
    ).toEqual({ kind: "wait", seconds: 42 });
    expect(passwordChangeFailureOf({ status: 429 }, "change")).toEqual({
      kind: "wait",
      seconds: 1,
    });
  });

  it("reads a refused new password from the validation errors of the change", () => {
    expect(
      passwordChangeFailureOf(
        {
          status: 400,
          code: "common.validation-failed",
          validationErrors: { NewPassword: ["too short"] },
        },
        "change",
      ),
    ).toEqual({ kind: "rejected" });
    expect(
      passwordChangeFailureOf(
        { status: 400, validationErrors: { NewPassword: ["x"] } },
        "proof",
      ),
    ).toEqual({ kind: "failed" });
  });

  it("falls back to a generic failure, also for a lost connection", () => {
    expect(passwordChangeFailureOf({ status: 500 }, "change")).toEqual({
      kind: "failed",
    });
    expect(passwordChangeFailureOf(new Error("offline"), "change")).toEqual({
      kind: "failed",
    });
    expect(passwordChangeFailureOf(undefined, "change")).toEqual({
      kind: "failed",
    });
  });
});
