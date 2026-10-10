/**
 * Workspace Home and My work copy (WX-HOME-001/002, WX-DEC-008), vi and en.
 *
 * Plain data with no imports so pure surfaces depend on the type only.
 * `workspace-home.catalog.ts` registers it as the `workspace-home` catalog.
 * `{{name}}` placeholders are filled with `formatCopy`.
 */
export const en = {
  page: {
    title: "Home",
    greeting: "Pick up where you left off in {{workspace}}.",
    greetingLoading: "Pick up where you left off.",
    sideLabel: "Workspace updates",
  },
  sections: {
    retry: "Try again",
    error: "This section couldn't load. Your data is unchanged.",
  },
  continue: {
    title: "Continue",
    description: "Boards and Docs you opened recently.",
    empty: "Boards and Docs you open will show up here.",
    emptyAction: "Browse Boards and Docs",
    opened: "Opened {{time}}",
    board: "Board",
    doc: "Doc",
    viewAll: "All Boards and Docs",
  },
  myWork: {
    title: "My work",
    description: "Open items assigned to you, by due date.",
    empty: "Nothing is assigned to you right now.",
    emptyHint:
      "Items assigned to you on any Board in this workspace appear here.",
    viewAll: "View all my work",
    windows: {
      overdue: "Overdue",
      today: "Today",
      "this-week": "This week",
      later: "Later",
      "no-date": "No due date",
    },
    due: "Due {{date}}",
    noDue: "No due date",
    complete: "Mark {{name}} complete",
    completing: "Completing {{name}}…",
    openItem: "Open {{name}}",
    onBoard: "on {{board}}",
    boardUnavailable: "a Board you can't open",
    loadMore: "Load more",
    loadingMore: "Loading…",
    itemCount_one: "{{count}} item",
    itemCount_other: "{{count}} items",
  },
  activity: {
    title: "Activity",
    empty: "No recent activity in this workspace yet.",
    entry: "{{actor}} {{action}} {{target}}",
  },
  invitations: {
    title: "Pending invitations",
    empty: "No invitations are waiting for an answer.",
    expires: "Expires {{time}}",
    manage: "Manage invitations",
    role: "as {{role}}",
  },
  roles: {
    owner: "Owner",
    admin: "Admin",
    member: "Member",
    guest: "Guest",
  },
  onboarding: {
    title: "Set up {{workspace}}",
    description:
      "This workspace is empty. Start with a Board to track work, a Doc to write things down, or invite the people you work with.",
    boardTitle: "Create a Board",
    boardDescription:
      "Plan and track work as items in groups, with a table or Kanban view.",
    boardAction: "New Board",
    docTitle: "Write a Doc",
    docDescription:
      "Capture specs, notes and decisions that your team can edit together.",
    docAction: "New Doc",
    inviteTitle: "Invite people",
    inviteDescription:
      "Bring teammates in so you can assign work and share Docs.",
    inviteAction: "Invite people",
    inviteUnavailable: "Only workspace admins can invite people.",
    createUnavailable:
      "Guests can't create Boards or Docs here. Ask an admin for member access.",
  },
  create: {
    boardTitle: "New Board",
    boardDescription: "Give the Board a name. You can rename it later.",
    boardLabel: "Board name",
    boardPlaceholder: "e.g. Product roadmap",
    docTitle: "New Doc",
    docDescription: "Give the Doc a title. You can change it later.",
    docLabel: "Doc title",
    docPlaceholder: "e.g. Launch plan",
    cancel: "Cancel",
    submitBoard: "Create Board",
    submitDoc: "Create Doc",
    submitting: "Creating…",
    required: "Enter a name to continue.",
    error: "It couldn't be created. Check your connection, then try again.",
  },
  toasts: {
    completed: "{{name}} is complete.",
    completeFailed: "{{name}} couldn't be marked complete.",
    completeFailedHint: "It's still open. Try again in a moment.",
    created: "{{name}} is ready.",
  },
};

export type WorkspaceHomeCopy = typeof en;

export const vi: WorkspaceHomeCopy = {
  page: {
    title: "Trang chủ",
    greeting: "Tiếp tục công việc dang dở trong {{workspace}}.",
    greetingLoading: "Tiếp tục công việc dang dở.",
    sideLabel: "Cập nhật của không gian làm việc",
  },
  sections: {
    retry: "Thử lại",
    error: "Không tải được mục này. Dữ liệu của bạn không thay đổi.",
  },
  continue: {
    title: "Tiếp tục",
    description: "Các Bảng và Tài liệu bạn mở gần đây.",
    empty: "Các Bảng và Tài liệu bạn mở sẽ hiện ở đây.",
    emptyAction: "Xem các Bảng và Tài liệu",
    opened: "Đã mở {{time}}",
    board: "Bảng",
    doc: "Tài liệu",
    viewAll: "Tất cả Bảng và Tài liệu",
  },
  myWork: {
    title: "Việc của tôi",
    description: "Các mục đang mở được giao cho bạn, theo hạn.",
    empty: "Hiện chưa có mục nào được giao cho bạn.",
    emptyHint:
      "Các mục được giao cho bạn trên bất kỳ Bảng nào trong không gian làm việc này sẽ hiện ở đây.",
    viewAll: "Xem tất cả việc của tôi",
    windows: {
      overdue: "Quá hạn",
      today: "Hôm nay",
      "this-week": "Tuần này",
      later: "Sau này",
      "no-date": "Không có hạn",
    },
    due: "Hạn {{date}}",
    noDue: "Không có hạn",
    complete: "Đánh dấu hoàn thành {{name}}",
    completing: "Đang hoàn thành {{name}}…",
    openItem: "Mở {{name}}",
    onBoard: "trong {{board}}",
    boardUnavailable: "một Bảng bạn không mở được",
    loadMore: "Tải thêm",
    loadingMore: "Đang tải…",
    itemCount_one: "{{count}} mục",
    itemCount_other: "{{count}} mục",
  },
  activity: {
    title: "Hoạt động",
    empty: "Chưa có hoạt động gần đây trong không gian làm việc này.",
    entry: "{{actor}} {{action}} {{target}}",
  },
  invitations: {
    title: "Lời mời đang chờ",
    empty: "Không có lời mời nào đang chờ trả lời.",
    expires: "Hết hạn {{time}}",
    manage: "Quản lý lời mời",
    role: "với vai trò {{role}}",
  },
  roles: {
    owner: "Chủ sở hữu",
    admin: "Quản trị viên",
    member: "Thành viên",
    guest: "Khách",
  },
  onboarding: {
    title: "Thiết lập {{workspace}}",
    description:
      "Không gian làm việc này đang trống. Hãy bắt đầu với một Bảng để theo dõi công việc, một Tài liệu để ghi chép, hoặc mời những người bạn cộng tác.",
    boardTitle: "Tạo một Bảng",
    boardDescription:
      "Lên kế hoạch và theo dõi công việc theo từng mục trong nhóm, với dạng bảng hoặc Kanban.",
    boardAction: "Bảng mới",
    docTitle: "Viết một Tài liệu",
    docDescription:
      "Ghi lại đặc tả, ghi chú và quyết định để cả nhóm cùng chỉnh sửa.",
    docAction: "Tài liệu mới",
    inviteTitle: "Mời mọi người",
    inviteDescription:
      "Mời đồng đội tham gia để giao việc và chia sẻ Tài liệu.",
    inviteAction: "Mời mọi người",
    inviteUnavailable: "Chỉ quản trị viên mới mời được người khác.",
    createUnavailable:
      "Khách không tạo được Bảng hoặc Tài liệu ở đây. Hãy nhờ quản trị viên cấp quyền thành viên.",
  },
  create: {
    boardTitle: "Bảng mới",
    boardDescription: "Đặt tên cho Bảng. Bạn có thể đổi tên sau.",
    boardLabel: "Tên Bảng",
    boardPlaceholder: "VD: Lộ trình sản phẩm",
    docTitle: "Tài liệu mới",
    docDescription: "Đặt tiêu đề cho Tài liệu. Bạn có thể đổi sau.",
    docLabel: "Tiêu đề Tài liệu",
    docPlaceholder: "VD: Kế hoạch ra mắt",
    cancel: "Hủy",
    submitBoard: "Tạo Bảng",
    submitDoc: "Tạo Tài liệu",
    submitting: "Đang tạo…",
    required: "Nhập tên để tiếp tục.",
    error: "Không tạo được. Kiểm tra kết nối rồi thử lại.",
  },
  toasts: {
    completed: "Đã hoàn thành {{name}}.",
    completeFailed: "Không đánh dấu hoàn thành được {{name}}.",
    completeFailedHint: "Mục vẫn đang mở. Hãy thử lại sau giây lát.",
    created: "{{name}} đã sẵn sàng.",
  },
};

/** The copy for one locale (pure; used by fixtures and containers). */
export function workspaceHomeCopy(locale: "vi" | "en"): WorkspaceHomeCopy {
  return locale === "en" ? en : vi;
}
