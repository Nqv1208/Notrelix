import { accountQueryKey } from "@notrelix/query";

export const notificationsQueryKeys = {
  all: accountQueryKey("notifications"),
  unreadCount: accountQueryKey("notifications", "unread-count"),
  preferences: accountQueryKey("notifications", "preferences"),
} as const;
