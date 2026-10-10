/**
 * Members and invitations copy (WX-MEM-001, WX-DEC-008), vi and en.
 * Registered as the `workspace-members` catalog.
 */
export const en = {
  title: "Members",
  description: "People who can open {{workspace}} and what they can do.",
  descriptionLoading:
    "People who can open this workspace and what they can do.",
  invite: "Invite people",
  tabs: {
    label: "Members and invitations",
    members: "Members",
    invitations: "Invitations",
  },
  roles: {
    owner: "Owner",
    admin: "Admin",
    member: "Member",
    guest: "Guest",
  },
  roleHelp: {
    owner: "Full control, including billing and deleting the workspace.",
    admin: "Manages members, invitations and settings.",
    member: "Creates and edits Boards and Docs.",
    guest: "Works only in what is shared with them.",
  },
  readOnly:
    "Only workspace admins and owners can change roles, suspend or remove members.",
  filter: {
    label: "Filter members by name",
    placeholder: "Filter members…",
    noResults: "No member matches “{{query}}”.",
  },
  columns: {
    member: "Member",
    role: "Role",
    joined: "Joined",
    status: "Access",
    actions: "Actions",
  },
  status: {
    active: "Active",
    suspended: "Suspended",
    unknown: "—",
    unknownHint: "Access status isn't reported yet.",
  },
  you: "You",
  roleSelect: "Role for {{name}}",
  roleUpdating: "Updating role for {{name}}…",
  blocked: {
    "last-owner":
      "The only owner. Transfer ownership before changing this role.",
    "owner-by-transfer": "Only an owner can change another owner.",
    self: "You can't change your own membership here.",
    "not-admin": "Only admins and owners can change this.",
    suspended: "Reactivate this member to change the role.",
  },
  rowMenu: "Actions for {{name}}",
  actions: {
    suspend: "Suspend access",
    activate: "Reactivate access",
    remove: "Remove from workspace",
  },
  confirm: {
    removeTitle: "Remove {{name}}?",
    removeDescription:
      "{{name}} loses access to every Board and Doc in {{workspace}}. Their work stays. You can invite them again later.",
    removeAction: "Remove member",
    suspendTitle: "Suspend {{name}}?",
    suspendDescription:
      "{{name}} can't open {{workspace}} until an admin reactivates access. Nothing is deleted.",
    suspendAction: "Suspend access",
    revokeTitle: "Revoke the invitation to {{email}}?",
    revokeDescription:
      "The invitation link stops working. You can send a new invitation later.",
    revokeAction: "Revoke invitation",
    cancel: "Cancel",
  },
  invitations: {
    formTitle: "Invite by e-mail",
    emailLabel: "E-mail address",
    emailPlaceholder: "name@company.com",
    emailInvalid: "Enter an e-mail address like name@company.com.",
    roleLabel: "Role",
    send: "Send invitation",
    sending: "Sending…",
    pendingTitle: "Pending invitations",
    empty: "No invitations are waiting for an answer.",
    columns: {
      email: "E-mail",
      role: "Role",
      sent: "Sent",
      expires: "Expires",
      actions: "Actions",
    },
    expired: "Expired",
    revoke: "Revoke invitation to {{email}}",
    revoking: "Revoking invitation to {{email}}…",
    adminOnly: "Only workspace admins and owners can invite people.",
  },
  error: {
    members: "Members couldn't load. Your access is unchanged.",
    invitations: "Invitations couldn't load.",
    retry: "Try again",
  },
  toasts: {
    roleChanged: "{{name}} is now {{role}}.",
    roleFailed: "{{name}}'s role wasn't changed.",
    removed: "{{name}} was removed from the workspace.",
    removeFailed: "{{name}} wasn't removed.",
    suspended: "{{name}}'s access is suspended.",
    suspendFailed: "{{name}}'s access wasn't suspended.",
    activated: "{{name}}'s access is active again.",
    activateFailed: "{{name}}'s access wasn't reactivated.",
    invited: "Invitation sent to {{email}}.",
    inviteFailed: "The invitation to {{email}} wasn't sent.",
    revoked: "The invitation to {{email}} was revoked.",
    revokeFailed: "The invitation to {{email}} wasn't revoked.",
    forbidden: "You don't have permission to do this. Ask a workspace owner.",
    conflict:
      "Someone else changed this just now. The list was refreshed; review it and try again.",
    notFound: "It no longer exists. The list was refreshed.",
    network: "The server couldn't be reached. Nothing was changed; try again.",
    unknown:
      "Something went wrong on the server. Nothing was changed; try again.",
  },
};

export type WorkspaceMembersCopy = typeof en;

export const vi: WorkspaceMembersCopy = {
  title: "Thành viên",
  description: "Những người mở được {{workspace}} và quyền của họ.",
  descriptionLoading:
    "Những người mở được không gian làm việc này và quyền của họ.",
  invite: "Mời mọi người",
  tabs: {
    label: "Thành viên và lời mời",
    members: "Thành viên",
    invitations: "Lời mời",
  },
  roles: {
    owner: "Chủ sở hữu",
    admin: "Quản trị viên",
    member: "Thành viên",
    guest: "Khách",
  },
  roleHelp: {
    owner: "Toàn quyền, gồm cả thanh toán và xóa không gian làm việc.",
    admin: "Quản lý thành viên, lời mời và cài đặt.",
    member: "Tạo và chỉnh sửa Bảng, Tài liệu.",
    guest: "Chỉ làm việc trong những gì được chia sẻ.",
  },
  readOnly:
    "Chỉ quản trị viên và chủ sở hữu mới đổi vai trò, tạm ngưng hoặc xóa thành viên được.",
  filter: {
    label: "Lọc thành viên theo tên",
    placeholder: "Lọc thành viên…",
    noResults: "Không có thành viên nào khớp “{{query}}”.",
  },
  columns: {
    member: "Thành viên",
    role: "Vai trò",
    joined: "Tham gia",
    status: "Quyền truy cập",
    actions: "Thao tác",
  },
  status: {
    active: "Đang hoạt động",
    suspended: "Đã tạm ngưng",
    unknown: "—",
    unknownHint: "Chưa có thông tin trạng thái truy cập.",
  },
  you: "Bạn",
  roleSelect: "Vai trò của {{name}}",
  roleUpdating: "Đang cập nhật vai trò của {{name}}…",
  blocked: {
    "last-owner":
      "Chủ sở hữu duy nhất. Hãy chuyển quyền sở hữu trước khi đổi vai trò này.",
    "owner-by-transfer":
      "Chỉ chủ sở hữu mới đổi được vai trò của chủ sở hữu khác.",
    self: "Bạn không thể tự thay đổi tư cách thành viên của mình ở đây.",
    "not-admin": "Chỉ quản trị viên và chủ sở hữu mới thay đổi được mục này.",
    suspended: "Hãy kích hoạt lại thành viên này để đổi vai trò.",
  },
  rowMenu: "Thao tác cho {{name}}",
  actions: {
    suspend: "Tạm ngưng truy cập",
    activate: "Kích hoạt lại truy cập",
    remove: "Xóa khỏi không gian làm việc",
  },
  confirm: {
    removeTitle: "Xóa {{name}}?",
    removeDescription:
      "{{name}} sẽ mất quyền truy cập mọi Bảng và Tài liệu trong {{workspace}}. Công việc của họ vẫn được giữ. Bạn có thể mời lại sau.",
    removeAction: "Xóa thành viên",
    suspendTitle: "Tạm ngưng {{name}}?",
    suspendDescription:
      "{{name}} sẽ không mở được {{workspace}} cho đến khi quản trị viên kích hoạt lại. Không có gì bị xóa.",
    suspendAction: "Tạm ngưng truy cập",
    revokeTitle: "Thu hồi lời mời gửi {{email}}?",
    revokeDescription:
      "Liên kết mời sẽ ngừng hoạt động. Bạn có thể gửi lời mời mới sau.",
    revokeAction: "Thu hồi lời mời",
    cancel: "Hủy",
  },
  invitations: {
    formTitle: "Mời qua e-mail",
    emailLabel: "Địa chỉ e-mail",
    emailPlaceholder: "ten@congty.com",
    emailInvalid: "Nhập địa chỉ e-mail dạng ten@congty.com.",
    roleLabel: "Vai trò",
    send: "Gửi lời mời",
    sending: "Đang gửi…",
    pendingTitle: "Lời mời đang chờ",
    empty: "Không có lời mời nào đang chờ trả lời.",
    columns: {
      email: "E-mail",
      role: "Vai trò",
      sent: "Đã gửi",
      expires: "Hết hạn",
      actions: "Thao tác",
    },
    expired: "Đã hết hạn",
    revoke: "Thu hồi lời mời gửi {{email}}",
    revoking: "Đang thu hồi lời mời gửi {{email}}…",
    adminOnly: "Chỉ quản trị viên và chủ sở hữu mới mời được người khác.",
  },
  error: {
    members:
      "Không tải được danh sách thành viên. Quyền truy cập của bạn không đổi.",
    invitations: "Không tải được lời mời.",
    retry: "Thử lại",
  },
  toasts: {
    roleChanged: "{{name}} giờ là {{role}}.",
    roleFailed: "Chưa đổi được vai trò của {{name}}.",
    removed: "Đã xóa {{name}} khỏi không gian làm việc.",
    removeFailed: "Chưa xóa được {{name}}.",
    suspended: "Đã tạm ngưng truy cập của {{name}}.",
    suspendFailed: "Chưa tạm ngưng được truy cập của {{name}}.",
    activated: "Đã kích hoạt lại truy cập của {{name}}.",
    activateFailed: "Chưa kích hoạt lại được truy cập của {{name}}.",
    invited: "Đã gửi lời mời tới {{email}}.",
    inviteFailed: "Chưa gửi được lời mời tới {{email}}.",
    revoked: "Đã thu hồi lời mời gửi {{email}}.",
    revokeFailed: "Chưa thu hồi được lời mời gửi {{email}}.",
    forbidden:
      "Bạn không có quyền làm việc này. Hãy nhờ chủ sở hữu không gian làm việc.",
    conflict:
      "Ai đó vừa thay đổi mục này. Danh sách đã được làm mới; hãy xem lại rồi thử lại.",
    notFound: "Mục này không còn tồn tại. Danh sách đã được làm mới.",
    network: "Không kết nối được máy chủ. Chưa có gì thay đổi; hãy thử lại.",
    unknown: "Máy chủ gặp sự cố. Chưa có gì thay đổi; hãy thử lại.",
  },
};

export function workspaceMembersCopy(
  locale: "vi" | "en",
): WorkspaceMembersCopy {
  return locale === "en" ? en : vi;
}
