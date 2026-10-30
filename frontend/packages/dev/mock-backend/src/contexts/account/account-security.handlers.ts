/**
 * Account security on the Identity contracts (`Identity.Security.*`,
 * `Identity.Sessions.*`, `Identity.Mfa.*`):
 *
 *   GET  /auth/security/summary                 summary of password, two-factor, sessions
 *   GET  /auth/sessions                         every sign-in (the page filters by status)
 *   POST /auth/sessions/:sessionId/revoke       200, no body; ending this device signs it out
 *   POST /auth/sessions/revoke-others           200 { revokedCount }; needs a RevokeOtherSessions proof
 *   GET  /auth/security/activity                cursor paging, newest first
 *   POST /auth/mfa/enrollment/start|verify      add the authenticator, recovery codes once; each needs an EnrollMfa proof,
 *                                               and activating ends every other session (`otherSessionsEnded`)
 *   POST /auth/security/step-up/*               proof of presence; throttled with 429
 *   POST /auth/change-password                  current + new password; the proof only with two-factor on
 *   POST /auth/mfa/disable, /auth/mfa/recovery-codes/regenerate   need a single-use proof
 */
import { defineMockOperation } from "../../operations/types";
import { MOCK_AUTH } from "../../state/mock-auth-directory";
import type { SecurityDetails } from "../../state/mock-account-pages";
import type { MockStore } from "../../state/mock-store";
import { notFound, ok } from "../../transport/create-response";
import { identityProblem } from "../identity/auth-flow.handlers";

const PAGE = 20;
/** The producer's `MfaPolicy.RecoveryCodeCount`. */
const RECOVERY_CODE_COUNT = 8;
const MAX_STEP_UP_FAILURES = 5;
const STEP_UP_LOCK_SECONDS = 60;
const PURPOSES = [
  "DisableMfa",
  "RegenerateRecoveryCodes",
  "ChangePassword",
  "RevokeOtherSessions",
  "EnrollMfa",
];

function securityOf(store: MockStore): SecurityDetails {
  return store.accountPages.securityOf(store.getCurrentUser().id, (minutes) =>
    store.getClock().offsetMinutes(minutes),
  );
}

const recoveryCodes = () =>
  Array.from(
    { length: RECOVERY_CODE_COUNT },
    (_, index) =>
      `RC${String(index + 1).padStart(2, "0")}-MOCK-${1000 + index}`,
  );

const throttled = () =>
  identityProblem(
    429,
    "identity.auth.too-many-attempts",
    "Too many attempts. Try again later.",
    STEP_UP_LOCK_SECONDS,
  );

/** The producer's minimum (`PasswordPolicy.MinimumLength`). */
const PASSWORD_MIN_LENGTH = 8;

/** A wrong proof counts; the fifth in a row is throttled, a right one resets. */
function checkProof(
  security: SecurityDetails,
  right: boolean,
  wrongCode:
    | "identity.security.step-up-invalid-code"
    | "identity.security.step-up-invalid-password",
) {
  if (security.stepUpFailures >= MAX_STEP_UP_FAILURES) return throttled();
  if (right) {
    security.stepUpFailures = 0;
    return null;
  }
  security.stepUpFailures += 1;
  if (security.stepUpFailures >= MAX_STEP_UP_FAILURES) return throttled();
  return identityProblem(400, wrongCode, "The proof is not right.");
}

/** Issues a single-use proof; the token never repeats while an earlier one is unspent. */
function issueProof(security: SecurityDetails, purpose: string): string {
  let n = security.proofs.size + 1;
  while (security.proofs.has(`proof-${n}`)) n += 1;
  const token = `proof-${n}`;
  security.proofs.set(token, purpose);
  return token;
}

/** No proof sent: 412, like the producer. */
const proofRequired = () =>
  identityProblem(
    412,
    "identity.security.step-up-required",
    "Strong verification is required for this action.",
  );

/** A proof that is not there, or was issued for another purpose, is invalid (400). */
function invalidProof(
  security: SecurityDetails,
  token: string,
  purpose: string,
) {
  return security.proofs.get(token) === purpose
    ? null
    : identityProblem(
        400,
        "identity.security.step-up-invalid",
        "Step-up verification is invalid or expired.",
      );
}

export const accountSecurityOperations = [
  defineMockOperation({
    id: "account.security.summary",
    contract: { kind: "openapi", operationId: "Identity.Security.GetSummary" },
    method: "GET",
    route: "/auth/security/summary",
    async handle({ store }) {
      const security = securityOf(store);
      return ok({
        serverTime: store.getClock().isoNow(),
        hasPasswordCredential: true,
        passwordChangedAt: security.passwordChangedAt,
        mfa: {
          isEnabled: security.twoFactorEnabled,
          primaryMethod: security.twoFactorEnabled ? "totp" : null,
          hasRecoveryCodes: security.recoveryCodesLeft > 0,
          recoveryCodesRemaining: security.recoveryCodesLeft,
        },
        activeSessionCount: security.sessions.filter(
          (s) => s.status === "Active",
        ).length,
        linkedProviders: [],
      });
    },
  }),

  defineMockOperation({
    id: "account.sessions.list",
    contract: { kind: "openapi", operationId: "Identity.Sessions.List" },
    method: "GET",
    route: "/auth/sessions",
    async handle({ store }) {
      // `SessionStatus` is the enum value: 0 active, 1 revoked, 2 expired.
      const code = { Active: 0, Revoked: 1, Expired: 2 } as const;
      return ok(
        securityOf(store).sessions.map((s) => ({
          ...s,
          status: code[s.status],
        })),
      );
    },
  }),

  defineMockOperation<{ sessionId: string }>({
    id: "account.sessions.revoke",
    contract: { kind: "openapi", operationId: "Identity.Sessions.Revoke" },
    method: "POST",
    route: "/auth/sessions/:sessionId/revoke",
    async handle({ params, store }) {
      const security = securityOf(store);
      const target = security.sessions.find((s) => s.id === params.sessionId);
      if (!target) return notFound();
      // Already ended: success, nothing changes and no second activity line.
      if (target.status !== "Active") return { status: 200 };
      target.status = "Revoked";
      target.revokedAt = store.getClock().isoNow();
      security.activity.unshift({
        id: `activity-${security.activity.length + 1}`,
        type: "SessionRevoked",
        occurredAt: store.getClock().isoNow(),
        ipAddress: target.ipAddress,
        userAgent: target.userAgent,
      });
      // The backend allows ending the current session: this device signs out.
      if (target.isCurrent) store.logoutCurrentUser();
      return { status: 200 };
    },
  }),

  defineMockOperation<Record<string, never>, { stepUpToken?: string }>({
    id: "account.sessions.revokeOthers",
    contract: {
      kind: "openapi",
      operationId: "Identity.Sessions.RevokeOthers",
    },
    method: "POST",
    route: "/auth/sessions/revoke-others",
    async handle({ body, store }) {
      const security = securityOf(store);
      const others = security.sessions.filter(
        (s) => !s.isCurrent && s.status === "Active",
      );
      // Nobody else is signed in: nothing changes and no proof is spent.
      if (others.length === 0) return ok({ revokedCount: 0 });
      if (
        security.proofs.get(body?.stepUpToken ?? "") !== "RevokeOtherSessions"
      )
        return identityProblem(
          412,
          "identity.security.step-up-required",
          "Confirm first.",
        );
      security.proofs.delete(body?.stepUpToken ?? "");
      for (const session of others) {
        session.status = "Revoked";
        session.revokedAt = store.getClock().isoNow();
      }
      security.activity.unshift({
        id: `activity-${security.activity.length + 1}`,
        type: "OtherSessionsRevoked",
        occurredAt: store.getClock().isoNow(),
        ipAddress: null,
        userAgent: null,
      });
      return ok({ revokedCount: others.length });
    },
  }),

  defineMockOperation({
    id: "account.security.activity",
    contract: {
      kind: "openapi",
      operationId: "Identity.Security.ListActivity",
    },
    method: "GET",
    route: "/auth/security/activity",
    async handle({ query, store }) {
      const all = securityOf(store).activity;
      const start = Math.max(0, Number(query.cursor ?? 0) || 0);
      const items = all.slice(start, start + PAGE);
      const next = start + PAGE < all.length ? String(start + PAGE) : null;
      return ok({ items, nextCursor: next, hasNextPage: next !== null });
    },
  }),

  defineMockOperation<Record<string, never>, { stepUpToken?: string }>({
    id: "account.mfa.enrollmentStart",
    contract: { kind: "openapi", operationId: "Identity.Mfa.StartEnrollment" },
    method: "POST",
    route: "/auth/mfa/enrollment/start",
    async handle({ body, store }) {
      const security = securityOf(store);
      const token = body?.stepUpToken ?? "";
      if (token === "") return proofRequired();
      // Refused before the proof is spent, like the producer.
      if (security.twoFactorEnabled)
        return identityProblem(
          409,
          "identity.mfa.already-enabled",
          "MFA is already enabled for this account. Disable it before re-enrolling.",
        );
      const refusal = invalidProof(security, token, "EnrollMfa");
      if (refusal) return refusal;
      security.proofs.delete(token);
      security.pendingEnrollment = "mock-method-1";
      return ok({
        mfaMethodId: security.pendingEnrollment,
        secret: "JBSWY3DPEHPK3PXP",
        otpAuthUri: `otpauth://totp/Notrelix:${store.getCurrentUser().email}?secret=JBSWY3DPEHPK3PXP&issuer=Notrelix`,
      });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { mfaMethodId?: string; code?: string; stepUpToken?: string }
  >({
    id: "account.mfa.enrollmentVerify",
    contract: { kind: "openapi", operationId: "Identity.Mfa.VerifyEnrollment" },
    method: "POST",
    route: "/auth/mfa/enrollment/verify",
    async handle({ body, store }) {
      const security = securityOf(store);
      const token = body?.stepUpToken ?? "";
      if (token === "") return proofRequired();
      if (
        !security.pendingEnrollment ||
        body?.mfaMethodId !== security.pendingEnrollment
      )
        return identityProblem(
          400,
          "identity.mfa.enrollment-not-found",
          "MFA enrollment not found or already completed.",
        );
      // The producer takes exactly six digits; a wrong code leaves the method pending.
      if (body?.code !== MOCK_AUTH.otpCode)
        return identityProblem(
          400,
          "identity.mfa.invalid-verification-code",
          "Invalid verification code.",
        );
      // After the code check, so a mistyped code keeps the proof.
      const refusal = invalidProof(security, token, "EnrollMfa");
      if (refusal) return refusal;
      security.proofs.delete(token);
      security.pendingEnrollment = null;
      security.twoFactorEnabled = true;
      security.recoveryCodesLeft = RECOVERY_CODE_COUNT;
      security.activity.unshift({
        id: `activity-${security.activity.length + 1}`,
        type: "MfaEnabled",
        occurredAt: store.getClock().isoNow(),
        ipAddress: null,
        userAgent: null,
      });
      // Every other session ends; the current one stays.
      const others = security.sessions.filter(
        (s) => !s.isCurrent && s.status === "Active",
      );
      for (const session of others) {
        session.status = "Revoked";
        session.revokedAt = store.getClock().isoNow();
      }
      if (others.length > 0)
        security.activity.unshift({
          id: `activity-${security.activity.length + 1}`,
          type: "OtherSessionsRevoked",
          occurredAt: store.getClock().isoNow(),
          ipAddress: null,
          userAgent: null,
        });
      return ok({
        mfaMethodId: "mock-method-1",
        recoveryCodes: recoveryCodes(),
        otherSessionsEnded: others.length,
      });
    },
  }),

  defineMockOperation<Record<string, never>, { purpose?: string }>({
    id: "account.security.stepUpRequirement",
    contract: {
      kind: "openapi",
      operationId: "Identity.Security.StepUpRequirement",
    },
    method: "POST",
    route: "/auth/security/step-up/requirement",
    async handle({ body, store }) {
      if (!PURPOSES.includes(body?.purpose ?? ""))
        return identityProblem(400, "common.validation", "Unknown purpose.");
      const security = securityOf(store);
      return ok({
        // 0 = MFA challenge, 1 = password.
        requiredFactor: security.twoFactorEnabled ? 0 : 1,
        challengeToken: security.twoFactorEnabled
          ? "mock-step-up-challenge"
          : null,
        expiresAt: store.getClock().offsetMinutes(5),
      });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { purpose?: string; challengeToken?: string; code?: string }
  >({
    id: "account.security.stepUpCompleteMfa",
    contract: {
      kind: "openapi",
      operationId: "Identity.Security.StepUpCompleteMfa",
    },
    method: "POST",
    route: "/auth/security/step-up/complete-mfa",
    async handle({ body, store }) {
      const security = securityOf(store);
      const code = (body?.code ?? "").replace(/[\s-]/g, "").toUpperCase();
      const right =
        body?.challengeToken === "mock-step-up-challenge" &&
        (code === MOCK_AUTH.otpCode ||
          code === MOCK_AUTH.backupCode.replace(/-/g, ""));
      const refusal = checkProof(
        security,
        right,
        "identity.security.step-up-invalid-code",
      );
      if (refusal) return refusal;
      const proofToken = issueProof(security, body?.purpose ?? "");
      return ok({ proofToken, expiresAt: store.getClock().offsetMinutes(5) });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { purpose?: string; password?: string }
  >({
    id: "account.security.stepUpCompletePassword",
    contract: {
      kind: "openapi",
      operationId: "Identity.Security.StepUpCompletePassword",
    },
    method: "POST",
    route: "/auth/security/step-up/complete-password",
    async handle({ body, store }) {
      const security = securityOf(store);
      const refusal = checkProof(
        security,
        body?.password === MOCK_AUTH.personalPassword,
        "identity.security.step-up-invalid-password",
      );
      if (refusal) return refusal;
      const proofToken = issueProof(security, body?.purpose ?? "");
      return ok({ proofToken, expiresAt: store.getClock().offsetMinutes(5) });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { currentPassword?: string; newPassword?: string; stepUpToken?: string }
  >({
    id: "account.auth.changePassword",
    contract: { kind: "openapi", operationId: "Identity.Auth.ChangePassword" },
    method: "POST",
    route: "/auth/change-password",
    async handle({ body, store }) {
      const security = securityOf(store);
      const current = body?.currentPassword ?? "";
      const next = body?.newPassword ?? "";
      const errors: Record<string, string[]> = {};
      if (current === "")
        errors.CurrentPassword = ["Current password is required"];
      if (next === "") errors.NewPassword = ["New password is required"];
      else if (next.length < PASSWORD_MIN_LENGTH)
        errors.NewPassword = [
          `Password must be at least ${PASSWORD_MIN_LENGTH} characters`,
        ];
      else if (next === current)
        errors.NewPassword = [
          "New password must be different from the current password",
        ];
      if (Object.keys(errors).length > 0)
        return {
          status: 400,
          body: {
            type: "https://notrelix.dev/problems/validation.failed",
            title: "Validation failed",
            status: 400,
            detail: "One or more validation errors occurred.",
            errorCode: "validation.failed",
            errors,
          },
        };
      // The current password shares the step-up budget: guesses are throttled alike.
      if (security.stepUpFailures >= MAX_STEP_UP_FAILURES) return throttled();
      if (current !== MOCK_AUTH.personalPassword) {
        security.stepUpFailures += 1;
        if (security.stepUpFailures >= MAX_STEP_UP_FAILURES) return throttled();
        return identityProblem(
          400,
          "identity.auth.invalid-current-password",
          "Current password is incorrect.",
        );
      }
      if (security.twoFactorEnabled) {
        const token = body?.stepUpToken ?? "";
        if (security.proofs.get(token) !== "ChangePassword")
          return identityProblem(
            412,
            "identity.security.step-up-required",
            "Strong verification is required for this action.",
          );
        security.proofs.delete(token);
      }
      security.stepUpFailures = 0;
      const now = store.getClock().isoNow();
      security.passwordChangedAt = now;
      // Every session ends, this one included; the person signs in again.
      for (const session of security.sessions)
        if (session.status === "Active") {
          session.status = "Revoked";
          session.revokedAt = now;
        }
      security.activity.unshift({
        id: `activity-${security.activity.length + 1}`,
        type: "PasswordChanged",
        occurredAt: now,
        ipAddress: null,
        userAgent: null,
      });
      // The mock keeps one fixed password for sign-in; only the session ends.
      store.logoutCurrentUser();
      return { status: 200 };
    },
  }),

  defineMockOperation<Record<string, never>, { stepUpToken?: string }>({
    id: "account.mfa.disable",
    contract: { kind: "openapi", operationId: "Identity.Mfa.Disable" },
    method: "POST",
    route: "/auth/mfa/disable",
    async handle({ body, store }) {
      const security = securityOf(store);
      // An off factor stays off: nothing changes and no session ends.
      if (!security.twoFactorEnabled) return { status: 200 };
      if (security.proofs.get(body?.stepUpToken ?? "") !== "DisableMfa")
        return identityProblem(
          412,
          "identity.step-up.required",
          "Confirm first.",
        );
      security.proofs.delete(body?.stepUpToken ?? "");
      security.twoFactorEnabled = false;
      security.recoveryCodesLeft = 0;
      // Turning it off ends every session, this one included; the person signs in again.
      const now = store.getClock().isoNow();
      for (const session of security.sessions)
        if (session.status === "Active") {
          session.status = "Revoked";
          session.revokedAt = now;
        }
      security.activity.unshift({
        id: `activity-${security.activity.length + 1}`,
        type: "MfaDisabled",
        occurredAt: now,
        ipAddress: null,
        userAgent: null,
      });
      store.logoutCurrentUser();
      return { status: 200 };
    },
  }),

  defineMockOperation<Record<string, never>, { stepUpToken?: string }>({
    id: "account.mfa.regenerateRecoveryCodes",
    contract: {
      kind: "openapi",
      operationId: "Identity.Mfa.RegenerateRecoveryCodes",
    },
    method: "POST",
    route: "/auth/mfa/recovery-codes/regenerate",
    async handle({ body, store }) {
      const security = securityOf(store);
      if (!security.twoFactorEnabled)
        return identityProblem(
          412,
          "identity.mfa.not-enabled",
          "MFA is not enabled for this account.",
        );
      if (
        security.proofs.get(body?.stepUpToken ?? "") !==
        "RegenerateRecoveryCodes"
      )
        return identityProblem(
          412,
          "identity.step-up.required",
          "Confirm first.",
        );
      security.proofs.delete(body?.stepUpToken ?? "");
      security.recoveryCodesLeft = RECOVERY_CODE_COUNT;
      return ok({
        mfaMethodId: "mock-method-1",
        recoveryCodes: recoveryCodes(),
      });
    },
  }),
];
