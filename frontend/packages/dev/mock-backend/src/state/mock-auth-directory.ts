import { wallClockMs } from "./clock";

/**
 * Who can sign in, and how, in the mock world: the accounts with a password and a second step, the company domains that
 * sign in through their own identity provider, the sign-in attempts and the
 * lock, the guest invitations. Test values for the mock only; no real
 * account is behind any of them.
 */
export const MOCK_AUTH = {
  /** A personal account with a password and an authenticator app. */
  personalEmail: "thuha.nguyen@gmail.com",
  personalName: "Nguyễn Thu Hà",
  personalPassword: "mock-pass-phrase-2026",
  /** The six digits the authenticator app shows for that account. */
  otpCode: "482931",
  /** One of the recovery codes handed out when the second step was set up. */
  backupCode: "BK7Q-M2XN-91TD",
  /** A company whose people sign in through the company identity provider. */
  ssoDomain: "minhan.vn",
  /** Wrong passwords in a row before the account is locked. */
  maxAttempts: 5,
  lockSeconds: 60,
} as const;

export interface MockSsoDirectory {
  readonly orgName: string;
  readonly orgInitials: string;
  readonly provider: "microsoft-entra";
  readonly providerLabel: string;
}

const SSO_DOMAINS: Readonly<Record<string, MockSsoDirectory>> = {
  [MOCK_AUTH.ssoDomain]: {
    orgName: "Minh An Group",
    orgInitials: "MA",
    provider: "microsoft-entra",
    providerLabel: "Microsoft Entra",
  },
};

export type MockInviteState = "valid" | "expired" | "used";

export interface MockGuestInvite {
  readonly token: string;
  readonly state: MockInviteState;
  readonly inviterName: string;
  readonly inviterInitials: string;
  readonly orgName: string;
  readonly boardTitle: string;
  /** Guest access: what they may do and until when (ISO day), "viewer" | "commenter" | "editor". */
  readonly level: "viewer" | "commenter" | "editor";
  /** ISO day the access ends. */
  readonly expiresOn: string;
  readonly email: string;
  /** The name the inviter typed, offered as the starting value. */
  readonly name: string;
  /** The invited email already has an account: the page asks to sign in instead. */
  readonly hasAccount: boolean;
  /** The workspace and board the invitation is for. */
  readonly workspaceId: string;
  readonly boardId: string;
}

const WORKSPACE_ID = "mock-workspace-primary";
const BOARD_ID = "mock-board-erp";

const INVITES: readonly MockGuestInvite[] = [
  {
    token: "invite-erp-guest",
    state: "valid",
    inviterName: "Nguyễn Thu Hà",
    inviterInitials: "HA",
    orgName: "Minh An Group",
    boardTitle: "Triển khai ERP Q4",
    level: "viewer",
    expiresOn: "2027-01-31",
    email: "tuan@doitac-erp.vn",
    name: "Đặng Minh Tuấn",
    hasAccount: false,
    workspaceId: WORKSPACE_ID,
    boardId: BOARD_ID,
  },
  {
    token: "invite-erp-existing",
    state: "valid",
    inviterName: "Lê Hoàng Phúc",
    inviterInitials: "HP",
    orgName: "Minh An Group",
    boardTitle: "Triển khai ERP Q4",
    level: "commenter",
    expiresOn: "2026-12-31",
    email: MOCK_AUTH.personalEmail,
    name: MOCK_AUTH.personalName,
    hasAccount: true,
    workspaceId: WORKSPACE_ID,
    boardId: BOARD_ID,
  },
  {
    token: "invite-erp-expired",
    state: "expired",
    inviterName: "Nguyễn Thu Hà",
    inviterInitials: "HA",
    orgName: "Minh An Group",
    boardTitle: "Triển khai ERP Q4",
    level: "viewer",
    expiresOn: "2026-09-30",
    email: "khach@doitac-erp.vn",
    name: "",
    hasAccount: false,
    workspaceId: WORKSPACE_ID,
    boardId: BOARD_ID,
  },
  {
    token: "invite-erp-used",
    state: "used",
    inviterName: "Nguyễn Thu Hà",
    inviterInitials: "HA",
    orgName: "Minh An Group",
    boardTitle: "Triển khai ERP Q4",
    level: "viewer",
    expiresOn: "2026-12-31",
    email: "da.dung@doitac-erp.vn",
    name: "",
    hasAccount: false,
    workspaceId: WORKSPACE_ID,
    boardId: BOARD_ID,
  },
];

export type MockLoginOutcome =
  | { readonly kind: "signed-in" }
  | { readonly kind: "otp"; readonly challengeToken: string }
  | { readonly kind: "invalid" }
  /** Too many wrong passwords: the attempt is throttled, not verified. */
  | { readonly kind: "locked"; readonly retryAfterSeconds: number };

/** The answer to a second-step code (`identity.mfa.*`); no one is told how many tries are left. */
export type MockOtpOutcome =
  | { readonly kind: "signed-in" }
  | { readonly kind: "invalid" }
  | { readonly kind: "challenge-invalid" };

const OTP_ATTEMPTS = 5;

export class MockAuthDirectory {
  private attempts = new Map<string, number>();
  private lockedUntil = new Map<string, number>();
  private challenges = new Map<string, { email: string; triesLeft: number }>();
  /** The backend's httpOnly MFA challenge cookie: set by a provider sign-in that needs a second step. */
  private mfaChallengeCookie: string | null = null;
  private acceptedInvites = new Set<string>();
  private declinedInvites = new Set<string>();
  private sequence = 0;

  /**
   * The lock runs on the wall clock: the mock's own clock is frozen at the
   * mockups' Monday morning and would never let a lock end.
   */
  constructor(private readonly nowMs: () => number = wallClockMs) {}

  reset(): void {
    this.attempts.clear();
    this.lockedUntil.clear();
    this.challenges.clear();
    this.mfaChallengeCookie = null;
    this.acceptedInvites.clear();
    this.declinedInvites.clear();
    this.sequence = 0;
  }

  /** How this email signs in. A company domain goes to its provider; everything else uses a password. */
  identify(email: string): {
    method: "password" | "sso";
    sso: MockSsoDirectory | null;
  } {
    const domain = email.trim().toLowerCase().split("@")[1] ?? "";
    const sso = SSO_DOMAINS[domain] ?? null;
    return { method: sso ? "sso" : "password", sso };
  }

  private secondsLocked(email: string): number {
    const until = this.lockedUntil.get(email);
    if (until === undefined) return 0;
    const left = Math.ceil((until - this.nowMs()) / 1000);
    if (left > 0) return left;
    this.lockedUntil.delete(email);
    this.attempts.delete(email);
    return 0;
  }

  /** Whether the email and password open the account, counting the wrong ones. */
  login(rawEmail: string, password: string): MockLoginOutcome {
    const email = rawEmail.trim().toLowerCase();
    const locked = this.secondsLocked(email);
    if (locked > 0) return { kind: "locked", retryAfterSeconds: locked };
    if (
      email === MOCK_AUTH.personalEmail &&
      password === MOCK_AUTH.personalPassword
    ) {
      this.attempts.delete(email);
      const challengeToken = `challenge-${(this.sequence += 1)}`;
      this.challenges.set(challengeToken, { email, triesLeft: OTP_ATTEMPTS });
      return { kind: "otp", challengeToken };
    }
    const failed = (this.attempts.get(email) ?? 0) + 1;
    this.attempts.set(email, failed);
    if (failed >= MOCK_AUTH.maxAttempts) {
      this.lockedUntil.set(email, this.nowMs() + MOCK_AUTH.lockSeconds * 1000);
      return { kind: "locked", retryAfterSeconds: MOCK_AUTH.lockSeconds };
    }
    return { kind: "invalid" };
  }

  /**
   * A provider sign-in for the account that has an authenticator app: the backend keeps the
   * challenge in an httpOnly cookie (never in a URL) and sends the browser to the second step.
   */
  startProviderMfa(): void {
    const challengeToken = `challenge-${(this.sequence += 1)}`;
    this.challenges.set(challengeToken, {
      email: MOCK_AUTH.personalEmail,
      triesLeft: OTP_ATTEMPTS,
    });
    this.mfaChallengeCookie = challengeToken;
  }

  /** What the browser would send back in the challenge cookie; null when there is none (expired or cleared). */
  readMfaChallengeCookie(): string | null {
    return this.mfaChallengeCookie;
  }

  clearMfaChallengeCookie(): void {
    this.mfaChallengeCookie = null;
  }

  /** The second step: the app's six digits, or a backup code. */
  verifyOtp(challengeToken: string, code: string): MockOtpOutcome {
    const challenge = this.challenges.get(challengeToken);
    if (!challenge) return { kind: "challenge-invalid" };
    const normalised = code.replace(/[\s-]/g, "").toUpperCase();
    if (
      normalised === MOCK_AUTH.otpCode ||
      normalised === MOCK_AUTH.backupCode.replace(/-/g, "")
    ) {
      this.challenges.delete(challengeToken);
      return { kind: "signed-in" };
    }
    challenge.triesLeft -= 1;
    // After the last wrong code the challenge is dead: sign in again.
    if (challenge.triesLeft <= 0) {
      this.challenges.delete(challengeToken);
      return { kind: "challenge-invalid" };
    }
    return { kind: "invalid" };
  }

  invite(token: string): MockGuestInvite | undefined {
    const found = INVITES.find((entry) => entry.token === token);
    if (!found) return undefined;
    if (this.acceptedInvites.has(token) && found.state === "valid")
      return { ...found, state: "used" };
    return found;
  }

  acceptInvite(token: string): "accepted" | "already" | "gone" {
    const invite = INVITES.find((entry) => entry.token === token);
    if (!invite || invite.state !== "valid") return "gone";
    if (this.declinedInvites.has(token)) return "gone";
    if (this.acceptedInvites.has(token)) return "already";
    this.acceptedInvites.add(token);
    return "accepted";
  }

  /** Declining twice is fine; an accepted, expired or used invitation cannot be declined. */
  declineInvite(token: string): boolean {
    const invite = INVITES.find((entry) => entry.token === token);
    if (!invite || invite.state !== "valid") return false;
    if (this.acceptedInvites.has(token)) return false;
    this.declinedInvites.add(token);
    return true;
  }
}
