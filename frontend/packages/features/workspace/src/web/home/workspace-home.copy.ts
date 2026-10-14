/**
 * Workspace Home and My work copy (WX-HOME-001/002, WX-DEC-008), vi and en.
 *
 * Plain data with no imports so pure surfaces depend on the type only.
 * `workspace-home.catalog.ts` registers it as the `workspace-home` catalog.
 * `{{name}}` placeholders are filled with `formatCopy`.
 */
export const en = {
  sections: {
    retry: "Try again",
    error: "This section couldn't load. Your data is unchanged.",
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
    inSpace: "It will be placed in {{space}}.",
  },
  work: {
    weekdays: {
      sun: "Sunday",
      mon: "Monday",
      tue: "Tuesday",
      wed: "Wednesday",
      thu: "Thursday",
      fri: "Friday",
      sat: "Saturday",
    },
    due: {
      none: "No due date",
      today: "Today",
      overdue_one: "Overdue {{count}} day",
      overdue_other: "Overdue {{count}} days",
    },
    status: {
      todo: "To do",
      doing: "In progress",
      review: "In review",
      done: "Done",
    },
    priority: {
      urgent: "Urgent",
      high: "High",
      medium: "Medium",
      low: "Low",
    },
    priorityLabel: "Priority: {{priority}}",
    reasons: {
      mine: "Assigned to me",
      follow: "Following",
      approve: "Needs my approval",
    },
  },
  home: {
    greeting: {
      morning: "Good morning, {{name}}",
      noon: "Good afternoon, {{name}}",
      afternoon: "Good afternoon, {{name}}",
      evening: "Good evening, {{name}}",
      plain: "Welcome back",
    },
    summary: "{{date}} · {{open}} open",
    summaryLoading: "{{date}} · loading your work…",
    calendar: "My calendar",
    calendarUnavailable: "A personal calendar is not available yet.",
    addItem: "Add item",
    addItemUnavailable: "Add items from My work or from a Board.",
    chipsLabel: "Quick filters for what needs attention",
    chips: {
      overdue: "overdue",
      today: "due today",
      approvals: "waiting for your approval",
      mentions: "mention you",
    },
    sections: {
      overdue: "Overdue",
      today: "Today",
      week: "This week",
      viewAll: "View all my work",
    },
    todayEmpty: "Nothing else is due today",
    todayEmptyOverdue: "Nothing else is due today · {{count}} overdue",
    todayEmptyNext: "The next item is due {{when}}.",
    later_one: "Later · {{count}} item due from next week",
    later_other: "Later · {{count}} items due from next week",
    laterAction: "View",
    following: "you are following",
    sectionError: "This section couldn't load. Your data is unchanged.",
    retry: "Try again",
    replies: {
      title: "Needs your reply",
      approve: "Approve",
      mention: "Mention",
      approveAction: "Approve",
      changesAction: "Request changes",
      viewAction: "View",
      replyAction: "Reply",
      openDocAction: "Open doc",
    },
    agenda: {
      title: "Today's schedule",
      now: "{{time}} now",
    },
    continue: {
      title: "Continue",
      board: "Board",
      doc: "Doc",
    },
    loadingSection: "Loading",
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
  sections: {
    retry: "Thử lại",
    error: "Không tải được mục này. Dữ liệu của bạn không thay đổi.",
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
    inSpace: "Mục này sẽ được đặt trong {{space}}.",
  },
  work: {
    weekdays: {
      sun: "Chủ nhật",
      mon: "Thứ Hai",
      tue: "Thứ Ba",
      wed: "Thứ Tư",
      thu: "Thứ Năm",
      fri: "Thứ Sáu",
      sat: "Thứ Bảy",
    },
    due: {
      none: "Không có hạn",
      today: "Hôm nay",
      overdue_one: "Quá hạn {{count}} ngày",
      overdue_other: "Quá hạn {{count}} ngày",
    },
    status: {
      todo: "Cần làm",
      doing: "Đang làm",
      review: "Chờ duyệt",
      done: "Hoàn thành",
    },
    priority: {
      urgent: "Khẩn",
      high: "Cao",
      medium: "Trung bình",
      low: "Thấp",
    },
    priorityLabel: "Ưu tiên: {{priority}}",
    reasons: {
      mine: "Giao cho tôi",
      follow: "Đang theo dõi",
      approve: "Cần tôi duyệt",
    },
  },
  home: {
    greeting: {
      morning: "Chào buổi sáng, {{name}}",
      noon: "Chào buổi trưa, {{name}}",
      afternoon: "Chào buổi chiều, {{name}}",
      evening: "Chào buổi tối, {{name}}",
      plain: "Chào mừng bạn trở lại",
    },
    summary: "{{date}} · {{open}} việc đang mở",
    summaryLoading: "{{date}} · đang tải việc của bạn…",
    calendar: "Lịch của tôi",
    calendarUnavailable: "Lịch cá nhân chưa có.",
    addItem: "Thêm công việc",
    addItemUnavailable: "Thêm việc từ Việc của tôi hoặc từ một Bảng.",
    chipsLabel: "Lọc nhanh việc cần chú ý",
    chips: {
      overdue: "quá hạn",
      today: "đến hạn hôm nay",
      approvals: "chờ bạn duyệt",
      mentions: "nhắc tên bạn",
    },
    sections: {
      overdue: "Quá hạn",
      today: "Hôm nay",
      week: "Tuần này",
      viewAll: "Xem tất cả việc của tôi",
    },
    todayEmpty: "Xong hết việc đến hạn hôm nay",
    todayEmptyOverdue:
      "Xong hết việc đến hạn hôm nay · còn {{count}} việc quá hạn",
    todayEmptyNext: "Việc tiếp theo đến hạn {{when}}.",
    later_one: "Sau đó · {{count}} việc đến hạn từ tuần sau",
    later_other: "Sau đó · {{count}} việc đến hạn từ tuần sau",
    laterAction: "Xem",
    following: "bạn đang theo dõi",
    sectionError: "Không tải được mục này. Dữ liệu của bạn không thay đổi.",
    retry: "Thử lại",
    replies: {
      title: "Cần bạn phản hồi",
      approve: "Duyệt",
      mention: "Nhắc tên",
      approveAction: "Duyệt",
      changesAction: "Yêu cầu sửa",
      viewAction: "Xem",
      replyAction: "Trả lời",
      openDocAction: "Mở tài liệu",
    },
    agenda: {
      title: "Lịch hôm nay",
      now: "{{time}} bây giờ",
    },
    continue: {
      title: "Tiếp tục",
      board: "Board",
      doc: "Tài liệu",
    },
    loadingSection: "Đang tải",
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
