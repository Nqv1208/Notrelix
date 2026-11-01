/**
 * Copy of the "Your workspaces" page of the account, vi and en, registered as
 * the `account-workspaces` catalog (`account-workspaces.catalog.ts`).
 * Placeholders use `{{name}}`; counts use `_one`/`_other`.
 */
export const en = {
  title: "Your workspaces",
  description: "The workspaces you belong to and your role in each.",
  loading: "Loading your workspaces…",
  loadError: "We couldn't load your workspaces.",
  retry: "Try again",
  actionError: "We couldn't do that. Try again.",
  lastOwnerError:
    "You are the only owner of this workspace. Hand the ownership to someone else before you leave.",
  create: "Create or join a workspace",
  defaultTag: "Opens by default",
  archivedTag: "Archived",
  archivedTitle: "Archived workspaces",
  archivedNote:
    "Read-only for everyone. You see these because you own or administer them.",
  unarchive: "Unarchive",
  unarchiveLabel: "Unarchive the workspace {{name}}",
  roles: {
    owner: "Workspace owner",
    admin: "Workspace admin",
    member: "Member",
    guest: "Guest",
  },
  people_one: "{{count}} person",
  people_other: "{{count}} people",
  departments_one: "{{count}} department",
  departments_other: "{{count}} departments",
  open: "Open",
  openLabel: "Open {{name}}",
  makeDefault: "Make default",
  makeDefaultLabel: "Make {{name}} the one that opens by default",
  leave: "Leave workspace…",
  leaveLabel: "Leave the workspace {{name}}…",
  noteSoleOwner:
    "You are the only owner of {{workspace}}. Hand the ownership to someone else before you leave.",
  confirm: {
    title: "Leave {{name}}?",
    text: "You lose access to every department, board and page in {{name}}. Items assigned to you keep their assignee.",
    cancel: "Cancel",
    action: "Leave workspace",
  },
  notice: {
    defaultSet: "{{name}} now opens by default.",
    left: "You left {{name}}.",
    unarchived: "{{name}} is active again.",
  },
};

export type AccountWorkspacesCopy = typeof en;

export const vi: AccountWorkspacesCopy = {
  title: "Workspace của bạn",
  description: "Các workspace bạn đang tham gia và vai trò của bạn ở từng nơi.",
  loading: "Đang tải các workspace của bạn…",
  loadError: "Không thể tải các workspace của bạn.",
  retry: "Thử lại",
  actionError: "Chưa làm được việc này. Hãy thử lại.",
  lastOwnerError:
    "Bạn là chủ sở hữu duy nhất của workspace này. Hãy chuyển quyền chủ sở hữu cho người khác trước khi rời.",
  create: "Tạo hoặc tham gia workspace",
  defaultTag: "Mặc định khi mở ứng dụng",
  archivedTag: "Đã lưu trữ",
  archivedTitle: "Workspace đã lưu trữ",
  archivedNote:
    "Chỉ đọc với mọi người. Bạn thấy các workspace này vì bạn là chủ sở hữu hoặc quản trị.",
  unarchive: "Bỏ lưu trữ",
  unarchiveLabel: "Bỏ lưu trữ workspace {{name}}",
  roles: {
    owner: "Chủ sở hữu workspace",
    admin: "Quản trị workspace",
    member: "Thành viên",
    guest: "Khách",
  },
  people_one: "{{count}} người",
  people_other: "{{count}} người",
  departments_one: "{{count}} phòng ban",
  departments_other: "{{count}} phòng ban",
  open: "Mở",
  openLabel: "Mở {{name}}",
  makeDefault: "Đặt làm mặc định",
  makeDefaultLabel: "Đặt {{name}} làm mặc định khi mở ứng dụng",
  leave: "Rời workspace…",
  leaveLabel: "Rời workspace {{name}}…",
  noteSoleOwner:
    "Bạn là chủ sở hữu duy nhất của {{workspace}}. Hãy chuyển quyền chủ sở hữu cho người khác trước khi rời.",
  confirm: {
    title: "Rời {{name}}?",
    text: "Bạn mất quyền vào mọi phòng ban, board và trang trong {{name}}. Việc đang giao cho bạn vẫn giữ người phụ trách.",
    cancel: "Hủy",
    action: "Rời workspace",
  },
  notice: {
    defaultSet: "{{name}} sẽ là nơi mở mặc định.",
    left: "Bạn đã rời {{name}}.",
    unarchived: "{{name}} đã hoạt động trở lại.",
  },
};

export function accountWorkspacesCopy(
  locale: "vi" | "en",
): AccountWorkspacesCopy {
  return locale === "vi" ? vi : en;
}
