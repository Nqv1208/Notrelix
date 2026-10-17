/**
 * Notification bell copy, vi and en, registered as the `notification-bell`
 * catalog (`notification-bell.catalog.ts`). The pure surface takes a `copy`
 * prop; `useNotificationBellCopy` resolves it for the active locale in the
 * container. Placeholders use the catalog `{{name}}` form and are filled by
 * `fillCopy`.
 */
export const en = {
  /** Bell button name when nothing is unread. */
  bell: "Notifications",
  /** Bell button name with unread items; the badge itself is decorative. */
  bellUnread_one: "Notifications, {{count}} unread",
  bellUnread_other: "Notifications, {{count}} unread",
  /** Visible badge text once the unread count passes the badge cap. */
  unreadOverflow: "99+",
  panelTitle: "Notifications",
  markAllRead: "Mark all read",
  loading: "Loading notifications…",
  empty: "No notifications found",
  markRead: "Mark “{{title}}” as read",
  markReadHint: "Mark as read",
  archive: "Archive “{{title}}”",
  archiveHint: "Archive",
};

export type NotificationBellCopy = typeof en;

export const vi: NotificationBellCopy = {
  bell: "Thông báo",
  // Vietnamese has no singular/plural distinction; both forms stay complete.
  bellUnread_one: "Thông báo, {{count}} chưa đọc",
  bellUnread_other: "Thông báo, {{count}} chưa đọc",
  unreadOverflow: "99+",
  panelTitle: "Thông báo",
  markAllRead: "Đánh dấu tất cả đã đọc",
  loading: "Đang tải thông báo…",
  empty: "Không có thông báo nào",
  markRead: "Đánh dấu “{{title}}” là đã đọc",
  markReadHint: "Đánh dấu đã đọc",
  archive: "Lưu trữ “{{title}}”",
  archiveHint: "Lưu trữ",
};

export function notificationBellCopy(
  locale: "vi" | "en",
): NotificationBellCopy {
  return locale === "vi" ? vi : en;
}

/** Fills `{{name}}` placeholders; an unknown placeholder is left visible. */
export function fillCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** Picks the `_one` form for exactly 1 and `_other` otherwise, then fills it. */
export function pluralCopy(
  forms: { readonly one: string; readonly other: string },
  count: number,
): string {
  return fillCopy(count === 1 ? forms.one : forms.other, { count });
}
