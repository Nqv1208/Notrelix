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

export interface NotificationPreferences {
  userId: string;
  emailEnabled: boolean;
  pushEnabled: boolean;
  mutedTypes: NotificationType[];
}
