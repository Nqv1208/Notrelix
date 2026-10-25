/**
 * Workspace settings copy (WX-SET-001, WX-DEC-008), vi and en.
 * Registered as the `workspace-settings` catalog.
 */
export const en = {
  title: "Settings",
  description:
    "How {{workspace}} is named, set up for new people, and retired.",
  descriptionLoading:
    "How this workspace is named, set up for new people, and retired.",
  readOnly:
    "Only workspace admins and owners can change these settings. You can see them here.",
  versionUnavailable:
    "Saving is turned off for now: this server doesn't report the workspace version that is needed to save changes safely.",
  save: "Save changes",
  saving: "Saving…",
  saved: "Saved",
  discard: "Discard changes",
  reload: "Load the latest",
  retry: "Try again",
  loadError: "These settings couldn't load. Nothing was changed.",
  conflict:
    "Someone changed these settings while you were editing. Load the latest version, then apply your edits again.",
  profile: {
    title: "Profile",
    description: "The name and description people see across the workspace.",
    nameLabel: "Workspace name",
    nameHelp: "Shown in the sidebar, switcher and invitations.",
    nameRequired: "Enter a name for the workspace.",
    nameTooLong: "Use 200 characters or fewer.",
    descriptionLabel: "Description",
    descriptionHelp: "What this workspace is for. Up to 1,024 characters.",
    descriptionPlaceholder: "e.g. Planning and delivery for the product team",
    descriptionTooLong: "Use 1,024 characters or fewer.",
    counter: "{{count}} / {{max}}",
  },
  defaults: {
    title: "Defaults",
    description: "What new members and invitations start with.",
    roleLabel: "Default role for new members",
    roleHelp: "Used when someone joins without a chosen role.",
    expiryLabel: "Invitations expire after (days)",
    expiryHelp: "Between 1 and 30 days.",
    expiryInvalid: "Enter a whole number of days from 1 to 30.",
    guestsLabel: "Allow inviting guests",
    guestsHelp: "Guests work only in the Boards and Docs shared with them.",
    sharingLabel: "Allow public links",
    sharingHelp:
      "Members can share a read-only link to a Board or Doc with anyone.",
    mfaLabel: "Require two-step verification",
    mfaHelp:
      "Members must turn on two-step verification to open this workspace.",
    timeZoneLabel: "Time zone",
    timeZoneHelp: "Every board of the workspace uses it for dates and times.",
    weekStartLabel: "First day of the week",
  },
  timeZones: {
    "Asia/Ho_Chi_Minh": "(GMT+7) Hanoi, Ho Chi Minh City",
    "Asia/Singapore": "(GMT+8) Singapore",
    "Asia/Tokyo": "(GMT+9) Tokyo",
  },
  weekDays: {
    monday: "Monday",
    sunday: "Sunday",
  },
  roles: {
    admin: "Admin",
    member: "Member",
    guest: "Guest",
  },
  danger: {
    title: "Danger zone",
    description: "Actions that take the workspace away from everyone.",
    archiveTitle: "Archive this workspace",
    archiveDescription:
      "Archiving hides {{workspace}} from every member and stops all work in it. An owner can restore it later.",
    archiveAction: "Archive workspace",
    confirmTitle: "Archive {{workspace}}?",
    confirmDescription:
      "Every member loses access until an owner restores the workspace. Type the workspace name to confirm.",
    confirmLabel: "Workspace name",
    confirmMismatch: "Type {{workspace}} exactly to continue.",
    confirmAction: "Archive workspace",
    archiving: "Archiving…",
    cancel: "Cancel",
  },
  toasts: {
    profileSaved: "Profile saved.",
    defaultsSaved: "Defaults saved.",
    saveFailed: "Your changes weren't saved.",
    archived: "{{workspace}} was archived.",
    archiveFailed: "{{workspace}} wasn't archived.",
    forbidden: "You don't have permission to change these settings.",
    network: "The server couldn't be reached. Nothing was changed; try again.",
    unknown:
      "Something went wrong on the server. Nothing was changed; try again.",
  },
};

export type WorkspaceSettingsCopy = typeof en;

export const vi: WorkspaceSettingsCopy = {
  title: "Cài đặt",
  description: "Tên, thiết lập cho người mới và việc lưu trữ {{workspace}}.",
  descriptionLoading:
    "Tên, thiết lập cho người mới và việc lưu trữ không gian làm việc này.",
  readOnly:
    "Chỉ quản trị viên và chủ sở hữu mới thay đổi được các cài đặt này. Bạn có thể xem chúng tại đây.",
  versionUnavailable:
    "Tạm thời chưa lưu được: máy chủ này chưa cung cấp phiên bản của không gian làm việc, thông tin cần để lưu thay đổi an toàn.",
  save: "Lưu thay đổi",
  saving: "Đang lưu…",
  saved: "Đã lưu",
  discard: "Hủy thay đổi",
  reload: "Tải bản mới nhất",
  retry: "Thử lại",
  loadError: "Không tải được các cài đặt này. Chưa có gì thay đổi.",
  conflict:
    "Ai đó đã thay đổi các cài đặt này trong lúc bạn chỉnh sửa. Hãy tải bản mới nhất rồi áp dụng lại chỉnh sửa của bạn.",
  profile: {
    title: "Hồ sơ",
    description: "Tên và mô tả mọi người thấy trong không gian làm việc.",
    nameLabel: "Tên không gian làm việc",
    nameHelp: "Hiển thị ở thanh bên, bộ chuyển đổi và lời mời.",
    nameRequired: "Nhập tên cho không gian làm việc.",
    nameTooLong: "Dùng tối đa 200 ký tự.",
    descriptionLabel: "Mô tả",
    descriptionHelp:
      "Không gian làm việc này dùng để làm gì. Tối đa 1.024 ký tự.",
    descriptionPlaceholder: "VD: Lập kế hoạch và triển khai cho nhóm sản phẩm",
    descriptionTooLong: "Dùng tối đa 1.024 ký tự.",
    counter: "{{count}} / {{max}}",
  },
  defaults: {
    title: "Mặc định",
    description: "Thiết lập ban đầu cho thành viên mới và lời mời.",
    roleLabel: "Vai trò mặc định cho thành viên mới",
    roleHelp: "Dùng khi ai đó tham gia mà chưa được chọn vai trò.",
    expiryLabel: "Lời mời hết hạn sau (ngày)",
    expiryHelp: "Từ 1 đến 30 ngày.",
    expiryInvalid: "Nhập số ngày nguyên từ 1 đến 30.",
    guestsLabel: "Cho phép mời khách",
    guestsHelp: "Khách chỉ làm việc trong các Bảng và Tài liệu được chia sẻ.",
    sharingLabel: "Cho phép liên kết công khai",
    sharingHelp:
      "Thành viên có thể chia sẻ liên kết chỉ xem của một Bảng hoặc Tài liệu cho bất kỳ ai.",
    mfaLabel: "Bắt buộc xác minh hai bước",
    mfaHelp:
      "Thành viên phải bật xác minh hai bước để mở không gian làm việc này.",
    timeZoneLabel: "Múi giờ",
    timeZoneHelp:
      "Mọi bảng của không gian làm việc dùng múi giờ này cho ngày và giờ.",
    weekStartLabel: "Ngày bắt đầu tuần",
  },
  timeZones: {
    "Asia/Ho_Chi_Minh": "(GMT+7) Hà Nội, TP. Hồ Chí Minh",
    "Asia/Singapore": "(GMT+8) Singapore",
    "Asia/Tokyo": "(GMT+9) Tokyo",
  },
  weekDays: {
    monday: "Thứ Hai",
    sunday: "Chủ nhật",
  },
  roles: {
    admin: "Quản trị viên",
    member: "Thành viên",
    guest: "Khách",
  },
  danger: {
    title: "Vùng nguy hiểm",
    description:
      "Các thao tác khiến mọi người mất quyền vào không gian làm việc.",
    archiveTitle: "Lưu trữ không gian làm việc này",
    archiveDescription:
      "Lưu trữ sẽ ẩn {{workspace}} với mọi thành viên và dừng mọi công việc trong đó. Chủ sở hữu có thể khôi phục sau.",
    archiveAction: "Lưu trữ không gian làm việc",
    confirmTitle: "Lưu trữ {{workspace}}?",
    confirmDescription:
      "Mọi thành viên sẽ mất quyền truy cập cho đến khi chủ sở hữu khôi phục. Nhập tên không gian làm việc để xác nhận.",
    confirmLabel: "Tên không gian làm việc",
    confirmMismatch: "Nhập chính xác {{workspace}} để tiếp tục.",
    confirmAction: "Lưu trữ không gian làm việc",
    archiving: "Đang lưu trữ…",
    cancel: "Hủy",
  },
  toasts: {
    profileSaved: "Đã lưu hồ sơ.",
    defaultsSaved: "Đã lưu mặc định.",
    saveFailed: "Chưa lưu được thay đổi.",
    archived: "Đã lưu trữ {{workspace}}.",
    archiveFailed: "Chưa lưu trữ được {{workspace}}.",
    forbidden: "Bạn không có quyền thay đổi các cài đặt này.",
    network: "Không kết nối được máy chủ. Chưa có gì thay đổi; hãy thử lại.",
    unknown: "Máy chủ gặp sự cố. Chưa có gì thay đổi; hãy thử lại.",
  },
};

export function workspaceSettingsCopy(
  locale: "vi" | "en",
): WorkspaceSettingsCopy {
  return locale === "en" ? en : vi;
}
