import { resolveApiAssetUrl } from "@notrelix/contracts";
import type {
  AccountLeaveFailure,
  AccountPhotoFailure,
  AccountWorkspaceEntry,
  AccountWorkspaceRole,
  AccountActivityEntry,
  AccountActivityPage,
  AccountDeviceKind,
  AccountPreferences,
  AccountPreferencesPatch,
  AccountSecuritySummary,
  AccountSession,
  AccountTheme,
  MyMemberProfile,
  StepUpFactor,
  StepUpPurpose,
  StepUpRequirement,
  TwoFactorEnrollment,
  UserProfile,
} from "../types/account";

export interface AccountApiClient {
  get<T>(url: string, options?: { skipGlobalErrorToast?: boolean }): Promise<T>;
  post<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean; skipAuthRefresh?: boolean },
  ): Promise<T>;
  put<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
  patch<T>(
    url: string,
    body: unknown,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
  delete<T>(
    url: string,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
  /** Multipart upload; `PUT` replaces a resource. */
  upload<T>(
    url: string,
    form: FormData,
    options: { method?: "POST" | "PUT"; idempotencyKey?: string },
  ): Promise<T>;
}

export interface AccountEndpoints {
  auth: {
    profile: string;
    securitySummary: string;
    securityActivity: string;
    stepUpRequirement: string;
    stepUpCompleteMfa: string;
    stepUpCompletePassword: string;
    sessions: string;
    sessionRevoke: (sessionId: string) => string;
    sessionsRevokeOthers: string;
    mfaEnrollmentStart: string;
    mfaEnrollmentVerify: string;
    mfaDisable: string;
    mfaRecoveryCodes: string;
    changePassword: string;
  };
  users: {
    updateProfile: string;
    preferences: string;
    avatar: string;
  };
  account: {
    workspaces: string;
    defaultWorkspace: string;
    leaveWorkspace: (workspaceId: string) => string;
  };
  workspaces: {
    myJobTitle: (workspaceId: string) => string;
    myMembership: (workspaceId: string) => string;
  };
}

/** Quiet: the account pages say what went wrong themselves, and a refused step-up is not a lost session. */
/** Quiet but session-refreshing: the pages report a refused call themselves. */
const PAGE_QUIET = { skipGlobalErrorToast: true } as const;

const QUIET = { skipGlobalErrorToast: true, skipAuthRefresh: true } as const;

// ─── The one mapping owner: backend shapes into the pages' models ──────────

interface UserWire {
  readonly id?: string;
  readonly email?: string | null;
  readonly name?: string | null;
  readonly avatarUrl?: string | null;
}

export function toUserProfile(wire: UserWire): UserProfile {
  return {
    id: wire.id ?? "",
    email: wire.email ?? "",
    name: wire.name ?? "",
    avatarUrl: resolveApiAssetUrl(wire.avatarUrl),
  };
}

interface WorkspaceEntryWire {
  readonly id?: string;
  readonly name?: string | null;
  readonly role?: string | null;
  readonly memberCount?: number | null;
  readonly departmentCount?: number | null;
  readonly isDefault?: boolean;
  readonly canLeave?: boolean;
}

const WORKSPACE_ROLES: readonly AccountWorkspaceRole[] = [
  "owner",
  "admin",
  "member",
  "guest",
];

/** The one place the producer's capitalised role and nullable counts become the page's model. */
export function toWorkspaceEntry(
  wire: WorkspaceEntryWire,
): AccountWorkspaceEntry {
  const role = (wire.role ?? "").toLowerCase();
  return {
    id: wire.id ?? "",
    name: wire.name ?? "",
    role: WORKSPACE_ROLES.find((known) => known === role) ?? "member",
    memberCount: wire.memberCount ?? null,
    departmentCount: wire.departmentCount ?? null,
    isDefault: wire.isDefault ?? false,
    canLeave: wire.canLeave ?? false,
  };
}

/** The error code of a refused call (`errorCode` of the problem answer), when there is one. */
function errorCodeOf(error: unknown): string | null {
  const code = (error as { code?: unknown } | null)?.code;
  return typeof code === "string" ? code : null;
}

/** A photo the page refuses before sending: what the producer accepts, with a safety margin on the size. */
export const AVATAR_MAX_BYTES = 1_500_000;
const AVATAR_TYPES = ["image/png", "image/jpeg", "image/webp"];

export function checkAvatarFile(file: {
  readonly type: string;
  readonly size: number;
}): Extract<AccountPhotoFailure, "unsupported" | "too-large"> | null {
  if (!AVATAR_TYPES.includes(file.type)) return "unsupported";
  if (file.size > AVATAR_MAX_BYTES) return "too-large";
  return null;
}

export function photoFailureOf(error: unknown): AccountPhotoFailure {
  const code = errorCodeOf(error);
  if (code === "identity.avatar.too-large") return "too-large";
  if (code === "identity.avatar.unsupported-format") return "unsupported";
  return "failed";
}

export function leaveFailureOf(error: unknown): AccountLeaveFailure {
  return errorCodeOf(error) === "workspaces.member.last-owner-cannot-leave"
    ? "last-owner"
    : "failed";
}

interface PreferencesWire {
  readonly locale?: string | null;
  readonly timezone?: string | null;
  readonly theme?: string | null;
  readonly phone?: string | null;
  readonly phoneVisibleToWorkspaceMembers?: boolean;
  readonly colourBlindLabels?: boolean;
  readonly version?: number;
}

const THEMES: readonly AccountTheme[] = ["light", "dark", "system"];

export function toPreferences(wire: PreferencesWire): AccountPreferences {
  const theme = THEMES.find((candidate) => candidate === wire.theme);
  return {
    locale: wire.locale ?? "vi",
    timezone: wire.timezone ?? "UTC",
    theme: theme ?? "system",
    phone: wire.phone ?? "",
    phoneVisibleToWorkspaceMembers:
      wire.phoneVisibleToWorkspaceMembers ?? false,
    colourBlindLabels: wire.colourBlindLabels ?? false,
    version: wire.version ?? 0,
  };
}

interface SummaryWire {
  readonly serverTime?: string;
  readonly hasPasswordCredential?: boolean;
  readonly passwordChangedAt?: string | null;
  readonly mfa?: {
    readonly isEnabled?: boolean;
    readonly recoveryCodesRemaining?: number;
  } | null;
  readonly activeSessionCount?: number;
  readonly linkedProviders?: readonly string[] | null;
}

export function toSecuritySummary(wire: SummaryWire): AccountSecuritySummary {
  return {
    serverTime: wire.serverTime ?? new Date().toISOString(),
    hasPassword: wire.hasPasswordCredential ?? false,
    passwordChangedAt: wire.passwordChangedAt ?? null,
    twoFactorEnabled: wire.mfa?.isEnabled ?? false,
    recoveryCodesLeft: wire.mfa?.recoveryCodesRemaining ?? 0,
    activeSessionCount: wire.activeSessionCount ?? 0,
    linkedProviders: wire.linkedProviders ?? [],
  };
}

/** The label and kind of a device from its user agent: the producer sends no device name. */
export function describeDevice(userAgent: string | null | undefined): {
  device: string;
  kind: AccountDeviceKind;
} {
  const ua = userAgent ?? "";
  const kind: AccountDeviceKind = /iPad|Tablet/i.test(ua)
    ? "tablet"
    : /iPhone|Mobile/i.test(ua)
      ? "phone"
      : "desktop";
  const browser = /Edg\//.test(ua)
    ? "Edge"
    : /Firefox\//.test(ua)
      ? "Firefox"
      : /Chrome\//.test(ua)
        ? "Chrome"
        : /Safari\//.test(ua)
          ? "Safari"
          : null;
  const system = /Windows/.test(ua)
    ? "Windows"
    : /iPad/.test(ua)
      ? "iPad"
      : /iPhone|iOS/.test(ua)
        ? "iPhone"
        : /Mac OS X|Macintosh/.test(ua)
          ? "macOS"
          : /Android/.test(ua)
            ? "Android"
            : /Linux/.test(ua)
              ? "Linux"
              : null;
  const device =
    browser && system ? `${browser} · ${system}` : (browser ?? system ?? "");
  return { device, kind };
}

interface SessionWire {
  readonly id?: string;
  readonly status?: number | string;
  readonly createdAt?: string;
  readonly expiresAt?: string;
  readonly ipAddress?: string | null;
  readonly userAgent?: string | null;
  readonly isCurrent?: boolean;
}

/** `SessionStatus` is the enum value (0 active, 1 revoked, 2 expired) or its name. */
function sessionStatus(value: SessionWire["status"]): AccountSession["status"] {
  if (value === 1 || value === "Revoked") return "revoked";
  if (value === 2 || value === "Expired") return "expired";
  return "active";
}

export function toSession(wire: SessionWire): AccountSession {
  return {
    id: wire.id ?? "",
    status: sessionStatus(wire.status),
    createdAt: wire.createdAt ?? "",
    expiresAt: wire.expiresAt ?? "",
    isCurrent: wire.isCurrent ?? false,
    ipAddress: wire.ipAddress ?? null,
    ...describeDevice(wire.userAgent),
  };
}

interface ActivityPageWire {
  readonly items?:
    | readonly {
        readonly id?: string;
        readonly type?: string;
        readonly occurredAt?: string;
        readonly ipAddress?: string | null;
        readonly userAgent?: string | null;
      }[]
    | null;
  readonly nextCursor?: string | null;
}

export function toActivityPage(wire: ActivityPageWire): AccountActivityPage {
  const items: AccountActivityEntry[] = (wire.items ?? []).map((item) => ({
    id: item.id ?? "",
    type: item.type ?? "",
    occurredAt: item.occurredAt ?? "",
    ipAddress: item.ipAddress ?? null,
    device: describeDevice(item.userAgent).device,
  }));
  return { items, nextCursor: wire.nextCursor ?? null };
}

const FACTORS: readonly StepUpFactor[] = ["mfa", "password", "oauth"];

export function toStepUpRequirement(wire: {
  readonly requiredFactor?: number;
  readonly challengeToken?: string | null;
}): StepUpRequirement {
  return {
    factor: FACTORS[wire.requiredFactor ?? 1] ?? "password",
    challengeToken: wire.challengeToken ?? null,
  };
}

export function createAccountService(
  api: AccountApiClient,
  endpoints: AccountEndpoints,
) {
  return {
    async getProfile(): Promise<UserProfile> {
      return toUserProfile(await api.get<UserWire>(endpoints.auth.profile));
    },

    async updateProfile(profile: { name?: string }): Promise<void> {
      await api.patch<unknown>(endpoints.users.updateProfile, profile);
    },

    async getPreferences(): Promise<AccountPreferences> {
      return toPreferences(
        await api.get<PreferencesWire>(endpoints.users.preferences),
      );
    },

    /** The save quotes the version it was read at; a stale one answers 412. */
    async updatePreferences(
      patch: AccountPreferencesPatch,
      expectedVersion: number,
    ): Promise<AccountPreferences> {
      return toPreferences(
        await api.patch<PreferencesWire>(endpoints.users.preferences, {
          ...patch,
          expectedVersion,
        }),
      );
    },

    /** The signed-in member's own row: job title, its version and when they joined. */
    /** Only the person's own active memberships, ordered by name. */
    async listMyWorkspaces(): Promise<AccountWorkspaceEntry[]> {
      const wire = await api.get<WorkspaceEntryWire[]>(
        endpoints.account.workspaces,
        PAGE_QUIET,
      );
      return wire.map(toWorkspaceEntry);
    },

    /** Answers 204; a workspace the person does not belong to answers 404. */
    async setDefaultWorkspace(workspaceId: string): Promise<void> {
      await api.put<void>(
        endpoints.account.defaultWorkspace,
        { workspaceId },
        PAGE_QUIET,
      );
    },

    /** Answers 204; the last owner is refused with `workspaces.member.last-owner-cannot-leave`. */
    async leaveWorkspace(workspaceId: string): Promise<void> {
      await api.delete<void>(
        endpoints.account.leaveWorkspace(workspaceId),
        PAGE_QUIET,
      );
    },

    /** Sends the file itself (multipart part `file`); answers the new photo address. */
    async uploadAvatar(file: File): Promise<string | null> {
      const form = new FormData();
      form.append("file", file, file.name);
      const wire = await api.upload<{ avatarUrl?: string | null }>(
        endpoints.users.avatar,
        form,
        { method: "PUT" },
      );
      return resolveApiAssetUrl(wire.avatarUrl);
    },

    async removeAvatar(): Promise<void> {
      await api.delete<unknown>(endpoints.users.avatar, PAGE_QUIET);
    },

    async getMyMemberProfile(workspaceId: string): Promise<MyMemberProfile> {
      const wire = await api.get<{
        jobTitle?: string | null;
        version?: number;
        joinedAt?: string | null;
      }>(endpoints.workspaces.myMembership(workspaceId));
      return {
        jobTitle: wire.jobTitle ?? "",
        version: wire.version ?? 0,
        joinedAt: wire.joinedAt ?? null,
      };
    },

    async updateMyJobTitle(
      workspaceId: string,
      jobTitle: string,
      expectedVersion: number,
    ): Promise<void> {
      await api.put<unknown>(endpoints.workspaces.myJobTitle(workspaceId), {
        jobTitle,
        expectedVersion,
      });
    },

    async getSecuritySummary(): Promise<AccountSecuritySummary> {
      return toSecuritySummary(
        await api.get<SummaryWire>(endpoints.auth.securitySummary, QUIET),
      );
    },

    /** Every sign-in; the page filters by status. */
    async listSessions(): Promise<AccountSession[]> {
      const wire = await api.get<SessionWire[]>(endpoints.auth.sessions, QUIET);
      return wire.map(toSession);
    },

    async listActivity(cursor?: string | null): Promise<AccountActivityPage> {
      const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : "";
      return toActivityPage(
        await api.get<ActivityPageWire>(
          `${endpoints.auth.securityActivity}${query}`,
          QUIET,
        ),
      );
    },

    /** Answers 200 with no body; revoking the current session signs this device out. */
    async revokeSession(sessionId: string): Promise<void> {
      await api.post<void>(endpoints.auth.sessionRevoke(sessionId), {}, QUIET);
    },

    async revokeOtherSessions(): Promise<void> {
      await api.post<void>(endpoints.auth.sessionsRevokeOthers, {}, QUIET);
    },

    async startTwoFactor(): Promise<TwoFactorEnrollment> {
      const wire = await api.post<{
        mfaMethodId?: string;
        secret?: string | null;
        otpAuthUri?: string | null;
      }>(endpoints.auth.mfaEnrollmentStart, {}, QUIET);
      return {
        methodId: wire.mfaMethodId ?? "",
        secret: wire.secret ?? "",
        otpAuthUri: wire.otpAuthUri ?? "",
      };
    },

    /** Activates the authenticator; the recovery codes are shown once. */
    async verifyTwoFactor(
      methodId: string,
      code: string,
    ): Promise<readonly string[]> {
      const wire = await api.post<{ recoveryCodes?: string[] | null }>(
        endpoints.auth.mfaEnrollmentVerify,
        { mfaMethodId: methodId, code },
        QUIET,
      );
      return wire.recoveryCodes ?? [];
    },

    async getStepUpRequirement(
      purpose: StepUpPurpose,
    ): Promise<StepUpRequirement> {
      return toStepUpRequirement(
        await api.post(endpoints.auth.stepUpRequirement, { purpose }, QUIET),
      );
    },

    /** The single-use proof of a second-factor answer. */
    async completeStepUpWithCode(
      purpose: StepUpPurpose,
      challengeToken: string,
      code: string,
    ): Promise<string> {
      const wire = await api.post<{ proofToken?: string | null }>(
        endpoints.auth.stepUpCompleteMfa,
        { purpose, challengeToken, code },
        QUIET,
      );
      return wire.proofToken ?? "";
    },

    async completeStepUpWithPassword(
      purpose: StepUpPurpose,
      password: string,
    ): Promise<string> {
      const wire = await api.post<{ proofToken?: string | null }>(
        endpoints.auth.stepUpCompletePassword,
        { purpose, password },
        QUIET,
      );
      return wire.proofToken ?? "";
    },

    /**
     * Answers 200 with no body. The producer then ends every session of the
     * person, this one included, and emails them; the step-up proof
     * (`ChangePassword`) is required only while two-factor is on.
     */
    async changePassword(input: {
      currentPassword: string;
      newPassword: string;
      stepUpToken?: string;
    }): Promise<void> {
      await api.post<void>(endpoints.auth.changePassword, input, QUIET);
    },

    async disableTwoFactor(stepUpToken: string): Promise<void> {
      await api.post<void>(endpoints.auth.mfaDisable, { stepUpToken }, QUIET);
    },

    async regenerateRecoveryCodes(
      stepUpToken: string,
    ): Promise<readonly string[]> {
      const wire = await api.post<{ recoveryCodes?: string[] | null }>(
        endpoints.auth.mfaRecoveryCodes,
        { stepUpToken },
        QUIET,
      );
      return wire.recoveryCodes ?? [];
    },
  };
}

export type AccountService = ReturnType<typeof createAccountService>;
