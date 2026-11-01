import { describe, expect, it } from "vitest";
import { MOCK_AUTH, MockAuthDirectory } from "../state/mock-auth-directory";

/** Sign-in of the Auth mockup in the mock world (CTR-GAP-AUTH-FLOW). */
function directory() {
  let now = 1_000_000;
  const auth = new MockAuthDirectory(() => now);
  return {
    auth,
    advance: (seconds: number) => {
      now += seconds * 1000;
    },
  };
}

describe("how an email signs in", () => {
  it("sends a company domain to its provider and everything else to a password", () => {
    const { auth } = directory();
    const company = auth.identify(
      `Thu.Ha@${MOCK_AUTH.ssoDomain.toUpperCase()}`,
    );
    expect(company.method).toBe("sso");
    expect(company.sso?.providerLabel).toBe("Microsoft Entra");
    expect(auth.identify("ai-do@example.com").method).toBe("password");
    // An unknown address is not told apart from a known one.
    expect(auth.identify("khong-co@gmail.com")).toEqual(
      auth.identify(MOCK_AUTH.personalEmail),
    );
  });
});

describe("password sign-in", () => {
  it("counts wrong passwords, throttles on the tenth for 15 minutes and opens again", () => {
    const { auth, advance } = directory();
    const outcomes = Array.from({ length: 10 }, () =>
      auth.login(MOCK_AUTH.personalEmail, "sai"),
    );
    expect(outcomes.slice(0, 9)).toEqual(Array(9).fill({ kind: "invalid" }));
    // No answer says how many tries are left.
    expect(outcomes[9]).toEqual({
      kind: "locked",
      retryAfterSeconds: MOCK_AUTH.lockSeconds,
    });
    // Locked: even the right password waits.
    expect(
      auth.login(MOCK_AUTH.personalEmail, MOCK_AUTH.personalPassword),
    ).toEqual({
      kind: "locked",
      retryAfterSeconds: MOCK_AUTH.lockSeconds,
    });
    advance(MOCK_AUTH.lockSeconds + 1);
    expect(
      auth.login(MOCK_AUTH.personalEmail, MOCK_AUTH.personalPassword).kind,
    ).toBe("otp");
  });

  it("does not tell a wrong email from a wrong password", () => {
    const { auth } = directory();
    expect(auth.login("khong-co@gmail.com", "sai")).toEqual({
      kind: "invalid",
    });
  });
});

describe("second step", () => {
  it("opens with the app's digits or a recovery code, refuses others and kills the challenge after five", () => {
    const { auth } = directory();
    const first = auth.login(
      MOCK_AUTH.personalEmail,
      MOCK_AUTH.personalPassword,
    );
    if (first.kind !== "otp") throw new Error("expected the second step");
    expect(auth.verifyOtp(first.challengeToken, "000000")).toEqual({
      kind: "invalid",
    });
    expect(auth.verifyOtp(first.challengeToken, MOCK_AUTH.otpCode).kind).toBe(
      "signed-in",
    );
    // The challenge is spent.
    expect(auth.verifyOtp(first.challengeToken, MOCK_AUTH.otpCode).kind).toBe(
      "challenge-invalid",
    );
    const second = auth.login(
      MOCK_AUTH.personalEmail,
      MOCK_AUTH.personalPassword,
    );
    if (second.kind !== "otp") throw new Error("expected the second step");
    expect(
      auth.verifyOtp(second.challengeToken, MOCK_AUTH.backupCode).kind,
    ).toBe("signed-in");
    const third = auth.login(
      MOCK_AUTH.personalEmail,
      MOCK_AUTH.personalPassword,
    );
    if (third.kind !== "otp") throw new Error("expected the second step");
    for (let i = 0; i < 4; i += 1)
      auth.verifyOtp(third.challengeToken, "111111");
    expect(auth.verifyOtp(third.challengeToken, "111111").kind).toBe(
      "challenge-invalid",
    );
  });
});

describe("guest invitations", () => {
  it("reads valid, expired and used ones and accepts once", () => {
    const { auth } = directory();
    expect(auth.invite("invite-erp-guest")?.state).toBe("valid");
    expect(auth.invite("invite-erp-expired")?.state).toBe("expired");
    expect(auth.invite("invite-erp-used")?.state).toBe("used");
    expect(auth.invite("nope")).toBeUndefined();
    expect(auth.acceptInvite("invite-erp-guest")).toBe("accepted");
    // A second press is safe and adds nobody twice.
    expect(auth.acceptInvite("invite-erp-guest")).toBe("already");
    expect(auth.invite("invite-erp-guest")?.state).toBe("used");
    expect(auth.acceptInvite("invite-erp-expired")).toBe("gone");
  });

  it("a declined invitation cannot be accepted afterwards", () => {
    const { auth } = directory();
    expect(auth.declineInvite("invite-erp-guest")).toBe(true);
    expect(auth.acceptInvite("invite-erp-guest")).toBe("gone");
    expect(auth.declineInvite("invite-erp-expired")).toBe(false);
  });
});
