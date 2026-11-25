/**
 * Identity and Workspaces: the pre-login calls of the sign-in and invitation
 * pages, on the backend's own routes and shapes:
 *
 *   POST /auth/identify                     how this email signs in
 *   GET  /auth/oauth/:provider/start        Google or GitHub sign-in (the real call is a redirect)
 *   POST /auth/sso/start                    the company sign-in: the provider's authorization address
 *   GET  /auth/sso/callback                 the company provider's way back (the real call is a redirect)
 *   POST /invitations/preview               guest invitation preview
 *   POST /invitations/decline
 *   POST /invitations/accept-with-registration
 *   POST /invitations/accept                the signed-in accept
 *
 * `POST /auth/login` (identity.handlers.ts) takes the same world: with an
 * email it checks the account, counts wrong passwords and locks.
 */
import { defineMockOperation } from "../../operations/types";
import {
  noContent,
  notFound,
  ok,
  unauthorized,
} from "../../transport/create-response";
import { MOCK_AUTH } from "../../state/mock-auth-directory";
import type { MockStore } from "../../state/mock-store";

const EMAIL = /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/;
/** The provider whose mock account has an authenticator app: its sign-in goes through the second step. */
const MFA_PROVIDER = "github";
const MOCK_PROVIDERS = ["google", "github"];

function hasControlCharacter(value: string): boolean {
  for (let index = 0; index < value.length; index += 1) {
    const code = value.charCodeAt(index);
    if (code <= 0x1f || (code >= 0x7f && code <= 0x9f)) return true;
  }
  return false;
}

/**
 * The backend's return-path rule: an internal path with a single leading slash, no backslash,
 * no control character, nothing that decodes into one, at most 512 characters.
 */
function isInternalPath(path: string | undefined): path is string {
  if (!path || path.length > 512) return false;
  let current = path;
  for (let pass = 0; pass < 3; pass += 1) {
    if (current[0] !== "/" || current[1] === "/" || current[1] === "\\")
      return false;
    if (current.includes("\\") || hasControlCharacter(current)) return false;
    let decoded: string;
    try {
      decoded = decodeURIComponent(current);
    } catch {
      return false;
    }
    if (decoded === current) return true;
    current = decoded;
  }
  return false;
}

/** The recovery codes the required enrolment hands out once. */
const RECOVERY_CODES = Array.from(
  { length: 8 },
  (_, index) => `RC${String(index + 1).padStart(2, "0")}-MOCK-${1000 + index}`,
);

/** A token that is unknown, used or out of tries. */
const enrollmentExpired = () =>
  identityProblem(
    400,
    "identity.mfa.challenge-invalid",
    "The set-up expired. Sign in again.",
  );

/** The backend's minimum; the page asks for the same. */
const MIN_PASSWORD = 8;

/**
 * The session a successful sign-in hands back: `AuthSessionResponse`. The
 * tokens are httpOnly cookies and never in the body.
 */
export function sessionOf(store: MockStore) {
  const user = store.getCurrentUser();
  return {
    expiresAt: store.getClock().offsetMinutes(60),
    user: {
      id: user.id,
      email: user.email,
      name: user.name,
      avatarUrl: user.avatarUrl,
      emailConfirmed: true,
    },
    mfaRequired: false,
  };
}

/**
 * A problem+json answer of an Identity operation: the dotted code is the
 * `errorCode`, and a throttled step says when to try again in the body and in
 * `Retry-After`.
 */
export function identityProblem(
  status: number,
  errorCode: string,
  detail: string,
  retryAfterSeconds?: number,
) {
  return {
    status,
    ...(retryAfterSeconds === undefined
      ? {}
      : { headers: { "Retry-After": String(retryAfterSeconds) } }),
    body: {
      type: `https://notrelix.dev/problems/${errorCode}`,
      title: detail,
      status,
      detail,
      errorCode,
      ...(retryAfterSeconds === undefined ? {} : { retryAfterSeconds }),
    },
  };
}

/** A problem+json answer without an Identity code: the producer's generic `errorCode`. */
function genericProblem(
  status: number,
  errorCode: string,
  detail: string,
  errors?: Record<string, string[]>,
) {
  return {
    status,
    body: {
      type: `https://notrelix.dev/problems/${errorCode}`,
      title: detail,
      status,
      detail,
      errorCode,
      ...(errors ? { errors } : {}),
    },
  };
}

/** A refused business rule: the messages sit under `errors._errors`. */
function ruleProblem(detail: string) {
  return genericProblem(400, "business_rule.violation", detail, {
    _errors: [detail],
  });
}

function levelWord(level: string): string {
  return level.charAt(0).toUpperCase() + level.slice(1);
}

export const authFlowOperations = [
  // The real route answers 200 whether or not the address has an account (no disclosure).
  defineMockOperation<Record<string, never>, { email?: string }, unknown>({
    id: "auth.resendEmailVerification",
    contract: {
      kind: "openapi",
      operationId: "Identity.Auth.ResendEmailVerification",
    },
    method: "POST",
    route: "/auth/email-verification/resend",
    async handle({ body }) {
      const email = body?.email?.trim() ?? "";
      if (!EMAIL.test(email))
        return genericProblem(400, "validation.failed", "Validation failed", {
          Email: ["Enter a work email."],
        });
      return ok({ success: true });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { name?: string; email?: string; password?: string },
    unknown
  >({
    id: "auth.register",
    contract: { kind: "openapi", operationId: "Identity.Auth.Register" },
    method: "POST",
    route: "/auth/register",
    async handle({ body }) {
      const email = body?.email?.trim() ?? "";
      const errors: Record<string, string[]> = {};
      if (!EMAIL.test(email)) errors.Email = ["Enter a work email."];
      if (!body?.name?.trim()) errors.Name = ["Enter your name."];
      if ((body?.password ?? "").length < 8)
        errors.Password = ["Password must be at least 8 characters"];
      if (Object.keys(errors).length)
        return genericProblem(
          400,
          "validation.failed",
          "Validation failed",
          errors,
        );
      // Test addresses for the two refusals a form must show.
      if (email === MOCK_AUTH.registerLimitedEmail)
        return genericProblem(429, "rate_limit.exceeded", "Too many requests");
      if (email === MOCK_AUTH.registerDownEmail)
        return genericProblem(503, "server.unavailable", "Unavailable");
      // The same neutral answer for a new and an existing address; no session is opened.
      return ok({});
    },
  }),

  defineMockOperation<Record<string, never>, { email?: string }, unknown>({
    id: "auth.identify",
    contract: { kind: "openapi", operationId: "Identity.Auth.IdentifySignIn" },
    method: "POST",
    route: "/auth/identify",
    async handle({ body, store }) {
      const email = body?.email?.trim() ?? "";
      if (!EMAIL.test(email))
        return genericProblem(400, "validation.failed", "Validation failed", {
          Email: ["Enter a work email."],
        });
      const { method, sso } = store.auth.identify(email);
      return ok({
        method,
        sso: sso
          ? {
              orgName: sso.orgName,
              orgInitials: sso.orgInitials,
              provider: sso.provider,
              providerLabel: sso.providerLabel,
              enforced: sso.enforced,
            }
          : null,
      });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { challengeToken?: string; code?: string },
    unknown
  >({
    id: "identity.mfa.verify",
    contract: { kind: "openapi", operationId: "Identity.Mfa.VerifyChallenge" },
    method: "POST",
    route: "/auth/mfa/verify",
    async handle({ body, store }) {
      // After a provider sign-in the challenge is the cookie; a token in the body wins.
      const fromCookie = !body?.challengeToken?.trim();
      const outcome = store.auth.verifyOtp(
        fromCookie
          ? (store.auth.readMfaChallengeCookie() ?? "")
          : (body?.challengeToken ?? ""),
        body?.code ?? "",
      );
      if (outcome.kind === "signed-in") store.auth.clearMfaChallengeCookie();
      if (outcome.kind === "challenge-invalid") {
        if (fromCookie) store.auth.clearMfaChallengeCookie();
        return identityProblem(
          400,
          "identity.mfa.challenge-invalid",
          "The verification expired. Sign in again.",
        );
      }
      if (outcome.kind === "invalid")
        return identityProblem(
          400,
          "identity.mfa.invalid-code",
          "The code is not right.",
        );
      store.loginCurrentUser();
      return ok(sessionOf(store));
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { enrollmentToken?: string },
    unknown
  >({
    id: "identity.mfa.requiredEnrollment.start",
    contract: {
      kind: "openapi",
      operationId: "Identity.Mfa.StartRequiredEnrollment",
    },
    method: "POST",
    route: "/auth/mfa/required-enrollment/start",
    async handle({ body, store }) {
      const outcome = store.auth.startEnrollment(body?.enrollmentToken);
      if (outcome.kind === "invalid-token") return enrollmentExpired();
      return ok({
        mfaMethodId: outcome.mfaMethodId,
        secret: MOCK_AUTH.enrollSecret,
        otpAuthUri: `otpauth://totp/Notrelix:${MOCK_AUTH.enrollEmail}?secret=${MOCK_AUTH.enrollSecret}&issuer=Notrelix`,
      });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { enrollmentToken?: string; mfaMethodId?: string; code?: string },
    unknown
  >({
    id: "identity.mfa.requiredEnrollment.complete",
    contract: {
      kind: "openapi",
      operationId: "Identity.Mfa.CompleteRequiredEnrollment",
    },
    method: "POST",
    route: "/auth/mfa/required-enrollment/complete",
    async handle({ body, store }) {
      const outcome = store.auth.completeEnrollment(
        body?.enrollmentToken,
        body?.code ?? "",
      );
      if (outcome === "invalid-token") return enrollmentExpired();
      if (outcome === "invalid")
        return identityProblem(
          400,
          "identity.mfa.invalid-code",
          "The code is not right.",
        );
      store.loginCurrentUser();
      const security = store.accountPages.securityOf(
        store.getCurrentUser().id,
        (minutes) => store.getClock().offsetMinutes(minutes),
      );
      security.twoFactorEnabled = true;
      security.recoveryCodesLeft = RECOVERY_CODES.length;
      return ok({ ...sessionOf(store), recoveryCodes: RECOVERY_CODES });
    },
  }),

  defineMockOperation<{ provider: string }, never, unknown>({
    id: "auth.oauth.start",
    contract: { kind: "openapi", operationId: "Identity.Auth.OAuth.Start" },
    method: "GET",
    route: "/auth/oauth/:provider/start",
    async handle({ params, query, store }) {
      // The real call redirects the browser to the provider and back to the
      // backend callback, which answers with a redirect. The mock plays the
      // round trip and says where that redirect goes (`redirectTo`): the
      // session opens and the host lands on `/auth/callback`, or, for the
      // provider whose account has an authenticator app (GitHub), the
      // challenge goes into the mock's cookie and the host lands on the
      // second step. A return path travels only when it is an internal one.
      const returnTo = isInternalPath(query.returnUrl) ? query.returnUrl : null;
      const suffix = returnTo ? `returnTo=${encodeURIComponent(returnTo)}` : "";
      // An unknown or switched-off provider makes no redirect to it: the backend sends the
      // browser back to the sign-in page with the code.
      if (!MOCK_PROVIDERS.includes(params.provider)) {
        return ok({
          redirectTo: "/sign-in?error=identity.oauth.provider-unavailable",
        });
      }
      // A company that requires single sign-on refuses the social sign-in of its people, the
      // Owner too, before any session or second step.
      if (store.auth.sso.isEnforcedFor(store.getCurrentUser().email))
        return ok({ redirectTo: "/sign-in?error=identity.oauth.sso-required" });
      if (params.provider === MFA_PROVIDER) {
        store.auth.startProviderMfa();
        return ok({
          redirectTo: `/sign-in?step=mfa${suffix ? `&${suffix}` : ""}`,
        });
      }
      // The Account requires two-step verification and the Google account has none: the token
      // goes into the cookie and the browser lands on the enrolment step (`enroll=1`).
      if (params.provider === "google" && store.auth.accountMfaRequired) {
        store.auth.startProviderEnrollment();
        return ok({
          redirectTo: `/sign-in?step=mfa&enroll=1${suffix ? `&${suffix}` : ""}`,
        });
      }
      store.loginCurrentUser();
      // The mock's Google account has no password and no second factor: only its provider.
      if (params.provider === "google")
        store.accountPages.becomeProviderOnly(
          store.getCurrentUser().id,
          "Google",
          (minutes) => store.getClock().offsetMinutes(minutes),
        );
      return ok({
        redirectTo: `/auth/callback${suffix ? `?${suffix}` : ""}`,
      });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { email?: string; returnUrl?: string },
    unknown
  >({
    id: "auth.sso.start",
    contract: { kind: "openapi", operationId: "Identity.Auth.Sso.Start" },
    method: "POST",
    route: "/auth/sso/start",
    async handle({ body, store }) {
      const email = body?.email?.trim() ?? "";
      const returnTo = isInternalPath(body?.returnUrl) ? body.returnUrl : null;
      const state = EMAIL.test(email)
        ? store.auth.sso.start(email, returnTo)
        : null;
      // The same answer for an unverified domain, no provider and a provider that is off.
      if (!state)
        return identityProblem(
          404,
          "identity.sso.unavailable",
          "Single sign-on is not available for this address.",
        );
      // The mock's provider has no page: the host plays the callback with this state and code.
      return ok({
        authorizationUrl: `https://mock-idp.invalid/authorize?state=${encodeURIComponent(state)}&code=mock-code`,
      });
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "auth.sso.callback",
    contract: { kind: "openapi", operationId: "Identity.Auth.Sso.Callback" },
    method: "GET",
    route: "/auth/sso/callback",
    async handle({ query, store }) {
      // The real call is a redirect to the web app; the mock says where it would land. Every
      // failure carries one `identity.sso.*` code and no session is opened.
      const fail = (code: string) =>
        ok({ redirectTo: `/sign-in?error=identity.sso.${code}` });
      const sso = store.auth.sso;
      const opened = sso.take(query.state ?? "");
      if (!opened) return fail("state-invalid");
      if (query.error) return fail("denied");
      if (!sso.routeOf(opened.email)) return fail("unavailable");
      // The person to sign in is chosen by the address: its local part picks the outcome.
      const local = opened.email.split("@")[0] ?? "";
      const refusal = {
        "sso-failed": "failed",
        "sso-email-unverified": "email-unverified",
        "sso-domain-mismatch": "domain-mismatch",
        "sso-identity-conflict": "identity-conflict",
        "sso-not-provisioned": "not-provisioned",
      }[local];
      if (refusal) return fail(refusal);
      // With first-sign-in creation off, only people who already have an account get in.
      const known = [
        MOCK_AUTH.ssoOwnerEmail,
        MOCK_AUTH.ssoMemberEmail,
        store.getCurrentUser().email.toLowerCase(),
      ].includes(opened.email);
      if (!known && sso.provider?.jitProvisioning === false)
        return fail("not-provisioned");
      const suffix = opened.returnTo
        ? `returnTo=${encodeURIComponent(opened.returnTo)}`
        : "";
      // The second step and the Account's two-step requirement apply exactly as after the Google sign-in.
      if (local === "sso-mfa") {
        store.auth.startProviderMfa();
        return ok({
          redirectTo: `/sign-in?step=mfa${suffix ? `&${suffix}` : ""}`,
        });
      }
      if (store.auth.accountMfaRequired) {
        store.auth.startProviderEnrollment();
        return ok({
          redirectTo: `/sign-in?step=mfa&enroll=1${suffix ? `&${suffix}` : ""}`,
        });
      }
      store.loginCurrentUser();
      return ok({
        redirectTo: `/auth/callback${suffix ? `?${suffix}` : ""}`,
      });
    },
  }),

  defineMockOperation<Record<string, never>, { token?: string }, unknown>({
    id: "invitations.preview",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Invitations.GetInvitationByToken",
    },
    method: "POST",
    route: "/invitations/preview",
    async handle({ body, store }) {
      const invite = store.auth.invite(body?.token ?? "");
      if (!invite) return notFound("Invitation not found");
      const valid = invite.state === "valid";
      return ok({
        state: invite.state,
        inviterName: invite.inviterName,
        workspaceName: invite.orgName,
        role: invite.workspaceRole === "member" ? "Member" : "Guest",
        // A workspace invitation names no resource.
        resourceKind: invite.workspaceRole ? null : "work-management.board",
        resourceTitle: invite.workspaceRole ? null : invite.boardTitle,
        resourceLevel: invite.workspaceRole ? null : levelWord(invite.level),
        expiresOn: invite.expiresOn,
        // Nothing of the guest's details leaves an invitation that cannot be used.
        email: valid ? invite.email : null,
        hasAccount: valid ? invite.hasAccount : false,
      });
    },
  }),

  defineMockOperation<Record<string, never>, { token?: string }, unknown>({
    id: "invitations.decline",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Invitations.DeclineInvitationByToken",
    },
    method: "POST",
    route: "/invitations/decline",
    async handle({ body, store }) {
      const token = body?.token ?? "";
      if (!store.auth.invite(token)) return notFound();
      if (!store.auth.declineInvite(token))
        return ruleProblem("The invitation cannot be declined any more.");
      return noContent();
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { token?: string; name?: string; password?: string },
    unknown
  >({
    id: "invitations.acceptWithRegistration",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Invitations.AcceptInvitationWithRegistration",
    },
    method: "POST",
    route: "/invitations/accept-with-registration",
    async handle({ body, store }) {
      const token = body?.token ?? "";
      const invite = store.auth.invite(token);
      if (!invite) return notFound("Invitation not found");
      const errors: Record<string, string[]> = {};
      if (!body?.name?.trim()) errors.Name = ["Enter your name."];
      if ((body?.password?.length ?? 0) < MIN_PASSWORD)
        errors.Password = ["At least 8 characters."];
      if (Object.keys(errors).length > 0)
        return genericProblem(400, "validation.failed", "Validation failed", {
          ...errors,
        });
      // The only 409 of this call: the invited address already has an account.
      if (invite.hasAccount)
        return genericProblem(
          409,
          "concurrency.conflict",
          "This email already has an account.",
        );
      const outcome = store.auth.acceptInvite(token);
      if (outcome === "gone")
        return ruleProblem("The invitation cannot be used.");
      const landing = {
        workspaceSlug: invite.workspaceId,
        workspaceId: invite.workspaceId,
        resourceKind: invite.workspaceRole ? null : "work-management.board",
        resourceId: invite.workspaceRole ? null : invite.boardId,
      };
      // A second press is safe: the guest is added once and no session opens.
      if (outcome === "already")
        return ok({
          ...landing,
          alreadyAccepted: true,
          expiresAt: null,
          user: null,
        });
      store.loginCurrentUser();
      const session = sessionOf(store);
      return ok({
        ...landing,
        alreadyAccepted: false,
        expiresAt: session.expiresAt,
        user: {
          id: session.user.id,
          email: invite.email,
          name: body?.name?.trim() ?? "",
          emailConfirmed: true,
        },
      });
    },
  }),

  defineMockOperation<Record<string, never>, { token?: string }, unknown>({
    id: "invitations.accept",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Invitations.AcceptInvitation",
    },
    method: "POST",
    route: "/invitations/accept",
    async handle({ body, store }) {
      if (store.isCurrentUserLoggedOut()) return unauthorized();
      const token = body?.token ?? "";
      const invite = store.auth.invite(token);
      if (!invite) return notFound("Invitation not found");
      if (store.auth.acceptInvite(token) === "gone")
        return ruleProblem("The invitation cannot be used.");
      return ok({
        workspaceSlug: invite.workspaceId,
        workspaceId: invite.workspaceId,
        resourceKind: invite.workspaceRole ? null : "work-management.board",
        resourceId: invite.workspaceRole ? null : invite.boardId,
      });
    },
  }),
];
