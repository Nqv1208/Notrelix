import type { Notification } from "../core/types/notifications";

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
