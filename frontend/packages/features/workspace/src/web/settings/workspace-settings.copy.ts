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
    "Only the workspace owner can change these settings. You can see them here.",
  archivedReadOnly:
    "This workspace is archived, so its settings are read-only until the owner unarchives it.",
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
    nameTooLong: "Use 160 characters or fewer.",
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
    timeZonePlaceholder: "Search a time zone, such as Europe/Paris",
    timeZoneInvalid: "Choose a time zone from the list.",
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
      "Archiving makes {{workspace}} read-only for every member and stops all work in it. An owner can restore it later.",
    archiveAction: "Archive workspace",
    unarchiveTitle: "This workspace is archived",
    unarchiveDescription:
      "{{workspace}} is read-only for every member and its automations are paused. Unarchive it to let everyone work again.",
    unarchiveAction: "Unarchive workspace",
    unarchiving: "Unarchiving…",
    unarchiveConfirmTitle: "Unarchive {{workspace}}?",
    unarchiveConfirmDescription:
      "Everyone can edit and comment again. Automations and integration sync run again from now on.",
    unarchiveOwnerOnly: "Only the owner can unarchive this workspace.",
    confirmTitle: "Archive {{workspace}}?",
    confirmDescription:
      "Every member can still read this workspace, but nothing in it can change until an owner restores it. Type the workspace name to confirm.",
    confirmLabel: "Workspace name",
    confirmMismatch: "Type {{workspace}} exactly to continue.",
    confirmAction: "Archive workspace",
    archiving: "Archiving…",
    cancel: "Cancel",
    deleteTitle: "Delete this workspace",
    deleteDescription:
      "Deleting {{workspace}} cuts every member off at once. Its content is kept for 30 days, and you can restore it from Recently deleted in your account.",
    deleteAction: "Delete workspace",
    deleteConfirmTitle: "Delete {{workspace}}?",
    deleteConfirmDescription:
      "Every member loses access now, including you. After 30 days the workspace and everything in it is erased for good. Type the workspace name to confirm.",
    deleteConfirmAction: "Delete workspace",
    deleting: "Deleting…",
    deletePersonal: "A personal workspace can't be deleted.",
    transferTitle: "Transfer ownership",
    transferDescription:
      "Choose a member to become the owner of {{workspace}}. You become an admin.",
    transferAction: "Transfer ownership",
    transferDialogTitle: "Transfer ownership of {{workspace}}",
    transferPick: "New owner",
    transferEffect:
      "{{name}} becomes the owner and can manage the plan and delete the workspace. You become an admin and can't take ownership back yourself.",
    transferConfirm: "Transfer to {{name}}",
    transferPending: "Transferring…",
    transferNoneTitle: "There is nobody to hand the workspace to yet",
    transferNoneBody:
      "Only an active member who isn't a guest can become the owner. Add or invite a member first.",
    transferInvite: "Invite a member",
    transferConflictTitle: "The member list has changed.",
    transferConflictBody:
      "The workspace or the person you chose changed since you opened this. Reload the list and choose again.",
    transferRejected:
      "That person can't become the owner. Choose another member.",
    transferReload: "Reload the list",
    transferRoleAdmin: "Admin",
    transferRoleMember: "Member",
    stepUpTitle: "Confirm it's you",
    stepUpText:
      "Handing {{workspace}} to {{name}} needs you to confirm it's you, even though you're signed in.",
    stepUpCodeLabel: "6-digit code from your authenticator app",
    stepUpUseRecovery: "Use a recovery code",
    stepUpUseCode: "Use the authenticator code",
    stepUpRecoveryLabel: "Recovery code",
    stepUpPasswordLabel: "Current password",
    stepUpConfirm: "Confirm and transfer",
    stepUpChecking: "Confirming…",
    stepUpWrongCode: "That code isn't right. Check it and try again.",
    stepUpWrongPassword: "That password isn't right. Try again.",
    stepUpWait: "Too many tries. Wait {{seconds}} seconds, then try again.",
    stepUpExpiredTitle: "The confirmation expired.",
    stepUpExpiredBody:
      "A confirmation lasts 5 minutes. Start again to hand the workspace over.",
    stepUpRestart: "Start again",
    stepUpClose: "Close",
    stepUpRefusedTitle: "Ownership was not transferred.",
    stepUpRefusedBody:
      "The confirmation expired or was already used. Confirm again to transfer ownership.",
    stepUpProviderTitle: "Add a way to confirm first",
    stepUpProviderBody:
      "You sign in with a provider and have no password or two-factor yet. Add one in Account settings, Security, then come back to hand the workspace over.",
    stepUpProviderText:
      "Confirm with the account you signed in with. The browser leaves for it and comes back here, where you press Transfer to finish the handover.",
    stepUpProviderBack:
      "Your provider confirmed you. Ownership of {{workspace}} moves to {{name}} only when you press Transfer.",
    stepUpProviderButton: "Confirm with {{provider}}",
    stepUpProviderPending: "Confirming…",
    stepUpProviderFailed:
      "The provider could not confirm you. Ownership was not transferred; try again or close.",
  },
  toasts: {
    profileSaved: "Profile saved.",
    defaultsSaved: "Defaults saved.",
    saveFailed: "Your changes weren't saved.",
    archived: "{{workspace}} was archived.",
    unarchived: "{{workspace}} was unarchived.",
    unarchiveFailed: "{{workspace}} wasn't unarchived.",
    unarchiveConflict:
      "{{workspace}} changed since you opened this page. Reload and try again.",
    archiveFailed: "{{workspace}} wasn't archived.",
    archiveConflict:
      "{{workspace}} changed since you opened this page. Reload and try again.",
    deleted:
      "{{workspace}} was deleted. You can restore it from Recently deleted for 30 days.",
    deleteFailed: "{{workspace}} wasn't deleted.",
    deleteConflict:
      "{{workspace}} changed since you opened this page. Reload and try again.",
    deletePersonal: "A personal workspace can't be deleted.",
    transferred:
      "{{name}} is now the owner of {{workspace}}. You are an admin.",
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
    "Chỉ chủ sở hữu workspace mới thay đổi được các cài đặt này. Bạn có thể xem chúng tại đây.",
  archivedReadOnly:
    "Không gian làm việc này đang được lưu trữ nên cài đặt chỉ xem được cho đến khi chủ sở hữu bỏ lưu trữ.",
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
    nameTooLong: "Dùng tối đa 160 ký tự.",
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
    timeZonePlaceholder: "Tìm múi giờ, ví dụ Europe/Paris",
    timeZoneInvalid: "Hãy chọn một múi giờ trong danh sách.",
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
      "Lưu trữ sẽ chuyển {{workspace}} sang chỉ đọc với mọi thành viên và dừng mọi công việc trong đó. Chủ sở hữu có thể khôi phục sau.",
    archiveAction: "Lưu trữ không gian làm việc",
    unarchiveTitle: "Không gian làm việc này đang được lưu trữ",
    unarchiveDescription:
      "{{workspace}} chỉ xem được với mọi thành viên và tự động hoá đã tạm dừng. Bỏ lưu trữ để mọi người làm việc lại.",
    unarchiveAction: "Bỏ lưu trữ không gian làm việc",
    unarchiving: "Đang bỏ lưu trữ…",
    unarchiveConfirmTitle: "Bỏ lưu trữ {{workspace}}?",
    unarchiveConfirmDescription:
      "Mọi người sửa và bình luận lại được. Tự động hoá và đồng bộ tích hợp chạy lại từ lúc này.",
    unarchiveOwnerOnly:
      "Chỉ chủ sở hữu mới bỏ lưu trữ được không gian làm việc này.",
    confirmTitle: "Lưu trữ {{workspace}}?",
    confirmDescription:
      "Mọi thành viên vẫn xem được không gian làm việc này nhưng không ai chỉnh sửa được cho đến khi chủ sở hữu khôi phục. Nhập tên không gian làm việc để xác nhận.",
    confirmLabel: "Tên không gian làm việc",
    confirmMismatch: "Nhập chính xác {{workspace}} để tiếp tục.",
    confirmAction: "Lưu trữ không gian làm việc",
    archiving: "Đang lưu trữ…",
    cancel: "Hủy",
    deleteTitle: "Xóa không gian làm việc này",
    deleteDescription:
      "Xóa {{workspace}} sẽ ngắt quyền của mọi thành viên ngay lập tức. Nội dung được giữ 30 ngày và bạn có thể khôi phục trong mục Đã xóa gần đây của tài khoản.",
    deleteAction: "Xóa không gian làm việc",
    deleteConfirmTitle: "Xóa {{workspace}}?",
    deleteConfirmDescription:
      "Mọi thành viên, kể cả bạn, mất quyền truy cập ngay bây giờ. Sau 30 ngày, không gian làm việc và mọi thứ trong đó bị xóa vĩnh viễn. Nhập tên không gian làm việc để xác nhận.",
    deleteConfirmAction: "Xóa không gian làm việc",
    deleting: "Đang xóa…",
    deletePersonal: "Không thể xóa không gian làm việc cá nhân.",
    transferTitle: "Chuyển quyền sở hữu",
    transferDescription:
      "Chọn một thành viên làm Chủ sở hữu mới của {{workspace}}. Bạn trở thành Quản trị.",
    transferAction: "Chuyển quyền",
    transferDialogTitle: "Chuyển quyền sở hữu {{workspace}}",
    transferPick: "Chủ sở hữu mới",
    transferEffect:
      "{{name}} thành Chủ sở hữu, quản lý được gói và xóa được không gian làm việc. Bạn thành Quản trị và không tự lấy lại quyền được.",
    transferConfirm: "Chuyển cho {{name}}",
    transferPending: "Đang chuyển…",
    transferNoneTitle: "Chưa có ai để nhận quyền sở hữu",
    transferNoneBody:
      "Chỉ thành viên đang hoạt động và không phải Khách mới nhận được quyền Chủ sở hữu. Hãy thêm hoặc mời một thành viên trước.",
    transferInvite: "Mời thành viên",
    transferConflictTitle: "Danh sách thành viên đã thay đổi.",
    transferConflictBody:
      "Không gian làm việc hoặc người bạn chọn đã thay đổi từ lúc bạn mở hộp thoại. Tải lại danh sách rồi chọn lại.",
    transferRejected:
      "Người này không nhận được quyền Chủ sở hữu. Hãy chọn người khác.",
    transferReload: "Tải lại danh sách",
    transferRoleAdmin: "Quản trị",
    transferRoleMember: "Thành viên",
    stepUpTitle: "Xác nhận lại danh tính",
    stepUpText:
      "Chuyển quyền sở hữu {{workspace}} cho {{name}} cần xác nhận lại danh tính, dù bạn đã đăng nhập.",
    stepUpCodeLabel: "Mã 6 số từ ứng dụng xác thực",
    stepUpUseRecovery: "Dùng mã khôi phục",
    stepUpUseCode: "Dùng mã từ ứng dụng xác thực",
    stepUpRecoveryLabel: "Mã khôi phục",
    stepUpPasswordLabel: "Mật khẩu hiện tại",
    stepUpConfirm: "Xác nhận và chuyển quyền",
    stepUpChecking: "Đang xác nhận…",
    stepUpWrongCode: "Mã chưa đúng. Kiểm tra lại rồi thử tiếp.",
    stepUpWrongPassword: "Mật khẩu chưa đúng. Hãy thử lại.",
    stepUpWait: "Thử quá nhiều lần. Đợi {{seconds}} giây rồi thử lại.",
    stepUpExpiredTitle: "Yêu cầu xác nhận đã hết hạn.",
    stepUpExpiredBody:
      "Phiên xác nhận chỉ có hiệu lực 5 phút. Bắt đầu lại để chuyển quyền.",
    stepUpRestart: "Bắt đầu lại",
    stepUpClose: "Đóng",
    stepUpRefusedTitle: "Chưa chuyển quyền.",
    stepUpRefusedBody:
      "Xác nhận đã hết hạn hoặc đã được dùng. Hãy xác nhận lại để chuyển quyền.",
    stepUpProviderTitle: "Thêm cách xác nhận trước",
    stepUpProviderBody:
      "Bạn đăng nhập bằng nhà cung cấp và chưa có mật khẩu hay xác thực hai bước. Hãy thêm một trong hai ở Cài đặt tài khoản, mục Bảo mật, rồi quay lại để chuyển quyền.",
    stepUpProviderText:
      "Hãy xác nhận bằng tài khoản bạn đã dùng để đăng nhập. Trình duyệt rời đi rồi quay lại đây, nơi bạn bấm Chuyển để hoàn tất việc chuyển quyền.",
    stepUpProviderBack:
      "Nhà cung cấp đã xác nhận bạn. Quyền sở hữu {{workspace}} chỉ chuyển cho {{name}} khi bạn bấm Chuyển.",
    stepUpProviderButton: "Xác nhận với {{provider}}",
    stepUpProviderPending: "Đang xác nhận…",
    stepUpProviderFailed:
      "Nhà cung cấp không xác nhận được. Bạn chưa chuyển quyền; thử lại hoặc đóng.",
  },
  toasts: {
    profileSaved: "Đã lưu hồ sơ.",
    defaultsSaved: "Đã lưu mặc định.",
    saveFailed: "Chưa lưu được thay đổi.",
    archived: "Đã lưu trữ {{workspace}}.",
    unarchived: "Đã bỏ lưu trữ {{workspace}}.",
    unarchiveFailed: "Chưa bỏ lưu trữ được {{workspace}}.",
    unarchiveConflict:
      "{{workspace}} đã thay đổi từ lúc bạn mở trang. Hãy tải lại rồi thử lại.",
    archiveFailed: "Chưa lưu trữ được {{workspace}}.",
    archiveConflict:
      "{{workspace}} đã thay đổi từ khi bạn mở trang này. Hãy tải lại rồi thử lại.",
    deleted:
      "Đã xóa {{workspace}}. Bạn có thể khôi phục trong mục Đã xóa gần đây trong 30 ngày.",
    deleteFailed: "Chưa xóa được {{workspace}}.",
    deleteConflict:
      "{{workspace}} đã thay đổi từ khi bạn mở trang này. Hãy tải lại rồi thử lại.",
    deletePersonal: "Không thể xóa không gian làm việc cá nhân.",
    transferred:
      "{{name}} giờ là Chủ sở hữu của {{workspace}}. Bạn là Quản trị.",
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
