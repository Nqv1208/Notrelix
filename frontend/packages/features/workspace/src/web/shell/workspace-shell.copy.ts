/**
 * Workspace shell copy (WX-SHELL-001…005, WX-DEC-008), vi and en.
 *
 * Plain data with no imports so pure surfaces can depend on the type without
 * pulling the i18n runtime. `workspace-shell.catalog.ts` registers it as the
 * `workspace-shell` catalog. Pure surfaces receive the resolved copy as a
 * prop; containers resolve it with `useWorkspaceShellCopy()`. `{{name}}`
 * placeholders are filled with `formatCopy`.
 */
export const en = {
  nav: {
    label: "Workspace navigation",
    primary: "Main",
    manage: "Manage",
    home: "Home",
    myWork: "My work",
    inbox: "Inbox",
    search: "Search",
    library: "Boards & Docs",
    members: "Members",
    settings: "Settings",
  },
  sections: {
    home: "Home",
    myWork: "My work",
    inbox: "Inbox",
    search: "Search",
    library: "Boards & Docs",
    members: "Members",
    settings: "Settings",
    billing: "Billing",
    account: "Account",
  },
  views: {
    table: "Table",
    kanban: "Kanban",
    calendar: "Calendar",
    timeline: "Timeline",
  },
  breadcrumb: {
    label: "Breadcrumb",
    loading: "Loading…",
    unavailable: "Not available",
  },
  switcher: {
    trigger: "Switch workspace, current: {{name}}",
    heading: "Workspaces",
    current: "Current",
    create: "Create workspace",
    loading: "Loading workspace…",
    roles: {
      owner: "Owner",
      admin: "Admin",
      member: "Member",
      guest: "Guest",
    },
  },
  lists: {
    pinned: "Pinned",
    recent: "Recent",
    toggle: "Show or hide {{section}}",
    pinnedEmpty: "Pin a Board or Doc from its menu to keep it here.",
    recentEmpty: "Boards and Docs you open will appear here.",
    error: "This list couldn't load.",
    retry: "Retry",
    rowMenu: "More actions for {{name}}",
    open: "Open",
    pin: "Pin",
    unpin: "Unpin",
    board: "Board",
    doc: "Doc",
  },
  user: {
    menu: "Account menu for {{name}}",
    profile: "Profile",
    appearance: "Appearance",
    shortcuts: "Keyboard shortcuts",
    signOut: "Sign out",
    signingOut: "Signing out…",
    unknown: "Signed-in member",
  },
  topbar: {
    openNavigation: "Show or hide navigation",
    command: "Search or jump to…",
    commandLabel: "Open the command palette",
    themeToLight: "Switch to light theme",
    themeToDark: "Switch to dark theme",
  },
  palette: {
    title: "Command palette",
    description: "Search Boards, Docs, people and actions in this workspace.",
    placeholder: "Search or type a command…",
    empty: "Nothing matches “{{query}}”.",
    groups: {
      navigation: "Go to",
      boards: "Boards",
      docs: "Docs",
      members: "People",
      actions: "Actions",
    },
    searchAll: "Search the workspace for “{{query}}”",
    openSearch: "Open workspace search",
    toggleThemeLight: "Switch to light theme",
    toggleThemeDark: "Switch to dark theme",
    shortcuts: "Show keyboard shortcuts",
    createWorkspace: "Create workspace",
  },
  shortcuts: {
    title: "Keyboard shortcuts",
    description:
      "Move around the workspace from the keyboard. Shortcuts pause while you type in a field.",
    navigation: "Navigation",
    general: "General",
    then: "then",
    goHome: "Go to Home",
    goMyWork: "Go to My work",
    goInbox: "Go to Inbox",
    openPalette: "Open the command palette",
    showShortcuts: "Show keyboard shortcuts",
    close: "Close",
  },
  scope: {
    loadingTitle: "Opening workspace",
    loadingDescription: "Checking your access…",
    forbiddenTitle: "You don't have access to this workspace",
    forbiddenDescription:
      "It may have been removed, or your membership changed. Ask a workspace admin to invite you again.",
    errorTitle: "This workspace couldn't load",
    errorDescription:
      "Your access is unchanged. Check your connection, then try again.",
    retry: "Try again",
    exit: "Go to my workspaces",
  },
  pages: {
    inboxTitle: "Inbox",
    inboxDescription: "Mentions, requests and replies that need you.",
    inboxEmptyTitle: "Inbox isn't available yet",
    inboxEmptyDescription:
      "Mentions and requests will collect here. Until then, updates arrive in the notifications bell at the top of the page.",
    openPalette: "Find a Board or Doc",
  },
  createWorkspace: {
    title: "Create workspace",
    description: "A workspace holds your team's Boards, Docs and members.",
    nameLabel: "Workspace name",
    namePlaceholder: "e.g. Product team",
    cancel: "Cancel",
    submit: "Create workspace",
    submitting: "Creating…",
    error: "The workspace couldn't be created. Try again.",
  },
  landing: {
    loadingTitle: "Loading your workspaces",
    emptyTitle: "Create your first workspace",
    emptyDescription:
      "Workspaces hold Boards, Docs and the people you work with. Create one to get started, or ask a teammate to invite you.",
    errorTitle: "Your workspaces couldn't load",
    errorDescription: "Check your connection, then try again.",
    retry: "Try again",
  },
};

export type WorkspaceShellCopy = typeof en;

export const vi: WorkspaceShellCopy = {
  nav: {
    label: "Điều hướng không gian làm việc",
    primary: "Chính",
    manage: "Quản lý",
    home: "Trang chủ",
    myWork: "Việc của tôi",
    inbox: "Hộp thư",
    search: "Tìm kiếm",
    library: "Bảng & Tài liệu",
    members: "Thành viên",
    settings: "Cài đặt",
  },
  sections: {
    home: "Trang chủ",
    myWork: "Việc của tôi",
    inbox: "Hộp thư",
    search: "Tìm kiếm",
    library: "Bảng & Tài liệu",
    members: "Thành viên",
    settings: "Cài đặt",
    billing: "Thanh toán",
    account: "Tài khoản",
  },
  views: {
    table: "Bảng",
    kanban: "Kanban",
    calendar: "Lịch",
    timeline: "Dòng thời gian",
  },
  breadcrumb: {
    label: "Đường dẫn",
    loading: "Đang tải…",
    unavailable: "Không khả dụng",
  },
  switcher: {
    trigger: "Chuyển không gian làm việc, hiện tại: {{name}}",
    heading: "Không gian làm việc",
    current: "Hiện tại",
    create: "Tạo không gian làm việc",
    loading: "Đang tải không gian làm việc…",
    roles: {
      owner: "Chủ sở hữu",
      admin: "Quản trị viên",
      member: "Thành viên",
      guest: "Khách",
    },
  },
  lists: {
    pinned: "Đã ghim",
    recent: "Gần đây",
    toggle: "Hiện hoặc ẩn {{section}}",
    pinnedEmpty: "Ghim một Bảng hoặc Tài liệu từ menu của nó để giữ ở đây.",
    recentEmpty: "Các Bảng và Tài liệu bạn mở sẽ hiện ở đây.",
    error: "Không tải được danh sách này.",
    retry: "Thử lại",
    rowMenu: "Thao tác khác cho {{name}}",
    open: "Mở",
    pin: "Ghim",
    unpin: "Bỏ ghim",
    board: "Bảng",
    doc: "Tài liệu",
  },
  user: {
    menu: "Menu tài khoản của {{name}}",
    profile: "Hồ sơ",
    appearance: "Giao diện",
    shortcuts: "Phím tắt",
    signOut: "Đăng xuất",
    signingOut: "Đang đăng xuất…",
    unknown: "Thành viên đã đăng nhập",
  },
  topbar: {
    openNavigation: "Hiện hoặc ẩn điều hướng",
    command: "Tìm kiếm hoặc chuyển tới…",
    commandLabel: "Mở bảng lệnh",
    themeToLight: "Chuyển sang giao diện sáng",
    themeToDark: "Chuyển sang giao diện tối",
  },
  palette: {
    title: "Bảng lệnh",
    description:
      "Tìm Bảng, Tài liệu, mọi người và thao tác trong không gian làm việc này.",
    placeholder: "Tìm kiếm hoặc nhập lệnh…",
    empty: "Không có kết quả cho “{{query}}”.",
    groups: {
      navigation: "Chuyển tới",
      boards: "Bảng",
      docs: "Tài liệu",
      members: "Mọi người",
      actions: "Thao tác",
    },
    searchAll: "Tìm “{{query}}” trong không gian làm việc",
    openSearch: "Mở tìm kiếm trong không gian làm việc",
    toggleThemeLight: "Chuyển sang giao diện sáng",
    toggleThemeDark: "Chuyển sang giao diện tối",
    shortcuts: "Xem phím tắt",
    createWorkspace: "Tạo không gian làm việc",
  },
  shortcuts: {
    title: "Phím tắt",
    description:
      "Di chuyển trong không gian làm việc bằng bàn phím. Phím tắt tạm dừng khi bạn đang nhập vào ô.",
    navigation: "Điều hướng",
    general: "Chung",
    then: "rồi",
    goHome: "Tới Trang chủ",
    goMyWork: "Tới Việc của tôi",
    goInbox: "Tới Hộp thư",
    openPalette: "Mở bảng lệnh",
    showShortcuts: "Xem phím tắt",
    close: "Đóng",
  },
  scope: {
    loadingTitle: "Đang mở không gian làm việc",
    loadingDescription: "Đang kiểm tra quyền truy cập…",
    forbiddenTitle: "Bạn không có quyền truy cập không gian làm việc này",
    forbiddenDescription:
      "Không gian có thể đã bị xóa, hoặc tư cách thành viên của bạn đã thay đổi. Hãy nhờ quản trị viên mời lại bạn.",
    errorTitle: "Không tải được không gian làm việc",
    errorDescription:
      "Quyền truy cập của bạn không đổi. Kiểm tra kết nối rồi thử lại.",
    retry: "Thử lại",
    exit: "Tới các không gian làm việc của tôi",
  },
  pages: {
    inboxTitle: "Hộp thư",
    inboxDescription: "Lượt nhắc, yêu cầu và phản hồi cần bạn xử lý.",
    inboxEmptyTitle: "Hộp thư chưa khả dụng",
    inboxEmptyDescription:
      "Lượt nhắc và yêu cầu sẽ được gom ở đây. Trong lúc chờ, thông báo vẫn đến ở chuông thông báo phía trên trang.",
    openPalette: "Tìm Bảng hoặc Tài liệu",
  },
  createWorkspace: {
    title: "Tạo không gian làm việc",
    description:
      "Không gian làm việc chứa các Bảng, Tài liệu và thành viên của nhóm.",
    nameLabel: "Tên không gian làm việc",
    namePlaceholder: "VD: Nhóm sản phẩm",
    cancel: "Hủy",
    submit: "Tạo không gian làm việc",
    submitting: "Đang tạo…",
    error: "Không tạo được không gian làm việc. Hãy thử lại.",
  },
  landing: {
    loadingTitle: "Đang tải các không gian làm việc",
    emptyTitle: "Tạo không gian làm việc đầu tiên",
    emptyDescription:
      "Không gian làm việc chứa các Bảng, Tài liệu và những người bạn cộng tác. Hãy tạo một không gian, hoặc nhờ đồng nghiệp mời bạn.",
    errorTitle: "Không tải được các không gian làm việc",
    errorDescription: "Kiểm tra kết nối rồi thử lại.",
    retry: "Thử lại",
  },
};

/** The copy for one locale (pure; used by fixtures and containers). */
export function workspaceShellCopy(locale: "vi" | "en"): WorkspaceShellCopy {
  return locale === "en" ? en : vi;
}
