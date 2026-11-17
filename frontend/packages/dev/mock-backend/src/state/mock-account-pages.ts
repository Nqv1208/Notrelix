/**
 * What the account pages of the 2026-10 Account mockup need beyond the
 * profile contract: the person's preferences, a member's job title in a workspace,
 * the security facts (single sign-on, two-factor, backup codes,
 * signed-in devices), the notification preferences of a workspace, the default
 * workspace and the profile photo's address. Small per-user state; the
 * handlers compose the store with it.
 */

export const TIME_ZONES = [
  "UTC",
  "Asia/Ho_Chi_Minh",
  "Asia/Tokyo",
  "Europe/Paris",
] as const;

/**
 * `Identity.Profile.*Preferences`: the person's own locale, time zone, theme
 * and phone. `version` 0 means never saved (the values are the defaults).
 */
export interface AccountPreferences {
  locale: string;
  timezone: string;
  theme: "light" | "dark" | "system";
  phone: string | null;
  phoneVisibleToWorkspaceMembers: boolean;
  colourBlindLabels: boolean;
  version: number;
}

/** A member's profile in one workspace (`Workspaces.Members.*JobTitle`). */
export interface MemberProfile {
  jobTitle: string | null;
  version: number;
}

/** One sign-in as `Identity.Sessions.List` answers it (no place, no last-active time). */
export interface MockSession {
  id: string;
  status: "Active" | "Revoked" | "Expired";
  createdAt: string;
  expiresAt: string;
  revokedAt: string | null;
  ipAddress: string;
  userAgent: string;
  isCurrent: boolean;
}

export interface MockActivity {
  id: string;
  type: string;
  occurredAt: string;
  ipAddress: string | null;
  userAgent: string | null;
}

export interface SecurityDetails {
  twoFactorEnabled: boolean;
  recoveryCodesLeft: number;
  passwordChangedAt: string | null;
  sessions: MockSession[];
  activity: MockActivity[];
  /** Wrong step-up proofs in a row; the throttle answers 429 from the fifth. */
  stepUpFailures: number;
  /** The authenticator being added, until its first code verifies. */
  pendingEnrollment: string | null;
  /** Single-use proofs handed out by a completed step-up, with their purpose. */
  proofs: Map<string, string>;
  /** The provider a person with no password signs in with (`null` = has a password). */
  signInProvider: string | null;
  /** Every provider linked to the account ("Google", "Microsoft"). */
  linkedProviders: string[];
  /** The proof(s) a provider re-authentication left in the (mock) httpOnly cookie, until claimed. */
  providerProofs: string[] | null;
}

/** `Collaboration.Preferences`: the types of the matrix and the two channels delivered. */
export const NOTIFICATION_EVENTS = [
  "assigned",
  "mention",
  "approval",
  "overdue",
  "watching",
  "invitation",
  "digest",
] as const;
export type NotificationEvent = (typeof NOTIFICATION_EVENTS)[number];

export interface ChannelChoice {
  inApp: boolean;
  email: boolean;
}

/** One member's choices in one workspace, with the version a save must quote. */
export interface NotificationPreferenceSet {
  version: number;
  rows: Record<NotificationEvent, ChannelChoice>;
}

/** The value of a cell nobody changed: in-app on everywhere, email on for four types. */
const defaultNotificationSet = (): NotificationPreferenceSet => ({
  version: 0,
  rows: {
    assigned: { inApp: true, email: true },
    mention: { inApp: true, email: true },
    approval: { inApp: true, email: true },
    overdue: { inApp: true, email: false },
    watching: { inApp: true, email: false },
    invitation: { inApp: true, email: true },
    digest: { inApp: true, email: false },
  },
});

const defaultPreferences = (): AccountPreferences => ({
  locale: "vi",
  timezone: "Asia/Ho_Chi_Minh",
  theme: "system",
  phone: null,
  phoneVisibleToWorkspaceMembers: false,
  colourBlindLabels: false,
  version: 0,
});

export const CURRENT_SESSION_ID = "session-macbook";

const MAC_UA =
  "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
const IPHONE_UA =
  "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1";
const IPAD_UA =
  "Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1";

/** The devices signed in and the person's recent security history; times are minutes before the mock clock's now. */
const defaultSecurity = (offsetMinutes: (m: number) => string) => {
  const day = 24 * 60;
  const kinds = [
    "SignInSucceeded",
    "SignInSucceeded",
    "SignInFailed",
    "SessionRevoked",
    "PasswordChanged",
  ];
  const session = (
    id: string,
    userAgent: string,
    ago: number,
    isCurrent = false,
  ): MockSession => ({
    id,
    status: "Active",
    createdAt: offsetMinutes(-ago),
    expiresAt: offsetMinutes(30 * day - ago),
    revokedAt: null,
    ipAddress: "113.190.10.4",
    userAgent,
    isCurrent,
  });
  return {
    twoFactorEnabled: true,
    recoveryCodesLeft: 8,
    passwordChangedAt: offsetMinutes(-40 * day),
    sessions: [
      session(CURRENT_SESSION_ID, MAC_UA, 60, true),
      session("session-iphone", IPHONE_UA, 120),
      session("session-ipad", IPAD_UA, 3 * day),
    ],
    // 25 lines so the feed pages (20 per page).
    activity: Array.from({ length: 25 }, (_, index) => ({
      id: `activity-${index + 1}`,
      type: kinds[index % kinds.length] ?? "SignInSucceeded",
      occurredAt: offsetMinutes(-(index + 1) * 90),
      ipAddress: "113.190.10.4",
      userAgent: index % 2 === 0 ? MAC_UA : IPHONE_UA,
    })),
    stepUpFailures: 0,
    pendingEnrollment: null,
    proofs: new Map<string, string>(),
    signInProvider: null,
    linkedProviders: [],
    providerProofs: null,
  } satisfies SecurityDetails;
};

/** One workspace API token as `Identity.ApiTokens.List` answers it (the secret is never kept). */
export interface MockApiToken {
  id: string;
  name: string;
  status: "Active" | "Revoked" | "Expired";
  lastUsedAt: string | null;
  expiresAt: string;
  createdAt: string;
  revokedAt: string | null;
  suffix: string;
}

/** Three tokens a workspace already holds, newest first; times are minutes from the mock clock's now. */
const defaultApiTokens = (
  offsetMinutes: (m: number) => string,
): MockApiToken[] => {
  const day = 24 * 60;
  const token = (
    n: number,
    name: string,
    suffix: string,
    usedAgo: number | null,
    left: number,
  ): MockApiToken => ({
    id: `00000000-0000-4000-8000-00000000000${n}`,
    name,
    status: "Active",
    lastUsedAt: usedAgo === null ? null : offsetMinutes(-usedAgo),
    expiresAt: offsetMinutes(left * day),
    createdAt: offsetMinutes(-n * day),
    revokedAt: null,
    suffix,
  });
  return [
    token(1, "CI triển khai ERP", "a41f", 120, 90),
    token(2, "Báo cáo BI hằng tuần", "9c07", day, 30),
    token(3, "Script dọn dẹp thẻ", "33be", null, 90),
  ];
};

export class MockAccountPages {
  /** The persona the mockups draw: its job title is seeded. */
  static readonly SEED_USER = "mock-user-owner";

  private preferences = new Map<string, AccountPreferences>();
  private memberProfiles = new Map<string, MemberProfile>();
  private security = new Map<string, SecurityDetails>();
  private notificationSets = new Map<string, NotificationPreferenceSet>();
  private defaultWorkspace = new Map<string, string>();
  private avatarVersions = new Map<string, number>();
  private apiTokens = new Map<string, MockApiToken[]>();

  reset(): void {
    this.apiTokens.clear();
    this.preferences.clear();
    this.memberProfiles.clear();
    this.security.clear();
    this.notificationSets.clear();
    this.defaultWorkspace.clear();
    this.avatarVersions.clear();
  }

  preferencesOf(userId: string): AccountPreferences {
    let value = this.preferences.get(userId);
    if (!value) {
      value = defaultPreferences();
      this.preferences.set(userId, value);
    }
    return value;
  }

  savePreferences(userId: string, next: AccountPreferences): void {
    this.preferences.set(userId, next);
  }

  /** The job title and version of one member of one workspace. */
  memberProfileOf(workspaceId: string, userId: string): MemberProfile {
    const key = `${workspaceId}|${userId}`;
    let value = this.memberProfiles.get(key);
    if (!value) {
      value = {
        jobTitle:
          userId === MockAccountPages.SEED_USER
            ? "Trưởng nhóm triển khai"
            : null,
        version: 0,
      };
      this.memberProfiles.set(key, value);
    }
    return value;
  }

  /** The API tokens of one workspace, newest first. */
  apiTokensOf(
    workspaceId: string,
    offsetMinutes: (m: number) => string,
  ): MockApiToken[] {
    let value = this.apiTokens.get(workspaceId);
    if (!value) {
      // The primary workspace already holds some; any other starts empty.
      value =
        workspaceId === "mock-workspace-primary"
          ? defaultApiTokens(offsetMinutes)
          : [];
      this.apiTokens.set(workspaceId, value);
    }
    return value;
  }

  securityOf(
    userId: string,
    offsetMinutes: (m: number) => string,
  ): SecurityDetails {
    let value = this.security.get(userId);
    if (!value) {
      value = defaultSecurity(offsetMinutes);
      this.security.set(userId, value);
    }
    return value;
  }

  /** A person's choices: one matrix for the account, read in every workspace. */
  notificationsOf(userId: string): NotificationPreferenceSet {
    let value = this.notificationSets.get(userId);
    if (!value) {
      value = defaultNotificationSet();
      this.notificationSets.set(userId, value);
    }
    return value;
  }

  defaultWorkspaceOf(userId: string): string | null {
    return this.defaultWorkspace.get(userId) ?? null;
  }

  setDefaultWorkspace(userId: string, workspaceId: string): void {
    this.defaultWorkspace.set(userId, workspaceId);
  }

  /** A leave clears the default when it was that workspace. */
  forgetDefaultWorkspace(userId: string, workspaceId: string): void {
    if (this.defaultWorkspace.get(userId) === workspaceId)
      this.defaultWorkspace.delete(userId);
  }

  /** The mock's Google account: no password and no second factor, only the provider they sign in with. */
  becomeProviderOnly(
    userId: string,
    provider: string,
    offsetMinutes: (m: number) => string,
  ): void {
    const security = this.securityOf(userId, offsetMinutes);
    security.signInProvider = provider;
    security.linkedProviders = [provider];
    security.twoFactorEnabled = false;
    security.recoveryCodesLeft = 0;
  }

  /** The address of the person's new photo; a new upload changes `v`. */
  nextAvatarUrl(userId: string): string {
    const version = (this.avatarVersions.get(userId) ?? 0) + 1;
    this.avatarVersions.set(userId, version);
    return `/api/v1/users/${userId}/avatar?v=photo-${version}`;
  }
}
