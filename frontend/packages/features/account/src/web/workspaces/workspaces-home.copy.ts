/**
 * Copy of the account-level Home (`/home`): the person's workspaces as a
 * launcher, outside any workspace. Placeholders use `{{name}}`; counts use
 * `_one`/`_other`.
 */
export const en = {
  hello: "Hello, {{name}}",
  helloAnon: "Welcome back",
  summary_one: "You belong to {{count}} workspace. Pick where to work.",
  summary_other: "You belong to {{count}} workspaces. Pick where to work.",
  summaryNone: "You are not in a workspace yet.",
  yourWorkspaces: "Your workspaces",
  create: "New workspace",
  createCard: "Create or join a workspace",
  createHint: "Start a new one or join with an invitation.",
  loading: "Loading your workspaces…",
  loadError: "We couldn't load your workspaces.",
  retry: "Try again",
  defaultTag: "Opens by default",
  makeDefault: "Make default",
  makeDefaultLabel: "Make {{name}} the one that opens by default",
  open: "Open",
  openLabel: "Open {{name}}",
  roles: {
    owner: "Workspace owner",
    admin: "Workspace admin",
    member: "Member",
    guest: "Guest",
  },
  people_one: "{{count}} person",
  people_other: "{{count}} people",
  archivedTitle: "Archived",
  archivedNote: "Read-only. Bring one back to work in it again.",
  archivedTag: "Archived",
  unarchive: "Unarchive",
  unarchiveLabel: "Unarchive the workspace {{name}}",
  deletedTitle: "Recently deleted",
  deletedNote:
    "Restore one within 30 days and everything comes back as it was.",
  deletedMeta: "Deleted {{date}} · {{left}}",
  daysLeft_one: "{{count}} day left",
  daysLeft_other: "{{count}} days left",
  restore: "Restore",
  restoreLabel: "Restore the workspace {{name}}",
  deletedLoadError: "We couldn't load your recently deleted workspaces.",
  actionError: "We couldn't do that. Try again.",
  restoreError:
    "We couldn't restore that workspace. It may have changed or its 30 days may be over.",
  accountLink: "Account settings",
  emptyTitle: "No workspace yet",
  emptyDescription:
    "Create one for your team, or open an invitation link you were sent to join one.",
  notice: {
    defaultSet: "{{name}} now opens by default.",
    unarchived: "{{name}} is back.",
    restored: "{{name}} is restored.",
  },
};

export type WorkspacesHomeCopy = typeof en;

export const vi: WorkspacesHomeCopy = {
  hello: "Xin chào, {{name}}",
  helloAnon: "Chào mừng bạn quay lại",
  summary_one: "Bạn thuộc {{count}} workspace. Chọn nơi để làm việc.",
  summary_other: "Bạn thuộc {{count}} workspace. Chọn nơi để làm việc.",
  summaryNone: "Bạn chưa thuộc workspace nào.",
  yourWorkspaces: "Workspace của bạn",
  create: "Workspace mới",
  createCard: "Tạo hoặc tham gia workspace",
  createHint: "Bắt đầu một workspace mới hoặc tham gia bằng lời mời.",
  loading: "Đang tải các workspace của bạn…",
  loadError: "Không tải được các workspace của bạn.",
  retry: "Thử lại",
  defaultTag: "Mở mặc định",
  makeDefault: "Đặt làm mặc định",
  makeDefaultLabel: "Đặt {{name}} làm workspace mở mặc định",
  open: "Mở",
  openLabel: "Mở {{name}}",
  roles: {
    owner: "Chủ workspace",
    admin: "Quản trị workspace",
    member: "Thành viên",
    guest: "Khách",
  },
  people_one: "{{count}} người",
  people_other: "{{count}} người",
  archivedTitle: "Đã lưu trữ",
  archivedNote: "Chỉ đọc. Khôi phục để làm việc trở lại.",
  archivedTag: "Đã lưu trữ",
  unarchive: "Bỏ lưu trữ",
  unarchiveLabel: "Bỏ lưu trữ workspace {{name}}",
  deletedTitle: "Đã xóa gần đây",
  deletedNote: "Khôi phục trong 30 ngày thì mọi thứ trở lại như cũ.",
  deletedMeta: "Xóa {{date}} · {{left}}",
  daysLeft_one: "còn {{count}} ngày",
  daysLeft_other: "còn {{count}} ngày",
  restore: "Khôi phục",
  restoreLabel: "Khôi phục workspace {{name}}",
  deletedLoadError: "Không tải được các workspace đã xóa gần đây.",
  actionError: "Chưa thực hiện được. Hãy thử lại.",
  restoreError:
    "Không khôi phục được workspace này. Có thể nó đã thay đổi hoặc đã quá 30 ngày.",
  accountLink: "Cài đặt tài khoản",
  emptyTitle: "Chưa có workspace",
  emptyDescription:
    "Tạo một workspace cho nhóm của bạn, hoặc mở đường dẫn lời mời để tham gia.",
  notice: {
    defaultSet: "{{name}} sẽ mở mặc định.",
    unarchived: "{{name}} đã hoạt động trở lại.",
    restored: "Đã khôi phục {{name}}.",
  },
};

export function workspacesHomeCopy(locale: string): WorkspacesHomeCopy {
  return locale === "vi" ? vi : en;
}
