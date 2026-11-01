/** The signed-in person (`Identity.Auth.GetCurrentUser`, `UserDto`). */
export interface UserProfile {
  id: string;
  email: string;
  name: string;
  avatarUrl: string | null;
}

export type AccountTheme = "light" | "dark" | "system";

/**
 * The person's own preferences (`Identity.Profile.*Preferences`): locale, time
 * zone, theme and phone. `version` is what the next save quotes as
 * `expectedVersion` (0 before the first save). Date format, sidebar and default
 * view are client-side presentation and not part of this contract.
 */
export interface AccountPreferences {
  readonly locale: string;
  readonly timezone: string;
  readonly theme: AccountTheme;
  /** E.164, or `""` when none. */
  readonly phone: string;
  readonly phoneVisibleToWorkspaceMembers: boolean;
  readonly version: number;
}

/** What a preferences save may change; every field is optional. */
export type AccountPreferencesPatch = Partial<
  Omit<AccountPreferences, "version">
>;

/** The member's job title in one workspace (`Workspaces.Members.*JobTitle`). */
export interface MyMemberProfile {
  /** `""` when none. */
  readonly jobTitle: string;
  readonly version: number;
  /** ISO timestamp the person joined the workspace. */
  readonly joinedAt: string | null;
}

export interface AccountSecuritySummary {
  readonly serverTime: string;
  readonly hasPassword: boolean;
  readonly passwordChangedAt: string | null;
  readonly twoFactorEnabled: boolean;
  readonly recoveryCodesLeft: number;
  readonly activeSessionCount: number;
  /** Sign-in providers linked to the account ("google", "microsoft"). */
  readonly linkedProviders: readonly string[];
}

export type AccountDeviceKind = "desktop" | "phone" | "tablet";

/** One sign-in of the person; the producer has no place and no last-active time. */
export interface AccountSession {
  readonly id: string;
  readonly status: "active" | "revoked" | "expired";
  readonly createdAt: string;
  readonly expiresAt: string;
  readonly isCurrent: boolean;
  /** "Chrome on macOS", read from the user agent. */
  readonly device: string;
  readonly kind: AccountDeviceKind;
  readonly ipAddress: string | null;
}

export type AccountActivityType =
  | "SignInSucceeded"
  | "SignInFailed"
  | "PasswordChanged"
  | "PasswordReset"
  | "MfaEnabled"
  | "MfaDisabled"
  | "SessionRevoked"
  | "OtherSessionsRevoked";

export interface AccountActivityEntry {
  readonly id: string;
  readonly type: AccountActivityType | string;
  readonly occurredAt: string;
  readonly ipAddress: string | null;
  readonly device: string;
}

export interface AccountActivityPage {
  readonly items: readonly AccountActivityEntry[];
  /** Pass back as `cursor` for the next page; `null` = the end. */
  readonly nextCursor: string | null;
}

/** The authenticator the person adds in step one of turning two-factor on. */
export interface TwoFactorEnrollment {
  readonly methodId: string;
  readonly secret: string;
  readonly otpAuthUri: string;
}

/** What proves the person is present before a sensitive change. */
export type StepUpFactor = "mfa" | "password" | "oauth";

export type StepUpPurpose =
  "DisableMfa" | "RegenerateRecoveryCodes" | "ChangePassword";

export interface StepUpRequirement {
  readonly factor: StepUpFactor;
  /** The challenge a second-factor answer quotes. */
  readonly challengeToken: string | null;
}

export type AccountWorkspaceRole = "owner" | "admin" | "member" | "guest";

/**
 * One workspace of the signed-in person (`Workspaces.Members.ListMyWorkspaces`).
 * A count is `null` when the person may not see it (a guest sees no people
 * count, only an owner or admin sees the department count).
 */
export interface AccountWorkspaceEntry {
  readonly id: string;
  readonly name: string;
  readonly role: AccountWorkspaceRole;
  readonly memberCount: number | null;
  readonly departmentCount: number | null;
  readonly isDefault: boolean;
  /** `false` for the last owner, who has to hand the ownership over first. */
  readonly canLeave: boolean;
  /** Read-only for every member; listed only to its owners and admins, who open it from here. */
  readonly isArchived: boolean;
}

/** Why a profile photo was not changed. */
export type AccountPhotoFailure = "unsupported" | "too-large" | "failed";

/** Why leaving a workspace did not happen. */
export type AccountLeaveFailure = "last-owner" | "failed";
