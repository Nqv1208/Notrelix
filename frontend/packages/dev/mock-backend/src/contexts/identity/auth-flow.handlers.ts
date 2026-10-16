/**
 * Identity: what the sign-in pages of the 2026-10 Auth mockup need beyond the
 * stub login (gap `CTR-GAP-AUTH-FLOW`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Auth"):
 *
 *   auth.identify      — POST /auth/identify              (how this email signs in)
 *   auth.otp.verify    — POST /auth/otp/verify            (second step)
 *   auth.sso.start     — POST /auth/sso/start             (company or Google/Microsoft sign-in)
 *   auth.invite.get    — GET  /invites/:token             (guest invitation preview)
 *   auth.invite.accept — POST /invites/:token/accept
 *   auth.invite.decline— POST /invites/:token/decline
 *
 * `POST /auth/login` (identity.handlers.ts) takes the same gap: with an email
 * it checks the account, counts wrong passwords and locks.
 */
import { defineMockOperation } from "../../operations/types";
import { noContent, notFound, ok } from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";

const gap = { kind: "gap", gapId: "CTR-GAP-AUTH-FLOW" } as const;

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const MIN_PASSWORD = 12;

/** The session a successful sign-in hands back, as the login stub does. */
export function sessionOf(store: MockStore) {
  const user = store.getCurrentUser();
  return {
    accessToken: "mock-access-token",
    user: {
      id: user.id,
      email: user.email,
      name: user.name,
      avatarUrl: user.avatarUrl,
    },
  };
}

function problem(
  status: number,
  code: string,
  message: string,
  extra: Record<string, unknown> = {},
) {
  return { status, body: { message, code, ...extra } };
}

export const authFlowOperations = [
  defineMockOperation<Record<string, never>, { email?: string }, unknown>({
    id: "auth.identify",
    contract: gap,
    method: "POST",
    route: "/auth/identify",
    async handle({ body, store }) {
      const email = body?.email?.trim() ?? "";
      if (!EMAIL.test(email))
        return problem(422, "VALIDATION_ERROR", "Validation failed", {
          errors: { email: ["Enter a work email."] },
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
            }
          : null,
      });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { challengeId?: string; code?: string; rememberDevice?: boolean },
    unknown
  >({
    id: "auth.otp.verify",
    contract: gap,
    method: "POST",
    route: "/auth/otp/verify",
    async handle({ body, store }) {
      const outcome = store.auth.verifyOtp(
        body?.challengeId ?? "",
        body?.code ?? "",
      );
      if (outcome.kind === "expired")
        return problem(
          401,
          "OTP_EXPIRED",
          "The verification expired. Sign in again.",
        );
      if (outcome.kind === "invalid")
        return problem(401, "OTP_INVALID", "The code is not right.", {
          attemptsLeft: outcome.attemptsLeft,
        });
      store.loginCurrentUser();
      return ok(sessionOf(store));
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { email?: string; provider?: string },
    unknown
  >({
    id: "auth.sso.start",
    contract: gap,
    method: "POST",
    route: "/auth/sso/start",
    async handle({ store }) {
      // The real call answers with the provider's address to go to; the mock
      // plays the round trip and answers with the signed-in session.
      store.loginCurrentUser();
      return ok({ redirectUrl: null, ...sessionOf(store) });
    },
  }),

  defineMockOperation<{ token: string }, never, unknown>({
    id: "auth.invite.get",
    contract: gap,
    method: "GET",
    route: "/invites/:token",
    async handle({ params, store }) {
      const invite = store.auth.invite(params.token);
      if (!invite) return notFound("Invitation not found");
      return ok({
        state: invite.state,
        inviterName: invite.inviterName,
        inviterInitials: invite.inviterInitials,
        orgName: invite.orgName,
        boardTitle: invite.boardTitle,
        level: invite.level,
        expiresOn: invite.expiresOn,
        // Nothing of the guest's details leaves an invitation that cannot be used.
        email: invite.state === "valid" ? invite.email : null,
        name: invite.state === "valid" ? invite.name : null,
        hasAccount: invite.hasAccount,
      });
    },
  }),

  defineMockOperation<
    { token: string },
    { name?: string; password?: string },
    unknown
  >({
    id: "auth.invite.accept",
    contract: gap,
    method: "POST",
    route: "/invites/:token/accept",
    async handle({ params, body, store }) {
      const invite = store.auth.invite(params.token);
      if (!invite) return notFound("Invitation not found");
      if (invite.state === "expired")
        return problem(410, "INVITE_EXPIRED", "The invitation expired.");
      const errors: Record<string, string[]> = {};
      if (!body?.name?.trim()) errors.name = ["Enter your name."];
      if (!invite.hasAccount && (body?.password?.length ?? 0) < MIN_PASSWORD)
        errors.password = ["At least 12 characters."];
      if (Object.keys(errors).length > 0)
        return problem(422, "VALIDATION_ERROR", "Validation failed", {
          errors,
        });
      const outcome = store.auth.acceptInvite(params.token);
      if (outcome === "gone")
        return problem(410, "INVITE_GONE", "The invitation cannot be used.");
      // A second press is safe: the guest is added once.
      store.loginCurrentUser();
      return ok({
        redirectTo: invite.boardPath,
        alreadyAccepted: outcome === "already",
      });
    },
  }),

  defineMockOperation<{ token: string }, never, unknown>({
    id: "auth.invite.decline",
    contract: gap,
    method: "POST",
    route: "/invites/:token/decline",
    async handle({ params, store }) {
      if (!store.auth.invite(params.token)) return notFound();
      store.auth.declineInvite(params.token);
      return noContent();
    },
  }),
];
