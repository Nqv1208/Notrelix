import type { User } from "../types/auth";
import { isSafeReturnPath } from "../utils/safe-return-path";

/**
 * The calls behind the sign-in pages that go past the plain login: which way
 * an email signs in, the second step, the Google/Microsoft round trip, the
 * guest invitation. The backend answers are read into the pages' models in
 * this one file; the pages never see a wire shape.
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
  /** The browser is sent here (a redirect), never called with XHR. */
  oauthStart: (provider: string) =>
    `/auth/oauth/${encodeURIComponent(provider)}/start`,
  forgotPassword: "/auth/forgot-password",
  invitationPreview: "/invitations/preview",
  invitationDecline: "/invitations/decline",
  invitationAcceptWithRegistration: "/invitations/accept-with-registration",
  invitationAccept: "/invitations/accept",
} as const;

/** `Identity.Auth.IdentifySignIn`: `SignInMethodDto`. */
interface IdentifyWire {
  readonly method?: string | null;
  readonly sso?: {
    readonly orgName?: string | null;
    readonly orgInitials?: string | null;
    readonly provider?: string | null;
    readonly providerLabel?: string | null;
  } | null;
}

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
  /** The invitation carries no name: the guest types their own. */
  readonly name: string | null;
  /** The email already has a Notrelix account. */
  readonly hasAccount: boolean;
}

export interface InviteAcceptResult {
  /** Where the guest lands: the board they were invited to, else the workspace. */
  readonly redirectTo: string;
  /** A second press of the link: no session was issued, the guest signs in. */
  readonly alreadyAccepted: boolean;
  /** The session the registration opened; null when none was. */
  readonly session: SessionResult | null;
}

/** `Workspaces.Invitations.GetInvitationByToken`: `InvitationPreviewDto`. */
interface InvitationPreviewWire {
  readonly state?: string | null;
  readonly inviterName?: string | null;
  readonly workspaceName?: string | null;
  readonly role?: string | null;
  readonly resourceKind?: string | null;
  readonly resourceTitle?: string | null;
  readonly resourceLevel?: string | null;
  readonly expiresOn?: string | null;
  readonly email?: string | null;
  readonly hasAccount?: boolean;
}

/** `AcceptInvitationWithRegistrationResponse`. */
interface AcceptWithRegistrationWire {
  readonly workspaceId?: string;
  readonly resourceKind?: string | null;
  readonly resourceId?: string | null;
  readonly alreadyAccepted?: boolean;
  readonly expiresAt?: string | null;
  readonly user?: AuthSessionWire["user"] & { emailConfirmed?: boolean };
}

/** `AcceptInvitationResultDto`. */
interface AcceptInvitationWire {
  readonly workspaceId?: string;
  readonly resourceKind?: string | null;
  readonly resourceId?: string | null;
}

const BOARD_KIND = "work-management.board";

function initialsOf(name: string): string {
  const letters = name
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .map((word) => Array.from(word)[0]?.toUpperCase() ?? "");
  return letters.length <= 2
    ? letters.join("")
    : `${letters[0]}${letters[letters.length - 1]}`;
}

function levelOf(wire: string | null | undefined): InvitePreview["level"] {
  const level = wire?.toLowerCase();
  return level === "editor" || level === "commenter" ? level : "viewer";
}

/** The one place the invitation preview is read into the page's model. */
export function toInvitePreview(wire: InvitationPreviewWire): InvitePreview {
  const state =
    wire.state === "expired" || wire.state === "used" ? wire.state : "valid";
  const workspaceName = wire.workspaceName ?? "";
  const inviterName = wire.inviterName ?? "";
  return {
    state,
    inviterName,
    inviterInitials: initialsOf(inviterName),
    orgName: workspaceName,
    boardTitle: wire.resourceTitle ?? workspaceName,
    level: levelOf(wire.resourceLevel),
    expiresOn: wire.expiresOn ?? null,
    email: wire.email ?? null,
    name: null,
    hasAccount: wire.hasAccount ?? false,
  };
}

/** The board the guest was invited to, else the workspace the invitation is in. */
function landingOf(wire: {
  workspaceId?: string;
  resourceKind?: string | null;
  resourceId?: string | null;
}): string {
  const workspace = `/workspaces/${encodeURIComponent(wire.workspaceId ?? "")}`;
  return wire.resourceKind === BOARD_KIND && wire.resourceId
    ? `${workspace}/boards/${encodeURIComponent(wire.resourceId)}`
    : workspace;
}

const QUIET: AuthFlowRequestOptions = {
  skipAuthRefresh: true,
  skipGlobalErrorToast: true,
};

export function createAuthFlowService(api: AuthFlowApiClient) {
  return {
    async identify(email: string): Promise<IdentifyResult> {
      const wire = await api.post<IdentifyWire>(
        AUTH_FLOW_PATHS.identify,
        { email },
        QUIET,
      );
      const sso = wire.sso;
      return wire.method === "sso" && sso
        ? {
            method: "sso",
            sso: {
              orgName: sso.orgName ?? "",
              orgInitials: sso.orgInitials ?? "",
              provider: sso.provider ?? "",
              providerLabel: sso.providerLabel ?? "",
            },
          }
        : { method: "password", sso: null };
    },
    async login(data: { email: string; password: string }) {
      return toLoginResult(
        await api.post<AuthSessionWire>(AUTH_FLOW_PATHS.login, data, QUIET),
      );
    },
    /**
     * Answers the second step with the code and the challenge the login gave.
     * After a provider sign-in the challenge is the backend's httpOnly cookie:
     * the page has none, sends the code alone and the API reads the cookie.
     */
    async verifyMfa(data: { challengeToken?: string | null; code: string }) {
      const { challengeToken, code } = data;
      return toLoginResult(
        await api.post<AuthSessionWire>(
          AUTH_FLOW_PATHS.mfaVerify,
          challengeToken ? { challengeToken, code } : { code },
          QUIET,
        ),
      );
    },
    /**
     * Where the browser goes to sign in with Google or Microsoft (path under
     * the API base). The provider's page and the way back are the backend's:
     * it sets the session cookies and redirects to the web `/auth/callback`
     * (with `?returnTo=` when the path is safe), to `/sign-in?step=mfa` when a
     * second step is due, or to `/sign-in?error=<code>`.
     */
    oauthStartPath(provider: string, returnUrl?: string | null): string {
      const path = AUTH_FLOW_PATHS.oauthStart(provider);
      return isSafeReturnPath(returnUrl)
        ? `${path}?returnUrl=${encodeURIComponent(returnUrl)}`
        : path;
    },
    forgotPassword(email: string) {
      return api.post<void>(AUTH_FLOW_PATHS.forgotPassword, { email }, QUIET);
    },
    /** A token the server does not know answers 404. */
    async getInvite(token: string) {
      return toInvitePreview(
        await api.post<InvitationPreviewWire>(
          AUTH_FLOW_PATHS.invitationPreview,
          { token },
          QUIET,
        ),
      );
    },
    /**
     * Accepts without an account: creates it and opens the session. An email
     * that already has one answers 409; the page asks the guest to sign in.
     */
    async acceptInviteWithRegistration(
      token: string,
      data: { name: string; password: string },
    ): Promise<InviteAcceptResult> {
      const wire = await api.post<AcceptWithRegistrationWire>(
        AUTH_FLOW_PATHS.invitationAcceptWithRegistration,
        { token, ...data },
        QUIET,
      );
      return {
        redirectTo: landingOf(wire),
        alreadyAccepted: wire.alreadyAccepted ?? false,
        session: wire.user
          ? { user: toUser(wire.user), expiresAt: wire.expiresAt ?? null }
          : null,
      };
    },
    /** Accepts with the signed-in session; without one the call answers 401. */
    async acceptInvite(token: string): Promise<InviteAcceptResult> {
      const wire = await api.post<AcceptInvitationWire>(
        AUTH_FLOW_PATHS.invitationAccept,
        { token },
        QUIET,
      );
      return {
        redirectTo: landingOf(wire),
        alreadyAccepted: false,
        session: null,
      };
    },
    declineInvite(token: string) {
      return api.post<void>(
        AUTH_FLOW_PATHS.invitationDecline,
        { token },
        QUIET,
      );
    },
  };
}

export type AuthFlowService = ReturnType<typeof createAuthFlowService>;
