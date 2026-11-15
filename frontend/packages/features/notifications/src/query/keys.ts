import { accountQueryKey } from "@notrelix/query";

export const notificationsQueryKeys = {
  all: (accountId: string) => accountQueryKey(accountId, "notifications"),
  unreadCount: (accountId: string) =>
    accountQueryKey(accountId, "notifications", "unread-count"),
  /** One person's choices, the same in every workspace. */
  preferences: (accountId: string) =>
    accountQueryKey(accountId, "notification-preferences"),
} as const;
