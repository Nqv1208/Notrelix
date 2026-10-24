import type { AccountWorkspaceRole } from "@notrelix/features-account";

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
    setAvatar(dataUrl: string) {
      return api.put<{ avatarUrl: string | null }>(
        "/account/avatar",
        { dataUrl },
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
