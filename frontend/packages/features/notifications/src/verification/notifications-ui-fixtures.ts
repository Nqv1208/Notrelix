import { createFormatters } from "@notrelix/i18n";
import type { Notification } from "../core/types/notifications";
import {
  notificationBellCopy,
  type NotificationBellCopy,
} from "../web/components/notification-bell.copy";
import type { NotificationBellFormatters } from "../web/components/notification-bell-surface";

/** Injected "now" so relative times are deterministic (two hours after the fixtures). */
export const NOTIFICATIONS_REFERENCE_DATE = "2026-01-15T12:30:00.000Z";

export function notificationsCopyFixture(
  locale: "en" | "vi" = "en",
): NotificationBellCopy {
  return notificationBellCopy(locale);
}

export function notificationsFormattersFixture(
  locale: "en" | "vi" = "en",
): NotificationBellFormatters {
  return createFormatters(locale);
}

export function notification(
  overrides: Partial<Notification> & Pick<Notification, "id" | "title">,
): Notification {
  return {
    workspaceId: overrides.workspaceId ?? "ws-main",
    userId: overrides.userId ?? "user-1",
    type: overrides.type ?? "system",
    body: overrides.body ?? "Notification body",
    isRead: overrides.isRead ?? false,
    isArchived: overrides.isArchived ?? false,
    createdAt: overrides.createdAt ?? "2026-01-15T10:30:00.000Z",
    ...overrides,
  };
}

export function notificationsDefaultScenario(): Notification[] {
  return [
    notification({
      id: "notif-mention",
      type: "mention",
      title: "Ada mentioned you",
      body: "Can you review the migration risks section?",
    }),
    notification({
      id: "notif-comment",
      type: "comment",
      title: "New comment on Operating plan",
      body: "Grace left a comment on your document.",
      isRead: true,
    }),
    notification({
      id: "notif-assignment",
      type: "assignment",
      title: "Task assigned to you",
      body: "Prepare the launch readiness checklist.",
    }),
  ];
}

export function notificationsEmptyScenario(): Notification[] {
  return [];
}

export function notificationsEdgeDataScenario(): Notification[] {
  return [
    notification({
      id: "notif-long-title",
      type: "system",
      title:
        "Enterprise rollout governance review requires additional approval",
      body: "Security and legal sign-off is required before publication.",
    }),
    notification({
      id: "notif-long-title-vi",
      type: "mention",
      title:
        "Nguyễn Thị Phương Anh đã nhắc đến bạn trong bản kế hoạch triển khai doanh nghiệp quý bốn",
      body: "Vui lòng xem lại phần đánh giá rủi ro di chuyển dữ liệu trước khi phát hành cho toàn bộ không gian làm việc.",
    }),
    notification({
      id: "notif-invitation",
      type: "invitation",
      title: "Workspace invitation",
      body: "You have been invited to the Enterprise Program workspace.",
    }),
    notification({
      id: "notif-status",
      type: "status_change",
      title: "Card moved to Blocked",
      body: "Migration risk card changed status.",
      isRead: true,
    }),
  ];
}
