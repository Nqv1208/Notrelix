import type {
  AccountNotificationValues,
  AccountWorkspaceRole,
} from "@notrelix/features-account";

/**
 * The calls behind the account pages that the profile and preference
 * contracts do not cover (gap `CTR-GAP-ACC-PAGES`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE:
 * Account"). `api` is the runtime client; every call is quiet: the pages say
 * what went wrong themselves.
 */
export interface AccountPagesApiClient {
  get<T>(url: string, options?: { skipGlobalErrorToast?: boolean }): Promise<T>;
  post<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean; idempotencyKey?: string },
  ): Promise<T>;
  put<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
  patch<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
  delete<T>(
    url: string,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
}

const QUIET = { skipGlobalErrorToast: true } as const;

export interface ProfileDetailsDto {
  readonly jobTitle: string;
  readonly phone: string;
  readonly timezone: string;
  readonly dateFormat: string;
  readonly avatarUrl: string | null;
  readonly joinedAt: string;
  readonly managedBy: string | null;
  readonly workspaceName: string;
}

export interface SecurityDetailsDto {
  readonly serverTime: string;
  readonly sso: { provider: string; email: string } | null;
  readonly twoFactorEnabled: boolean;
  readonly backupCodesLeft: number;
  readonly sessions: readonly {
    id: string;
    device: string;
    kind: "desktop" | "phone" | "tablet";
    place: string;
    lastActiveAt: string | null;
  }[];
}

export interface AccountWorkspacesDto {
  readonly workspaces: readonly {
    id: string;
    name: string;
    role: AccountWorkspaceRole;
    memberCount: number;
    departmentCount: number | null;
    unreadCount: number | null;
    guestBoard: string | null;
    otherOwnerName: string | null;
    isDefault: boolean;
    canLeave: boolean;
  }[];
}

export function createAccountPagesApi(api: AccountPagesApiClient) {
  return {
    getDetails() {
      return api.get<ProfileDetailsDto>("/account/profile-details", QUIET);
    },
    updateDetails(input: {
      jobTitle: string;
      phone: string;
      timezone: string;
      dateFormat: string;
    }) {
      return api.patch<ProfileDetailsDto>(
        "/account/profile-details",
        input,
        QUIET,
      );
    },
    setAvatar(dataUrl: string) {
      return api.put<ProfileDetailsDto>("/account/avatar", { dataUrl }, QUIET);
    },
    getSecurity() {
      return api.get<SecurityDetailsDto>("/account/security-details", QUIET);
    },
    setTwoFactor(enabled: boolean) {
      return api.put<SecurityDetailsDto>(
        "/account/security/two-factor",
        { enabled },
        QUIET,
      );
    },
    renewBackupCodes() {
      return api.post<SecurityDetailsDto>(
        "/account/security/backup-codes",
        {},
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    revokeSession(sessionId: string) {
      return api.delete<void>(
        `/account/security/sessions/${encodeURIComponent(sessionId)}`,
        QUIET,
      );
    },
    revokeOtherSessions() {
      return api.post<void>(
        "/account/security/sessions/revoke-others",
        {},
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    getNotificationMatrix() {
      return api.get<AccountNotificationValues>(
        "/account/notification-matrix",
        QUIET,
      );
    },
    saveNotificationMatrix(values: AccountNotificationValues) {
      return api.put<AccountNotificationValues>(
        "/account/notification-matrix",
        values,
        QUIET,
      );
    },
    getWorkspaces() {
      return api.get<AccountWorkspacesDto>("/account/workspaces", QUIET);
    },
    setDefaultWorkspace(workspaceId: string) {
      return api.put<AccountWorkspacesDto>(
        "/account/default-workspace",
        { workspaceId },
        QUIET,
      );
    },
    leaveWorkspace(workspaceId: string) {
      return api.delete<void>(
        `/account/workspaces/${encodeURIComponent(workspaceId)}`,
        QUIET,
      );
    },
  };
}
