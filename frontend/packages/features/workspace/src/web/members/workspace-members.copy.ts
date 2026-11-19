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
  guestOwnRowOnly:
    "The member list is closed to guests. You see your own membership only.",
  onlyYou: "You're the only member so far.",
  onlyYouAdmin: "You're the only member so far. Invite people to work with.",
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
    unknownHint: "Access status wasn't reported for this member.",
  },
  you: "You",
  roleSelect: "Role for {{name}}",
  roleUpdating: "Updating role for {{name}}…",
  blocked: {
    "last-owner":
      "The only owner. Transfer ownership before changing this role.",
    "owner-by-transfer": "Only an owner can change another owner.",
    "admin-by-owner": "Only an owner can change an admin.",
    "owner-only": "Only an owner can change roles.",
    self: "You can't change your own membership here.",
    "not-admin": "Only admins and owners can change this.",
    suspended: "Reactivate this member to change the role.",
  },
  rowMenu: "Actions for {{name}}",
  actions: {
    suspend: "Suspend access",
    activate: "Reactivate access",
    remove: "Remove from workspace",
    editJobTitle: "Edit job title",
  },
  jobTitle: {
    title: "Job title of {{name}}",
    description:
      "The title shows on their profile and in the member list. It changes no role or permission. At most {{max}} characters.",
    label: "Job title",
    placeholder: "For example, Engineering lead",
    save: "Save",
    saving: "Saving…",
    cancel: "Cancel",
    tooLong:
      "A job title is at most {{max}} characters. Shorten it and save again.",
    invalid: "That job title isn't valid. Remove line breaks and try again.",
    conflict:
      "This profile just changed. Reload to see the latest, then edit the title again.",
    reload: "Reload",
    denied:
      "You don't have permission to edit this person's job title. Only owners and admins can.",
    failed: "The job title wasn't saved. Nothing changed; try again.",
  },
  confirm: {
    removeTitle: "Remove {{name}}?",
    removeDescription:
      "{{name}} loses access to every Board and Doc in {{workspace}}. Their work stays. You can invite them again later.",
    removeAction: "Remove member",
    demoteTitle: "Change {{name}} to {{role}}?",
    demoteDescription:
      "{{name}} will have fewer permissions in {{workspace}}. You can change the role back later.",
    demoteAction: "Change role",
    suspendTitle: "Suspend {{name}}?",
    suspendDescription:
      "{{name}} can't open {{workspace}} until an admin reactivates access. Nothing is deleted.",
    suspendAction: "Suspend access",
    revokeTitle: "Revoke the invitation to {{email}}?",
    revokeDescription:
      "The invitation link to {{workspace}} stops working. You can send a new invitation later.",
    revokeAction: "Revoke invitation",
    cancel: "Cancel",
  },
  invitations: {
    entries: {
      label: "Add on joining",
      hint: "Optional. They join these Teams and Spaces when they accept.",
      add: "Add a Team or Space",
      addLabel: "Choose a Team or Space to add",
      team: "Team",
      space: "Space",
      remove: "Remove {{name}}",
      roleLabel: "Role in {{name}}",
      teamRoles: { member: "Member", lead: "Lead" },
      spaceRoles: { viewer: "Viewer", member: "Member", admin: "Admin" },
      teamsDenied: "You can't add people to Teams, so none are offered.",
      spacesDenied: "You can't add people to Spaces, so none are offered.",
      guestNote: "A Guest can't be a Team lead or a Space admin.",
      refused:
        "A Team or Space you chose can't take this person. Remove it or ask an owner.",
      notActive:
        "{{name}} isn't active any more, so it can't take this person. Remove it or choose another.",
      guestTooPrivileged:
        "A Guest can't have this role in {{name}}. Choose a lower role or remove it.",
    },
    formTitle: "Invite by e-mail",
    emailLabel: "E-mail address",
    emailPlaceholder: "name@company.com",
    emailInvalid: "Enter an e-mail address like name@company.com.",
    alreadyPending:
      "An invitation to {{email}} is already waiting for an answer. Revoke it to send a new one.",
    alreadyMember:
      "{{email}} is already a member of this workspace, so no invitation was sent.",
    guestInvitesOff:
      "Guest invites are off in this workspace. Ask the Owner to turn them on.",
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
    roleOfInvitation: "Role offered in the invitation to {{email}}",
    resend: "Resend invitation to {{email}}",
    resending: "Resending invitation to {{email}}…",
    emailHint:
      "Invite several people at once: separate the addresses with commas, spaces or new lines.",
    emailInvalidMany: "Check these addresses: {{emails}}",
    results: {
      title: "Invitation results",
      summary:
        "{{sent}} of {{total}} invitations sent. Each address is handled on its own.",
      sent: "Sent",
      alreadyMember: "Already a member",
      alreadyPending: "Already has a pending invitation",
      invalid: "Not a valid address or role",
      guestInvitesOff: "Guest invitations are off",
      refused: "Refused",
      done: "Done",
    },
    tooMany: "Invite up to {{max}} people at a time.",
    adminOnly: "Only workspace admins and owners can invite people.",
  },
  error: {
    members: "Members couldn't load. Your access is unchanged.",
    membersForbidden:
      "You can no longer see the member list. Ask a workspace admin if you need it.",
    membersOffline:
      "You're offline, so members couldn't load. Reconnect and try again.",
    invitations: "Invitations couldn't load.",
    retry: "Try again",
  },
  toasts: {
    roleChanged: "{{name}} is now {{role}}.",
    roleFailed: "{{name}}'s role wasn't changed.",
    jobTitleSaved: "{{name}}'s job title was saved.",
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
    invitationRoleChanged: "The invitation to {{email}} now offers {{role}}.",
    invitationRoleFailed:
      "The role in the invitation to {{email}} wasn't changed.",
    resent: "Invitation sent again to {{email}}.",
    resendFailed: "The invitation to {{email}} wasn't sent again.",
    invitedMany: "{{count}} invitations sent.",
    invitedSome: "{{count}} invitations sent. These were not sent: {{emails}}.",
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
  guestOwnRowOnly:
    "Danh sách thành viên không mở cho khách. Bạn chỉ thấy tư cách thành viên của mình.",
  onlyYou: "Hiện chỉ có mình bạn.",
  onlyYouAdmin: "Hiện chỉ có mình bạn. Hãy mời thêm người cùng làm việc.",
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
    unknownHint: "Không có thông tin trạng thái truy cập của thành viên này.",
  },
  you: "Bạn",
  roleSelect: "Vai trò của {{name}}",
  roleUpdating: "Đang cập nhật vai trò của {{name}}…",
  blocked: {
    "last-owner":
      "Chủ sở hữu duy nhất. Hãy chuyển quyền sở hữu trước khi đổi vai trò này.",
    "owner-by-transfer":
      "Chỉ chủ sở hữu mới đổi được vai trò của chủ sở hữu khác.",
    "admin-by-owner": "Chỉ chủ sở hữu mới thay đổi được quản trị viên.",
    "owner-only": "Chỉ chủ sở hữu đổi được vai trò.",
    self: "Bạn không thể tự thay đổi tư cách thành viên của mình ở đây.",
    "not-admin": "Chỉ quản trị viên và chủ sở hữu mới thay đổi được mục này.",
    suspended: "Hãy kích hoạt lại thành viên này để đổi vai trò.",
  },
  rowMenu: "Thao tác cho {{name}}",
  actions: {
    suspend: "Tạm ngưng truy cập",
    activate: "Kích hoạt lại truy cập",
    remove: "Xóa khỏi không gian làm việc",
    editJobTitle: "Sửa chức danh",
  },
  jobTitle: {
    title: "Chức danh của {{name}}",
    description:
      "Chức danh hiện ở hồ sơ và danh sách thành viên. Nó không đổi vai trò hay quyền. Tối đa {{max}} ký tự.",
    label: "Chức danh",
    placeholder: "Ví dụ: Kỹ sư trưởng",
    save: "Lưu",
    saving: "Đang lưu…",
    cancel: "Huỷ",
    tooLong: "Chức danh tối đa {{max}} ký tự. Rút gọn rồi lưu lại.",
    invalid: "Chức danh không hợp lệ. Bỏ các dấu xuống dòng rồi thử lại.",
    conflict:
      "Hồ sơ vừa được người khác sửa. Tải lại để xem bản mới, rồi sửa lại chức danh.",
    reload: "Tải lại",
    denied:
      "Bạn không có quyền sửa chức danh của người này. Chỉ chủ sở hữu và quản trị viên sửa được.",
    failed: "Chưa lưu được chức danh. Chưa có gì thay đổi; hãy thử lại.",
  },
  confirm: {
    removeTitle: "Xóa {{name}}?",
    removeDescription:
      "{{name}} sẽ mất quyền truy cập mọi Bảng và Tài liệu trong {{workspace}}. Công việc của họ vẫn được giữ. Bạn có thể mời lại sau.",
    removeAction: "Xóa thành viên",
    demoteTitle: "Đổi {{name}} thành {{role}}?",
    demoteDescription:
      "{{name}} sẽ có ít quyền hơn trong {{workspace}}. Bạn có thể đổi lại vai trò sau.",
    demoteAction: "Đổi vai trò",
    suspendTitle: "Tạm ngưng {{name}}?",
    suspendDescription:
      "{{name}} sẽ không mở được {{workspace}} cho đến khi quản trị viên kích hoạt lại. Không có gì bị xóa.",
    suspendAction: "Tạm ngưng truy cập",
    revokeTitle: "Thu hồi lời mời gửi {{email}}?",
    revokeDescription:
      "Liên kết mời vào {{workspace}} sẽ ngừng hoạt động. Bạn có thể gửi lời mời mới sau.",
    revokeAction: "Thu hồi lời mời",
    cancel: "Hủy",
  },
  invitations: {
    entries: {
      label: "Vào cùng",
      hint: "Tuỳ chọn. Họ vào các nhóm và phòng ban này khi nhận lời mời.",
      add: "Thêm nhóm hoặc phòng ban",
      addLabel: "Chọn nhóm hoặc phòng ban để thêm",
      team: "Nhóm",
      space: "Phòng ban",
      remove: "Bỏ {{name}}",
      roleLabel: "Vai trò trong {{name}}",
      teamRoles: { member: "Thành viên", lead: "Trưởng nhóm" },
      spaceRoles: {
        viewer: "Người xem",
        member: "Thành viên",
        admin: "Quản trị",
      },
      teamsDenied:
        "Bạn không thêm người vào nhóm được nên không có nhóm nào để chọn.",
      spacesDenied:
        "Bạn không thêm người vào phòng ban được nên không có phòng ban nào để chọn.",
      guestNote: "Khách không làm được trưởng nhóm hay quản trị phòng ban.",
      refused:
        "Nhóm hoặc phòng ban bạn chọn không nhận được người này. Bỏ mục đó hoặc nhờ chủ sở hữu.",
      notActive:
        "{{name}} không còn hoạt động nên không nhận được người này. Bỏ mục đó hoặc chọn mục khác.",
      guestTooPrivileged:
        "Khách không giữ được vai trò này trong {{name}}. Chọn vai trò thấp hơn hoặc bỏ mục đó.",
    },
    formTitle: "Mời qua e-mail",
    emailLabel: "Địa chỉ e-mail",
    emailPlaceholder: "ten@congty.com",
    emailInvalid: "Nhập địa chỉ e-mail dạng ten@congty.com.",
    alreadyPending:
      "Đã có một lời mời tới {{email}} đang chờ trả lời. Thu hồi lời mời đó để gửi lời mời mới.",
    alreadyMember:
      "{{email}} đã là thành viên của không gian làm việc này nên chưa gửi lời mời.",
    guestInvitesOff:
      "Không gian làm việc này đang tắt lời mời khách. Hãy nhờ Chủ sở hữu bật lên.",
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
    roleOfInvitation: "Vai trò trong lời mời gửi {{email}}",
    resend: "Gửi lại lời mời tới {{email}}",
    resending: "Đang gửi lại lời mời tới {{email}}…",
    emailHint:
      "Mời nhiều người một lần: ngăn cách các địa chỉ bằng dấu phẩy, dấu cách hoặc xuống dòng.",
    emailInvalidMany: "Kiểm tra lại các địa chỉ này: {{emails}}",
    results: {
      title: "Kết quả gửi lời mời",
      summary:
        "Đã gửi {{sent}} trên {{total}} lời mời. Mỗi địa chỉ được xử lý riêng.",
      sent: "Đã gửi",
      alreadyMember: "Đã là thành viên",
      alreadyPending: "Đã có lời mời đang chờ",
      invalid: "Địa chỉ hoặc vai trò không hợp lệ",
      guestInvitesOff: "Lời mời khách đang tắt",
      refused: "Bị từ chối",
      done: "Xong",
    },
    tooMany: "Mỗi lần mời tối đa {{max}} người.",
    adminOnly: "Chỉ quản trị viên và chủ sở hữu mới mời được người khác.",
  },
  error: {
    members:
      "Không tải được danh sách thành viên. Quyền truy cập của bạn không đổi.",
    membersForbidden:
      "Bạn không còn xem được danh sách thành viên. Hãy hỏi quản trị viên nếu bạn cần.",
    membersOffline:
      "Bạn đang ngoại tuyến nên chưa tải được thành viên. Hãy kết nối lại rồi thử lại.",
    invitations: "Không tải được lời mời.",
    retry: "Thử lại",
  },
  toasts: {
    roleChanged: "{{name}} giờ là {{role}}.",
    roleFailed: "Chưa đổi được vai trò của {{name}}.",
    jobTitleSaved: "Đã lưu chức danh của {{name}}.",
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
    invitationRoleChanged:
      "Lời mời gửi {{email}} giờ mời với vai trò {{role}}.",
    invitationRoleFailed: "Chưa đổi được vai trò trong lời mời gửi {{email}}.",
    resent: "Đã gửi lại lời mời tới {{email}}.",
    resendFailed: "Chưa gửi lại được lời mời tới {{email}}.",
    invitedMany: "Đã gửi {{count}} lời mời.",
    invitedSome: "Đã gửi {{count}} lời mời. Chưa gửi được tới: {{emails}}.",
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
