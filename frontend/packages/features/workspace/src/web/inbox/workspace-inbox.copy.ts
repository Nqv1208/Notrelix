/**
 * Inbox copy (Final-Inbox), vi and en. Plain data with no imports so pure
 * surfaces depend on the type only. `workspace-inbox.catalog.ts` registers it
 * as the `workspace-inbox` catalog. `{{name}}` placeholders are filled with
 * `formatCopy`.
 */
export const en = {
  title: "Inbox",
  listLabel: "Notifications",
  markAllRead: "Mark all as read",
  settings: "Notification settings",
  settingsUnavailable: "Notification settings are not available here yet.",
  scope: "Only workspace {{name}} · ",
  scopeAll: "See every workspace",
  tabsLabel: "Notification types",
  tabs: {
    need: "Needs action",
    mention: "Mentions",
    assigned: "Assigned",
    follow: "Following",
    all: "All",
  },
  tabUnavailable:
    "Notifications carry no type in this list yet, so this tab cannot filter.",
  unreadOnly: "Unread only",
  grouped: "Small changes are grouped",
  days: {
    today: "Today",
    yesterday: "Yesterday",
  },
  unread: "Unread. ",
  need: {
    approve: "Needs approval",
    reply: "Needs a reply",
    overdue: "Overdue",
  },
  time: {
    now: "now",
    minutes: "{{count}} min",
    hours: "{{count}} h",
  },
  bundleOpen: "Open {{count}} changes",
  bundleClose: "Hide {{count}} changes",
  kinds: {
    approve: "Approval",
    mention: "Mention",
    assign: "Assigned",
    overdue: "Overdue",
    invite: "Invitation",
    comment: "Comment",
    change: "Change",
    system: "Notification",
  },
  read: {
    label: "Notification details",
    crumb: "Breadcrumb",
    previous: "Previous notification (K)",
    next: "Next notification (J)",
    openItem: "Open item",
    openUnavailable: "This notification has no item to open.",
    approve: "Approve",
    requestChanges: "Request changes",
    actionUnavailable: "This action has no write behind it yet.",
    approveNote: "Once approved, the notification moves to Handled",
    replyLabel: "Reply",
    replyPlaceholder: "Reply to {{name}}…",
    commentsLatest: "Latest comment",
    priority: "Priority {{value}}",
    due: "· Due {{date}}",
    comments_one: "· {{count}} comment",
    comments_other: "· {{count}} comments",
    handled: "Handled",
    handledDone: "Handled",
    snooze: "Remind me later",
    mute: "Mute notifications for this item",
    shortcuts: {
      move: "move",
      handled: "handled",
      snooze: "remind",
    },
    handledUnavailable: "Marking as read is not available here.",
  },
  empty: {
    title: "Nothing needs your action",
    text: "Assigned items, mentions and approval requests show up here. Small changes are in the All tab.",
    settings: "Change which notifications I get",
    readTitle: "Nothing left to handle",
    readText: "You have handled everything. New notifications open here.",
  },
  allEmpty: {
    title: "No notifications yet",
    text: "Notifications about your work show up here.",
  },
  filteredEmpty: {
    title: "No unread notifications",
    text: "Everything here has been read. Show all to see them again.",
    showAll: "Show all",
  },
  loading: "Loading notifications…",
  error: "Notifications couldn't load. Your data is unchanged.",
  retry: "Try again",
  toasts: {
    allRead: "All notifications are marked as read.",
    markFailed: "The notification couldn't be marked as read.",
  },
};

export type WorkspaceInboxCopy = typeof en;

export const vi: WorkspaceInboxCopy = {
  title: "Hộp thư",
  listLabel: "Danh sách thông báo",
  markAllRead: "Đánh dấu tất cả đã đọc",
  settings: "Cài đặt thông báo",
  settingsUnavailable: "Chưa có cài đặt thông báo ở đây.",
  scope: "Chỉ workspace {{name}} · ",
  scopeAll: "Xem mọi workspace",
  tabsLabel: "Loại thông báo",
  tabs: {
    need: "Cần xử lý",
    mention: "Nhắc tên",
    assigned: "Được giao",
    follow: "Đang theo dõi",
    all: "Tất cả",
  },
  tabUnavailable:
    "Thông báo chưa có loại trong danh sách này, nên tab này chưa lọc được.",
  unreadOnly: "Chỉ chưa đọc",
  grouped: "Thay đổi nhỏ được gộp lại",
  days: {
    today: "Hôm nay",
    yesterday: "Hôm qua",
  },
  unread: "Chưa đọc. ",
  need: {
    approve: "Cần duyệt",
    reply: "Cần trả lời",
    overdue: "Quá hạn",
  },
  time: {
    now: "vừa xong",
    minutes: "{{count}} phút",
    hours: "{{count}} giờ",
  },
  bundleOpen: "Mở {{count}} thay đổi",
  bundleClose: "Ẩn {{count}} thay đổi",
  kinds: {
    approve: "Duyệt",
    mention: "Nhắc tên",
    assign: "Được giao",
    overdue: "Quá hạn",
    invite: "Lời mời",
    comment: "Bình luận",
    change: "Thay đổi",
    system: "Thông báo",
  },
  read: {
    label: "Chi tiết thông báo",
    crumb: "Đường dẫn",
    previous: "Thông báo trước (K)",
    next: "Thông báo sau (J)",
    openItem: "Mở công việc",
    openUnavailable: "Thông báo này không có việc để mở.",
    approve: "Duyệt",
    requestChanges: "Yêu cầu sửa",
    actionUnavailable: "Thao tác này chưa có thao tác ghi.",
    approveNote: "Duyệt xong, thông báo tự chuyển sang Đã xử lý",
    replyLabel: "Trả lời",
    replyPlaceholder: "Trả lời {{name}}…",
    commentsLatest: "Bình luận gần nhất",
    priority: "Ưu tiên {{value}}",
    due: "· Hạn {{date}}",
    comments_one: "· {{count}} bình luận",
    comments_other: "· {{count}} bình luận",
    handled: "Đã xử lý",
    handledDone: "Đã xử lý",
    snooze: "Nhắc lại sau",
    mute: "Tắt thông báo việc này",
    shortcuts: {
      move: "chuyển",
      handled: "đã xử lý",
      snooze: "nhắc lại",
    },
    handledUnavailable: "Chưa đánh dấu đã đọc được ở đây.",
  },
  empty: {
    title: "Không còn việc cần xử lý",
    text: "Việc được giao, nhắc tên và yêu cầu duyệt mới sẽ hiện ở đây. Các thay đổi nhỏ nằm ở tab Tất cả.",
    settings: "Chỉnh loại thông báo nhận",
    readTitle: "Không còn gì cần xử lý",
    readText: "Bạn đã xử lý hết. Thông báo mới sẽ mở ở đây.",
  },
  allEmpty: {
    title: "Chưa có thông báo nào",
    text: "Thông báo về công việc của bạn sẽ hiện ở đây.",
  },
  filteredEmpty: {
    title: "Không có thông báo chưa đọc",
    text: "Mọi thông báo ở đây đã đọc. Hiện tất cả để xem lại.",
    showAll: "Hiện tất cả",
  },
  loading: "Đang tải thông báo…",
  error: "Không tải được thông báo. Dữ liệu của bạn không thay đổi.",
  retry: "Thử lại",
  toasts: {
    allRead: "Đã đánh dấu đã đọc mọi thông báo.",
    markFailed: "Không đánh dấu đã đọc được thông báo.",
  },
};

/** The copy for one locale (pure; used by fixtures and containers). */
export function workspaceInboxCopy(locale: "vi" | "en"): WorkspaceInboxCopy {
  return locale === "en" ? en : vi;
}
