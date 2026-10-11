/**
 * Teams copy (WX-TEAM-001, GOV-026), vi and en.
 * Registered as the `workspace-teams` catalog.
 */
export const en = {
  nav: { teams: "Teams" },
  list: {
    title: "Teams",
    description:
      "Groups of people that Spaces and sharing can address together.",
    new: "New Team",
    filterLabel: "Show Teams",
    filter: { active: "Active", archived: "Archived" },
    members_one: "{{count}} member",
    members_other: "{{count}} members",
    archived: "Archived",
    loading: "Loading Teams",
    empty: {
      title: "No Teams yet",
      description:
        "A Team groups people so you can add them to a Space in one step.",
      archivedTitle: "No archived Teams",
      archivedDescription: "Teams you archive are kept here.",
    },
    error: "Teams couldn't load. Nothing was changed.",
    retry: "Try again",
  },
  create: {
    title: "New Team",
    description:
      "Name the Team. You become its first Lead and can add people next.",
    nameLabel: "Team name",
    namePlaceholder: "e.g. Platform engineering",
    nameRequired: "Enter a name for the Team.",
    nameTooLong: "A Team name can have at most 160 characters.",
    descriptionLabel: "Description",
    descriptionHelp: "Optional. What this Team is for.",
    descriptionTooLong: "A description can have at most 1024 characters.",
    submit: "Create Team",
    submitting: "Creating…",
    cancel: "Cancel",
  },
  team: {
    archivedBadge: "Archived",
    details: {
      title: "Details",
      description: "The Team's name and what it is for.",
    },
    nameLabel: "Team name",
    nameRequired: "Enter a name for the Team.",
    nameTooLong: "A Team name can have at most 160 characters.",
    rename: "Rename",
    descriptionLabel: "Description",
    descriptionHelp: "Optional. Shown to everyone who can see the Team.",
    descriptionTooLong: "A description can have at most 1024 characters.",
    noDescription: "No description.",
    saveDescription: "Save description",
    saving: "Saving…",
    loading: "Loading Team",
    error: "This Team couldn't load. Nothing was changed.",
    retry: "Try again",
    backToTeams: "Back to Teams",
  },
  members: {
    title: "Members",
    description: "Leads manage the Team's work; members take part in it.",
    count_one: "{{count}} member",
    count_other: "{{count}} members",
    loading: "Loading Team members",
    empty: "This Team has no members to show.",
    error: "Members couldn't load. Nothing was changed.",
    retry: "Try again",
    you: "You",
    roleSelect: "Team role for {{name}}",
    roleUpdating: "Updating Team role for {{name}}…",
    remove: "Remove {{name}} from the Team",
    removing: "Removing {{name}}…",
    add: {
      title: "Add a member",
      filterLabel: "Find a workspace member",
      filterPlaceholder: "Find a person…",
      userLabel: "Person",
      userPlaceholder: "Choose a person",
      roleLabel: "Team role",
      submit: "Add to Team",
      submitting: "Adding…",
      required: "Choose a person to add.",
      noCandidates: "Everyone in the workspace is already in this Team.",
      noMatch: "No one matches “{{query}}”.",
    },
  },
  roles: { Lead: "Lead", Member: "Member" },
  roleHelp: {
    Lead: "Manages the Team's members and keeps it up to date.",
    Member: "Takes part in the Team's work.",
  },
  readOnly:
    "Only workspace admins and owners can change this Team. You can see who is in it.",
  archivedReason:
    "This Team is archived. Unarchive it to add or remove members or change roles.",
  blocked: {
    lastLead:
      "The only Lead. Make someone else a Lead before changing this role.",
    archived: "Archived Teams can't be changed.",
    notAdmin: "Only workspace admins and owners can change this.",
  },
  confirmRemove: {
    title: "Remove {{name}} from {{team}}?",
    description:
      "{{name}} stops receiving what is shared with {{team}}. They stay in the workspace and you can add them again later.",
    action: "Remove from Team",
    cancel: "Cancel",
  },
  danger: {
    title: "Archive",
    description:
      "An archived Team keeps its history but its membership is frozen. You can unarchive it at any time.",
    archivedDescription:
      "This Team is archived. Unarchive it to change its members again.",
    archive: "Archive Team",
    archiving: "Archiving…",
    unarchive: "Unarchive Team",
    unarchiving: "Unarchiving…",
  },
  confirmArchive: {
    title: "Archive {{team}}?",
    description:
      "Its members and roles are kept but can't be changed until you unarchive it. Nothing is deleted.",
    action: "Archive Team",
    unarchiveTitle: "Unarchive {{team}}?",
    unarchiveDescription:
      "Its membership can be changed again and it returns to the active list.",
    unarchiveAction: "Unarchive Team",
    cancel: "Cancel",
  },
  errors: {
    conflict:
      "Someone else changed this Team just now. It was refreshed; review it and try again.",
    forbidden:
      "You don't have permission to do this. Ask a workspace admin or owner.",
    notFound: "This Team no longer exists. The list was refreshed.",
    network: "The server couldn't be reached. Nothing was changed; try again.",
    unknown:
      "Something went wrong on the server. Nothing was changed; try again.",
  },
  toasts: {
    created: "{{name}} was created.",
    renamed: "The Team is now called {{name}}.",
    described: "The description of {{name}} was saved.",
    archived: "{{name}} was archived.",
    unarchived: "{{name}} was unarchived.",
    memberAdded: "{{name}} was added to the Team.",
    memberRemoved: "{{name}} was removed from the Team.",
    roleChanged: "{{name}} is now {{role}}.",
  },
};

export type WorkspaceTeamsCopy = typeof en;

export const vi: WorkspaceTeamsCopy = {
  nav: { teams: "Nhóm" },
  list: {
    title: "Nhóm",
    description:
      "Các nhóm người mà Không gian và chia sẻ có thể nhắm tới cùng lúc.",
    new: "Nhóm mới",
    filterLabel: "Hiển thị nhóm",
    filter: { active: "Đang hoạt động", archived: "Đã lưu trữ" },
    members_one: "{{count}} thành viên",
    members_other: "{{count}} thành viên",
    archived: "Đã lưu trữ",
    loading: "Đang tải danh sách nhóm",
    empty: {
      title: "Chưa có nhóm nào",
      description:
        "Nhóm gom mọi người lại để bạn thêm họ vào một Không gian chỉ bằng một bước.",
      archivedTitle: "Không có nhóm nào đã lưu trữ",
      archivedDescription: "Những nhóm bạn lưu trữ sẽ nằm ở đây.",
    },
    error: "Không tải được danh sách nhóm. Chưa có gì thay đổi.",
    retry: "Thử lại",
  },
  create: {
    title: "Nhóm mới",
    description:
      "Đặt tên cho nhóm. Bạn là Trưởng nhóm đầu tiên và có thể thêm người ngay sau đó.",
    nameLabel: "Tên nhóm",
    namePlaceholder: "Ví dụ: Kỹ thuật nền tảng",
    nameRequired: "Hãy nhập tên cho nhóm.",
    nameTooLong: "Tên nhóm tối đa 160 ký tự.",
    descriptionLabel: "Mô tả",
    descriptionHelp: "Không bắt buộc. Nhóm này dùng để làm gì.",
    descriptionTooLong: "Mô tả tối đa 1024 ký tự.",
    submit: "Tạo nhóm",
    submitting: "Đang tạo…",
    cancel: "Hủy",
  },
  team: {
    archivedBadge: "Đã lưu trữ",
    details: {
      title: "Chi tiết",
      description: "Tên nhóm và mục đích của nhóm.",
    },
    nameLabel: "Tên nhóm",
    nameRequired: "Hãy nhập tên cho nhóm.",
    nameTooLong: "Tên nhóm tối đa 160 ký tự.",
    rename: "Đổi tên",
    descriptionLabel: "Mô tả",
    descriptionHelp: "Không bắt buộc. Hiển thị cho mọi người thấy được nhóm.",
    descriptionTooLong: "Mô tả tối đa 1024 ký tự.",
    noDescription: "Chưa có mô tả.",
    saveDescription: "Lưu mô tả",
    saving: "Đang lưu…",
    loading: "Đang tải nhóm",
    error: "Không tải được nhóm này. Chưa có gì thay đổi.",
    retry: "Thử lại",
    backToTeams: "Quay lại danh sách nhóm",
  },
  members: {
    title: "Thành viên",
    description:
      "Trưởng nhóm điều phối công việc của nhóm; thành viên cùng tham gia.",
    count_one: "{{count}} thành viên",
    count_other: "{{count}} thành viên",
    loading: "Đang tải thành viên nhóm",
    empty: "Nhóm này chưa có thành viên nào để hiển thị.",
    error: "Không tải được thành viên. Chưa có gì thay đổi.",
    retry: "Thử lại",
    you: "Bạn",
    roleSelect: "Vai trò trong nhóm của {{name}}",
    roleUpdating: "Đang cập nhật vai trò trong nhóm của {{name}}…",
    remove: "Xóa {{name}} khỏi nhóm",
    removing: "Đang xóa {{name}}…",
    add: {
      title: "Thêm thành viên",
      filterLabel: "Tìm thành viên của không gian làm việc",
      filterPlaceholder: "Tìm một người…",
      userLabel: "Người",
      userPlaceholder: "Chọn một người",
      roleLabel: "Vai trò trong nhóm",
      submit: "Thêm vào nhóm",
      submitting: "Đang thêm…",
      required: "Hãy chọn một người để thêm.",
      noCandidates:
        "Mọi người trong không gian làm việc đều đã ở trong nhóm này.",
      noMatch: "Không có ai khớp “{{query}}”.",
    },
  },
  roles: { Lead: "Trưởng nhóm", Member: "Thành viên" },
  roleHelp: {
    Lead: "Quản lý thành viên của nhóm và giữ thông tin nhóm luôn cập nhật.",
    Member: "Tham gia công việc của nhóm.",
  },
  readOnly:
    "Chỉ quản trị viên và chủ sở hữu mới thay đổi được nhóm này. Bạn vẫn xem được ai ở trong nhóm.",
  archivedReason:
    "Nhóm này đã được lưu trữ. Hãy bỏ lưu trữ để thêm, xóa thành viên hoặc đổi vai trò.",
  blocked: {
    lastLead:
      "Trưởng nhóm duy nhất. Hãy chọn một Trưởng nhóm khác trước khi đổi vai trò này.",
    archived: "Không thể thay đổi nhóm đã lưu trữ.",
    notAdmin: "Chỉ quản trị viên và chủ sở hữu mới thay đổi được mục này.",
  },
  confirmRemove: {
    title: "Xóa {{name}} khỏi {{team}}?",
    description:
      "{{name}} sẽ không còn nhận những gì được chia sẻ cho {{team}}. Họ vẫn ở trong không gian làm việc và bạn có thể thêm lại sau.",
    action: "Xóa khỏi nhóm",
    cancel: "Hủy",
  },
  danger: {
    title: "Lưu trữ",
    description:
      "Nhóm đã lưu trữ vẫn giữ lịch sử nhưng không đổi được thành viên. Bạn có thể bỏ lưu trữ bất cứ lúc nào.",
    archivedDescription:
      "Nhóm này đã được lưu trữ. Hãy bỏ lưu trữ để đổi thành viên trở lại.",
    archive: "Lưu trữ nhóm",
    archiving: "Đang lưu trữ…",
    unarchive: "Bỏ lưu trữ nhóm",
    unarchiving: "Đang bỏ lưu trữ…",
  },
  confirmArchive: {
    title: "Lưu trữ {{team}}?",
    description:
      "Thành viên và vai trò được giữ nguyên nhưng không đổi được cho đến khi bạn bỏ lưu trữ. Không có gì bị xóa.",
    action: "Lưu trữ nhóm",
    unarchiveTitle: "Bỏ lưu trữ {{team}}?",
    unarchiveDescription:
      "Thành viên lại đổi được và nhóm trở về danh sách đang hoạt động.",
    unarchiveAction: "Bỏ lưu trữ nhóm",
    cancel: "Hủy",
  },
  errors: {
    conflict:
      "Có người vừa thay đổi nhóm này. Dữ liệu đã được làm mới; hãy xem lại rồi thử lại.",
    forbidden:
      "Bạn không có quyền làm việc này. Hãy hỏi quản trị viên hoặc chủ sở hữu.",
    notFound: "Nhóm này không còn tồn tại. Danh sách đã được làm mới.",
    network: "Không kết nối được máy chủ. Chưa có gì thay đổi; hãy thử lại.",
    unknown: "Máy chủ gặp sự cố. Chưa có gì thay đổi; hãy thử lại.",
  },
  toasts: {
    created: "Đã tạo {{name}}.",
    renamed: "Nhóm giờ có tên {{name}}.",
    described: "Đã lưu mô tả của {{name}}.",
    archived: "Đã lưu trữ {{name}}.",
    unarchived: "Đã bỏ lưu trữ {{name}}.",
    memberAdded: "Đã thêm {{name}} vào nhóm.",
    memberRemoved: "Đã xóa {{name}} khỏi nhóm.",
    roleChanged: "{{name}} giờ là {{role}}.",
  },
};

export function workspaceTeamsCopy(locale: "vi" | "en"): WorkspaceTeamsCopy {
  return locale === "en" ? en : vi;
}
