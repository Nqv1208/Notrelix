/**
 * @notrelix/feature-notifications — Notifications query keys.
 *
 * Type C: Realtime invalidation.
 * Notification list uses TanStack Query; realtime events invalidate the query.
 */

export const notificationsQueryKeys = {
  all: ["notifications"] as const,
  unreadCount: ["notifications", "unread-count"] as const,
  preferences: ["notifications", "preferences"] as const,
} as const;
