/**
 * Account security on the Identity contracts (`Identity.Security.*`,
 * `Identity.Sessions.*`, `Identity.Mfa.*`):
 *
 *   GET  /auth/security/summary                 summary of password, two-factor, sessions
 *   GET  /auth/sessions                         every sign-in (the page filters by status)
 *   POST /auth/sessions/:sessionId/revoke       200, no body; ending this device signs it out
 *   POST /auth/sessions/revoke-others           200, no body
 *   GET  /auth/security/activity                cursor paging, newest first
 *   POST /auth/mfa/enrollment/start|verify      add the authenticator, recovery codes once
 *   POST /auth/security/step-up/*               proof of presence; throttled with 429
 *   POST /auth/mfa/disable, /auth/mfa/recovery-codes/regenerate   need a single-use proof
 */
import { defineMockOperation } from "../../operations/types";
import { MOCK_AUTH } from "../../state/mock-auth-directory";
import type { SecurityDetails } from "../../state/mock-account-pages";
import type { MockStore } from "../../state/mock-store";
import { notFound, ok } from "../../transport/create-response";
import { identityProblem } from "../identity/auth-flow.handlers";

const PAGE = 20;
const MAX_STEP_UP_FAILURES = 5;
const STEP_UP_LOCK_SECONDS = 60;
const PURPOSES = ["DisableMfa", "RegenerateRecoveryCodes", "ChangePassword"];

function securityOf(store: MockStore): SecurityDetails {
  return store.accountPages.securityOf(store.getCurrentUser().id, (minutes) =>
    store.getClock().offsetMinutes(minutes),
  );
}

const recoveryCodes = () =>
  Array.from(
    { length: 10 },
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

/** A wrong proof counts; the fifth in a row is throttled, a right one resets. */
function checkProof(security: SecurityDetails, right: boolean) {
  if (security.stepUpFailures >= MAX_STEP_UP_FAILURES) return throttled();
  if (right) {
    security.stepUpFailures = 0;
    return null;
  }
  security.stepUpFailures += 1;
  if (security.stepUpFailures >= MAX_STEP_UP_FAILURES) return throttled();
  return identityProblem(
    400,
    "identity.mfa.invalid-code",
    "The proof is not right.",
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
      const target = security.sessions.find(
        (s) => s.id === params.sessionId && s.status === "Active",
      );
      if (!target) return notFound();
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

  defineMockOperation({
    id: "account.sessions.revokeOthers",
    contract: {
      kind: "openapi",
      operationId: "Identity.Sessions.RevokeOthers",
    },
    method: "POST",
    route: "/auth/sessions/revoke-others",
    async handle({ store }) {
      const security = securityOf(store);
      for (const session of security.sessions)
        if (!session.isCurrent && session.status === "Active") {
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
      return { status: 200 };
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

  defineMockOperation({
    id: "account.mfa.enrollmentStart",
    contract: { kind: "openapi", operationId: "Identity.Mfa.StartEnrollment" },
    method: "POST",
    route: "/auth/mfa/enrollment/start",
    async handle({ store }) {
      const security = securityOf(store);
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
    { mfaMethodId?: string; code?: string }
  >({
    id: "account.mfa.enrollmentVerify",
    contract: { kind: "openapi", operationId: "Identity.Mfa.VerifyEnrollment" },
    method: "POST",
    route: "/auth/mfa/enrollment/verify",
    async handle({ body, store }) {
      const security = securityOf(store);
      if (
        !security.pendingEnrollment ||
        body?.mfaMethodId !== security.pendingEnrollment
      )
        return identityProblem(
          400,
          "identity.mfa.challenge-invalid",
          "Start the enrollment again.",
        );
      if ((body?.code ?? "").replace(/\s/g, "") !== MOCK_AUTH.otpCode)
        return identityProblem(
          400,
          "identity.mfa.invalid-code",
          "The code is not right.",
        );
      security.pendingEnrollment = null;
      security.twoFactorEnabled = true;
      security.recoveryCodesLeft = 10;
      security.activity.unshift({
        id: `activity-${security.activity.length + 1}`,
        type: "MfaEnabled",
        occurredAt: store.getClock().isoNow(),
        ipAddress: null,
        userAgent: null,
      });
      return ok({
        mfaMethodId: "mock-method-1",
        recoveryCodes: recoveryCodes(),
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
      const refusal = checkProof(security, right);
      if (refusal) return refusal;
      const proofToken = `proof-${security.proofs.size + 1}`;
      security.proofs.set(proofToken, body?.purpose ?? "");
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
      );
      if (refusal) return refusal;
      const proofToken = `proof-${security.proofs.size + 1}`;
      security.proofs.set(proofToken, body?.purpose ?? "");
      return ok({ proofToken, expiresAt: store.getClock().offsetMinutes(5) });
    },
  }),

  defineMockOperation<Record<string, never>, { stepUpToken?: string }>({
    id: "account.mfa.disable",
    contract: { kind: "openapi", operationId: "Identity.Mfa.Disable" },
    method: "POST",
    route: "/auth/mfa/disable",
    async handle({ body, store }) {
      const security = securityOf(store);
      if (security.proofs.get(body?.stepUpToken ?? "") !== "DisableMfa")
        return identityProblem(
          412,
          "identity.step-up.required",
          "Confirm first.",
        );
      security.proofs.delete(body?.stepUpToken ?? "");
      security.twoFactorEnabled = false;
      security.recoveryCodesLeft = 0;
      // Turning it off signs every other device out.
      for (const session of security.sessions)
        if (!session.isCurrent && session.status === "Active")
          session.status = "Revoked";
      security.activity.unshift({
        id: `activity-${security.activity.length + 1}`,
        type: "MfaDisabled",
        occurredAt: store.getClock().isoNow(),
        ipAddress: null,
        userAgent: null,
      });
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
      security.recoveryCodesLeft = 10;
      return ok({
        mfaMethodId: "mock-method-1",
        recoveryCodes: recoveryCodes(),
      });
    },
  }),
];
