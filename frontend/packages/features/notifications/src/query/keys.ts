import { accountQueryKey } from "@notrelix/query";

export const notificationsQueryKeys = {
  all: (accountId: string) => accountQueryKey(accountId, "notifications"),
  unreadCount: (accountId: string) =>
    accountQueryKey(accountId, "notifications", "unread-count"),
  preferences: (accountId: string) =>
    accountQueryKey(accountId, "notifications", "preferences"),
} as const;
