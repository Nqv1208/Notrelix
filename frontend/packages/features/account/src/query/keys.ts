import { accountQueryKey } from "@notrelix/query";

export const accountQueryKeys = {
  all: (accountId: string) => accountQueryKey(accountId, "account"),
  profile: (accountId: string) =>
    accountQueryKey(accountId, "account", "profile"),
  preferences: (accountId: string) =>
    accountQueryKey(accountId, "account", "preferences"),
  security: (accountId: string) =>
    accountQueryKey(accountId, "account", "security"),
  sessions: (accountId: string) =>
    accountQueryKey(accountId, "account", "sessions"),
  activity: (accountId: string) =>
    accountQueryKey(accountId, "account", "security-activity"),
  /** The API tokens of one workspace (metadata only). */
  apiTokens: (accountId: string, workspaceId: string) =>
    accountQueryKey(accountId, "account", "api-tokens", workspaceId),
  /** The workspaces the person belongs to (account-scoped: it is their own list). */
  workspaces: (accountId: string) =>
    accountQueryKey(accountId, "account", "workspaces"),
  /** The Owner's Recently deleted workspaces (restorable for 30 days). */
  deletedWorkspaces: (accountId: string) =>
    accountQueryKey(accountId, "account", "deleted-workspaces"),
  /** The member's job title is a workspace fact of the person's own row. */
  memberProfile: (accountId: string, workspaceId: string) =>
    accountQueryKey(accountId, "account", "member-profile", workspaceId),
} as const;
