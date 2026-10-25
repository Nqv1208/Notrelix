export type NotificationType =
  | "mention"
  | "comment"
  | "assignment"
  | "status_change"
  | "invitation"
  | "system";

export interface Notification {
  id: string;
  workspaceId: string;
  userId: string;
  type: NotificationType;
  title: string;
  body: string;
  link?: string;
  isRead: boolean;
  isArchived: boolean;
  createdAt: string;
}

/** The types a member sets preferences for, in the order a client shows them. */
export const NOTIFICATION_PREFERENCE_TYPES = [
  "assigned",
  "mention",
  "approval",
  "overdue",
  "watching",
  "invitation",
  "digest",
] as const;
export type NotificationPreferenceType =
  (typeof NOTIFICATION_PREFERENCE_TYPES)[number];

/** One type on the two channels the producer delivers on. */
export interface NotificationChannelChoice {
  readonly inApp: boolean;
  readonly email: boolean;
}

/**
 * A member's notification choices in one workspace (workspace-scoped, not
 * account-scoped). `version` is the value the next save quotes as
 * `expectedVersion`.
 */
export interface NotificationPreferences {
  readonly version: number;
  readonly rows: Readonly<
    Record<NotificationPreferenceType, NotificationChannelChoice>
  >;
}
