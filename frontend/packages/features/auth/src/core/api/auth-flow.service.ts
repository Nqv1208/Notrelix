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
  otpVerify: "/auth/otp/verify",
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

export interface SessionResult {
  readonly accessToken: string;
  readonly user: User;
}

/** A password that opens the account, or asks for the second step first. */
export type LoginResult =
  SessionResult | { readonly requiresOtp: true; readonly challengeId: string };

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
    login(data: { email: string; password: string }) {
      return api.post<LoginResult>(AUTH_FLOW_PATHS.login, data, QUIET);
    },
    verifyOtp(data: {
      challengeId: string;
      code: string;
      rememberDevice: boolean;
    }) {
      return api.post<SessionResult>(AUTH_FLOW_PATHS.otpVerify, data, QUIET);
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
