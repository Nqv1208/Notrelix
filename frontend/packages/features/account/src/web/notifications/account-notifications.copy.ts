/**
 * Account notification preferences copy, vi and en, registered as the
 * `account-notifications` catalog (`account-notifications.catalog.ts`). The
 * pure surface takes a `copy` prop; `useAccountNotificationsCopy` resolves
 * it for the active locale in the container.
 */
export const en = {
  title: "Notifications",
  description: "Choose how you want to be notified in each workspace.",
  loading: "Loading your notification preferences…",
  loadError: "We couldn't load your notification preferences.",
  retry: "Try again",
  matrixLabel: "Notifications by event and channel",
  when: "When",
  channels: {
    inApp: "In the app",
    email: "Email",
  },
  workspaceScope: "These choices apply to this workspace only.",
  cellLabel: "{{event}}: {{channel}}",
  events: {
    assigned: "Assigned to you",
    mention: "Mentions",
    approval: "Needs your approval",
    overdue: "Overdue items",
    watching: "Items you follow change",
    invitation: "Invitations",
    digest: "Daily digest",
  },
  eventNotes: {
    mention: "Someone mentions @{{name}} in a comment or page",
    overdue: "Items assigned to you or that you follow",
    watching: "Status, due date or assignee changed",
    invitation: "To a workspace, department, board or page",
    digest: "Sent at 08:00 · items due and items that need you",
  },
  mentionRule:
    "Someone you mention who can't see the item doesn't get a notification (COL-030).",
  save: "Save preferences",
  saving: "Saving…",
  saved: "Notification preferences saved.",
  saveError: "We couldn't save your notification preferences. Try again.",
};

export type AccountNotificationsCopy = typeof en;

export const vi: AccountNotificationsCopy = {
  title: "Thông báo",
  description: "Chọn cách bạn muốn nhận thông báo trong từng workspace.",
  loading: "Đang tải tùy chọn thông báo của bạn…",
  loadError: "Không thể tải tùy chọn thông báo của bạn.",
  retry: "Thử lại",
  matrixLabel: "Thông báo theo sự kiện và kênh",
  when: "Khi nào",
  channels: {
    inApp: "Trong ứng dụng",
    email: "Email",
  },
  workspaceScope: "Lựa chọn này chỉ áp dụng cho workspace này.",
  cellLabel: "{{event}}: {{channel}}",
  events: {
    assigned: "Được giao việc",
    mention: "Nhắc tên",
    approval: "Cần bạn duyệt",
    overdue: "Việc quá hạn",
    watching: "Việc bạn theo dõi thay đổi",
    invitation: "Lời mời",
    digest: "Tóm tắt hằng ngày",
  },
  eventNotes: {
    mention: "Ai đó nhắc @{{name}} trong bình luận hoặc trang",
    overdue: "Việc giao cho bạn hoặc bạn theo dõi",
    watching: "Trạng thái, hạn hoặc người phụ trách đổi",
    invitation: "Vào workspace, phòng ban, board hoặc trang",
    digest: "Gửi lúc 08:00 · việc đến hạn và việc cần bạn",
  },
  mentionRule:
    "Nhắc tên người không xem được việc thì họ không nhận thông báo (COL-030).",
  save: "Lưu tùy chọn",
  saving: "Đang lưu…",
  saved: "Đã lưu tùy chọn thông báo.",
  saveError: "Không thể lưu tùy chọn thông báo của bạn. Hãy thử lại.",
};

export function accountNotificationsCopy(
  locale: "vi" | "en",
): AccountNotificationsCopy {
  return locale === "vi" ? vi : en;
}
