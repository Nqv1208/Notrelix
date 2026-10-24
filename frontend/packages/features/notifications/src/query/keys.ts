import { accountQueryKey, workspaceQueryKey } from "@notrelix/query";

export const notificationsQueryKeys = {
  all: (accountId: string) => accountQueryKey(accountId, "notifications"),
  unreadCount: (accountId: string) =>
    accountQueryKey(accountId, "notifications", "unread-count"),
  /** A member's choices belong to one workspace, not to the account. */
  preferences: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "notification-preferences"),
} as const;
