import type { User } from "../types/auth";

/**
 * The calls behind the sign-in pages that go past the plain login (gap
 * `CTR-GAP-AUTH-FLOW`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Auth"): which way an email signs in, the second step,
 * the company or Google/Microsoft round trip, the guest invitation.
 */
export interface AuthFlowRequestOptions {
  skipAuthRefresh?: boolean;
  skipGlobalErrorToast?: boolean;
}

export interface AuthFlowApiClient {
  get<T>(url: string, options?: AuthFlowRequestOptions): Promise<T>;
  post<T>(
    url: string,
    body?: unknown,
    options?: AuthFlowRequestOptions,
  ): Promise<T>;
}

export const AUTH_FLOW_PATHS = {
  identify: "/auth/identify",
  login: "/auth/login",
  mfaVerify: "/auth/mfa/verify",
  ssoStart: "/auth/sso/start",
  forgotPassword: "/auth/forgot-password",
  invite: (token: string) => `/invites/${encodeURIComponent(token)}`,
} as const;

/** How an email signs in: with a password, or through its company's provider. */
export interface IdentifyResult {
  readonly method: "password" | "sso";
  readonly sso: {
    readonly orgName: string;
    readonly orgInitials: string;
    readonly provider: string;
    /** "Microsoft Entra". */
    readonly providerLabel: string;
  } | null;
}

/**
 * What a signed-in answer carries. The tokens are httpOnly cookies the page
 * never reads, so the result is the user and when the session ends.
 */
export interface SessionResult {
  readonly user: User;
  readonly expiresAt: string | null;
}

/** A password that opens the account, or asks for the second step first. */
export type LoginResult =
  | ({ readonly kind: "session" } & SessionResult)
  | {
      readonly kind: "mfa";
      readonly challengeToken: string;
      /** How the second step is answered ("totp"); the page asks for the app's digits or a recovery code. */
      readonly method: string | null;
      readonly expiresAt: string | null;
    };

/** `Identity.Auth.Login` / `Identity.Mfa.VerifyChallenge`: `AuthSessionResponse`. */
interface AuthSessionWire {
  readonly expiresAt?: string | null;
  readonly user?: {
    readonly id?: string;
    readonly email?: string | null;
    readonly name?: string | null;
    readonly avatarUrl?: string | null;
  } | null;
  readonly mfaRequired?: boolean;
  readonly mfaChallengeToken?: string | null;
  readonly mfaMethod?: string | null;
  readonly mfaExpiresAt?: string | null;
}

function toUser(wire: AuthSessionWire["user"]): User {
  return {
    id: wire?.id ?? "",
    email: wire?.email ?? "",
    name: wire?.name ?? "",
    avatarUrl: wire?.avatarUrl ?? null,
  };
}

/** The one place the session answer is read into the page's model. */
export function toLoginResult(wire: AuthSessionWire): LoginResult {
  if (wire.mfaRequired && wire.mfaChallengeToken)
    return {
      kind: "mfa",
      challengeToken: wire.mfaChallengeToken,
      method: wire.mfaMethod ?? null,
      expiresAt: wire.mfaExpiresAt ?? null,
    };
  return {
    kind: "session",
    user: toUser(wire.user),
    expiresAt: wire.expiresAt ?? null,
  };
}

export type InviteState = "valid" | "expired" | "used";

/** What the guest invitation says before anyone accepts it. */
export interface InvitePreview {
  readonly state: InviteState;
  readonly inviterName: string;
  readonly inviterInitials: string;
  readonly orgName: string;
  readonly boardTitle: string;
  readonly level: "viewer" | "commenter" | "editor";
  /** ISO day the access ends; null = it does not. */
  readonly expiresOn: string | null;
  /** The invited email; withheld when the invitation cannot be used. */
  readonly email: string | null;
  readonly name: string | null;
  /** The email already has a Notrelix account. */
  readonly hasAccount: boolean;
}

export interface InviteAcceptResult {
  /** Where the guest lands: the board they were invited to. */
  readonly redirectTo: string;
  readonly alreadyAccepted: boolean;
}

const QUIET: AuthFlowRequestOptions = {
  skipAuthRefresh: true,
  skipGlobalErrorToast: true,
};

export function createAuthFlowService(api: AuthFlowApiClient) {
  return {
    identify(email: string) {
      return api.post<IdentifyResult>(
        AUTH_FLOW_PATHS.identify,
        { email },
        QUIET,
      );
    },
    async login(data: { email: string; password: string }) {
      return toLoginResult(
        await api.post<AuthSessionWire>(AUTH_FLOW_PATHS.login, data, QUIET),
      );
    },
    /** Answers the second step with the challenge the login gave and the code. */
    async verifyMfa(data: { challengeToken: string; code: string }) {
      return toLoginResult(
        await api.post<AuthSessionWire>(AUTH_FLOW_PATHS.mfaVerify, data, QUIET),
      );
    },
    /** Starts the round trip to Google, Microsoft or the company provider. */
    ssoStart(data: { email?: string; provider: string }) {
      return api.post<SessionResult & { redirectUrl: string | null }>(
        AUTH_FLOW_PATHS.ssoStart,
        data,
        QUIET,
      );
    },
    forgotPassword(email: string) {
      return api.post<void>(AUTH_FLOW_PATHS.forgotPassword, { email }, QUIET);
    },
    getInvite(token: string) {
      return api.get<InvitePreview>(AUTH_FLOW_PATHS.invite(token), QUIET);
    },
    acceptInvite(token: string, data: { name: string; password: string }) {
      return api.post<InviteAcceptResult>(
        `${AUTH_FLOW_PATHS.invite(token)}/accept`,
        data,
        QUIET,
      );
    },
    declineInvite(token: string) {
      return api.post<void>(
        `${AUTH_FLOW_PATHS.invite(token)}/decline`,
        {},
        QUIET,
      );
    },
  };
}

export type AuthFlowService = ReturnType<typeof createAuthFlowService>;
