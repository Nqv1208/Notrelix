/**
 * Space org tree copy (OT-5, WX-DEC-008), vi and en.
 * Registered as the `workspace-spaces` catalog.
 */
import { spacePageEn, spacePageVi } from "./space-page.copy";

export const en = {
  tree: {
    title: "Departments",
    label: "Departments",
    create: "Create department",
    loading: "Loading Spaces",
    error: "Spaces couldn't load.",
    retry: "Try again",
    empty: "No Spaces yet.",
    emptyManager: "Group work by department or team with a Space.",
    private: "Private",
    archived: "Archived",
    keyboardHint:
      "Use the arrow keys to move through Spaces; Right opens a Space, Left closes it.",
  },
  types: {
    Department: "Department",
    Folder: "Folder",
    Portfolio: "Portfolio",
    ProjectArea: "Project area",
  },
  typeHelp: {
    Department: "A team or department, such as IT or Accounting.",
    Folder: "A simple group of Boards and Docs.",
    Portfolio: "A set of related projects followed together.",
    ProjectArea: "Everything for one project or initiative.",
  },
  visibility: {
    Private: "Private",
    Workspace: "Open",
  },
  visibilityHelp: {
    Private:
      "Only members of this Space — and workspace owners and admins — can find it and what's inside.",
    Workspace:
      "Everyone who can reach the parent can find it. At the top level, that is every workspace member (not guests).",
  },
  roles: {
    Admin: "Admin",
    Member: "Member",
    Viewer: "Viewer",
  },
  roleHelp: {
    Admin: "Manages the Space, its members and the Spaces inside it.",
    Member: "Creates and edits Boards and Docs in the Space.",
    Viewer: "Sees the Space and its Boards and Docs.",
  },
  access: {
    Manager: "You manage this Space",
    Editor: "You can add Boards and Docs",
    Viewer: "You can view this Space",
  },
  create: {
    title: "New Space",
    childTitle: "New Space in {{parent}}",
    description:
      "A Space groups Boards, Docs and people — for example one per department.",
    nameLabel: "Name",
    namePlaceholder: "e.g. Accounting",
    nameRequired: "Enter a name for the Space.",
    typeLabel: "Type",
    visibilityLabel: "Who can find it",
    locationLabel: "Location",
    locationRoot: "Top level of {{workspace}}",
    cancel: "Cancel",
    submit: "Create Space",
    submitting: "Creating…",
  },
  move: {
    spaceTitle: "Move {{name}}",
    boardTitle: "Move {{name}} to a Space",
    pageTitle: "Move {{name}} to a Space",
    description:
      "Pick where it goes. You can only pick places where you're allowed to add it.",
    pageDescription:
      "The Doc moves with the pages inside it. You can only pick places where you're allowed to add it.",
    spaceDescription:
      "The Space moves with everything inside it. You can only pick Spaces you manage.",
    searchLabel: "Find a Space",
    searchPlaceholder: "Search Spaces",
    root: "Top level of {{workspace}}",
    rootBoard: "No Space (workspace level)",
    current: "Current location",
    unavailable: "You can't add it here",
    descendant: "It can't go inside itself",
    noMatches: "No Spaces match.",
    cancel: "Cancel",
    submit: "Move here",
    submitting: "Moving…",
    pickerLabel: "Destination",
  },
  ...spacePageEn,
  members: {
    title: "Members",
    description:
      "People and teams in this Space. Members of a parent Space also count here unless this Space is private.",
    loading: "Loading members",
    error: "Members couldn't load.",
    retry: "Try again",
    empty: "No one has been added to this Space yet.",
    user: "Person",
    team: "Team",
    roleSelect: "Role for {{name}}",
    roleUpdating: "Updating {{name}}…",
    rowMenu: "Actions for {{name}}",
    remove: "Remove from Space",
    lastAdmin:
      "The last admin of a Space can't be removed or changed. Make someone else an admin first.",
    readOnly:
      "Only Space admins and workspace owners and admins can change members. You can see them here.",
    add: {
      title: "Add people or a team",
      subjectLabel: "Add",
      userTab: "Person",
      teamTab: "Team",
      userLabel: "Person",
      userPlaceholder: "Pick a workspace member",
      teamLabel: "Team",
      teamPlaceholder: "Pick a team",
      noUsers: "Everyone in the workspace is already here.",
      noTeams: "No teams to add. Workspace admins create teams.",
      teamsError: "Teams couldn't load.",
      roleLabel: "Role",
      required: "Pick who to add.",
      submit: "Add",
      submitting: "Adding…",
      teamMembers_one: "{{count}} member",
      teamMembers_other: "{{count}} members",
    },
    confirm: {
      removeTitle: "Remove {{name}} from {{space}}?",
      removeDescription:
        "They lose the access this Space gives them. Boards and Docs they were granted directly keep that access.",
      removeAction: "Remove",
      cancel: "Cancel",
    },
  },
  toasts: {
    created: "{{name}} was created.",
    createFailed: "The Space wasn't created.",
    moved: "{{name}} was moved.",
    moveFailed: "{{name}} wasn't moved.",
    saved: "Space settings saved.",
    saveFailed: "The changes weren't saved.",
    archived: "{{name}} was archived.",
    archiveFailed: "{{name}} wasn't archived.",
    unarchived: "{{name}} is active again.",
    unarchiveFailed: "{{name}} wasn't unarchived.",
    memberAdded: "{{name}} was added.",
    memberAddFailed: "{{name}} wasn't added.",
    roleChanged: "{{name}} is now {{role}}.",
    roleFailed: "The role of {{name}} wasn't changed.",
    memberRemoved: "{{name}} was removed.",
    memberRemoveFailed: "{{name}} wasn't removed.",
    conflict:
      "Someone changed this Space at the same time. The latest version is loaded; try again.",
    forbidden: "You don't have permission to do that here.",
    notFound: "That Space or item is no longer available.",
    network: "Couldn't reach the server. Nothing was changed; try again.",
    unknown: "The server ran into a problem. Nothing was changed; try again.",
  },
};

export type WorkspaceSpacesCopy = typeof en;

export const vi: WorkspaceSpacesCopy = {
  tree: {
    title: "Phòng ban",
    label: "Phòng ban",
    create: "Tạo phòng ban",
    loading: "Đang tải Không gian",
    error: "Chưa tải được Không gian.",
    retry: "Thử lại",
    empty: "Chưa có Không gian nào.",
    emptyManager:
      "Nhóm công việc theo phòng ban hoặc nhóm bằng một Không gian.",
    private: "Riêng tư",
    archived: "Đã lưu trữ",
    keyboardHint:
      "Dùng phím mũi tên để di chuyển giữa các Không gian; phím Phải để mở, phím Trái để thu gọn.",
  },
  types: {
    Department: "Phòng ban",
    Folder: "Thư mục",
    Portfolio: "Danh mục dự án",
    ProjectArea: "Khu vực dự án",
  },
  typeHelp: {
    Department: "Một nhóm hoặc phòng ban, ví dụ IT hay Kế toán.",
    Folder: "Một nhóm Bảng và Tài liệu đơn giản.",
    Portfolio: "Các dự án liên quan được theo dõi cùng nhau.",
    ProjectArea: "Mọi thứ của một dự án hoặc sáng kiến.",
  },
  visibility: {
    Private: "Riêng tư",
    Workspace: "Mở",
  },
  visibilityHelp: {
    Private:
      "Chỉ thành viên của Không gian này — cùng chủ sở hữu và quản trị viên workspace — mới tìm thấy nó và nội dung bên trong.",
    Workspace:
      "Mọi người vào được Không gian cha đều tìm thấy nó. Ở cấp cao nhất, đó là mọi thành viên workspace (trừ khách).",
  },
  roles: {
    Admin: "Quản trị",
    Member: "Thành viên",
    Viewer: "Người xem",
  },
  roleHelp: {
    Admin: "Quản lý Không gian, thành viên và các Không gian bên trong.",
    Member: "Tạo và chỉnh sửa Bảng và Tài liệu trong Không gian.",
    Viewer: "Xem Không gian cùng Bảng và Tài liệu của nó.",
  },
  access: {
    Manager: "Bạn quản lý Không gian này",
    Editor: "Bạn có thể thêm Bảng và Tài liệu",
    Viewer: "Bạn có thể xem Không gian này",
  },
  create: {
    title: "Không gian mới",
    childTitle: "Không gian mới trong {{parent}}",
    description:
      "Không gian gom Bảng, Tài liệu và mọi người lại với nhau — ví dụ mỗi phòng ban một Không gian.",
    nameLabel: "Tên",
    namePlaceholder: "ví dụ: Kế toán",
    nameRequired: "Nhập tên cho Không gian.",
    typeLabel: "Loại",
    visibilityLabel: "Ai có thể tìm thấy",
    locationLabel: "Vị trí",
    locationRoot: "Cấp cao nhất của {{workspace}}",
    cancel: "Hủy",
    submit: "Tạo Không gian",
    submitting: "Đang tạo…",
  },
  move: {
    spaceTitle: "Di chuyển {{name}}",
    boardTitle: "Chuyển {{name}} vào một Không gian",
    pageTitle: "Chuyển {{name}} vào một Không gian",
    description:
      "Chọn nơi đến. Bạn chỉ chọn được những nơi bạn có quyền thêm vào.",
    pageDescription:
      "Tài liệu được chuyển cùng các trang bên trong. Bạn chỉ chọn được những nơi bạn có quyền thêm vào.",
    spaceDescription:
      "Không gian được chuyển cùng mọi thứ bên trong. Bạn chỉ chọn được Không gian bạn quản lý.",
    searchLabel: "Tìm Không gian",
    searchPlaceholder: "Tìm Không gian",
    root: "Cấp cao nhất của {{workspace}}",
    rootBoard: "Không thuộc Không gian nào (cấp workspace)",
    current: "Vị trí hiện tại",
    unavailable: "Bạn không thể thêm vào đây",
    descendant: "Không thể chuyển vào chính nó",
    noMatches: "Không có Không gian nào khớp.",
    cancel: "Hủy",
    submit: "Chuyển đến đây",
    submitting: "Đang chuyển…",
    pickerLabel: "Nơi đến",
  },
  ...spacePageVi,
  members: {
    title: "Thành viên",
    description:
      "Những người và nhóm trong Không gian này. Thành viên của Không gian cha cũng được tính ở đây, trừ khi Không gian này riêng tư.",
    loading: "Đang tải thành viên",
    error: "Chưa tải được thành viên.",
    retry: "Thử lại",
    empty: "Chưa có ai được thêm vào Không gian này.",
    user: "Người",
    team: "Nhóm",
    roleSelect: "Vai trò của {{name}}",
    roleUpdating: "Đang cập nhật {{name}}…",
    rowMenu: "Thao tác cho {{name}}",
    remove: "Xóa khỏi Không gian",
    lastAdmin:
      "Không thể xóa hoặc đổi vai trò của quản trị viên cuối cùng. Hãy chỉ định người khác làm quản trị trước.",
    readOnly:
      "Chỉ quản trị viên Không gian và chủ sở hữu, quản trị viên workspace mới thay đổi được thành viên. Bạn có thể xem ở đây.",
    add: {
      title: "Thêm người hoặc nhóm",
      subjectLabel: "Thêm",
      userTab: "Người",
      teamTab: "Nhóm",
      userLabel: "Người",
      userPlaceholder: "Chọn một thành viên workspace",
      teamLabel: "Nhóm",
      teamPlaceholder: "Chọn một nhóm",
      noUsers: "Mọi người trong workspace đều đã ở đây.",
      noTeams: "Không có nhóm nào để thêm. Quản trị viên workspace tạo nhóm.",
      teamsError: "Chưa tải được danh sách nhóm.",
      roleLabel: "Vai trò",
      required: "Chọn người hoặc nhóm để thêm.",
      submit: "Thêm",
      submitting: "Đang thêm…",
      teamMembers_one: "{{count}} thành viên",
      teamMembers_other: "{{count}} thành viên",
    },
    confirm: {
      removeTitle: "Xóa {{name}} khỏi {{space}}?",
      removeDescription:
        "Họ mất quyền truy cập mà Không gian này cấp. Những Bảng và Tài liệu được cấp quyền trực tiếp vẫn giữ quyền đó.",
      removeAction: "Xóa",
      cancel: "Hủy",
    },
  },
  toasts: {
    created: "Đã tạo {{name}}.",
    createFailed: "Chưa tạo được Không gian.",
    moved: "Đã di chuyển {{name}}.",
    moveFailed: "Chưa di chuyển được {{name}}.",
    saved: "Đã lưu cài đặt Không gian.",
    saveFailed: "Chưa lưu được thay đổi.",
    archived: "Đã lưu trữ {{name}}.",
    archiveFailed: "Chưa lưu trữ được {{name}}.",
    unarchived: "{{name}} đã hoạt động trở lại.",
    unarchiveFailed: "Chưa bỏ lưu trữ được {{name}}.",
    memberAdded: "Đã thêm {{name}}.",
    memberAddFailed: "Chưa thêm được {{name}}.",
    roleChanged: "{{name}} giờ là {{role}}.",
    roleFailed: "Chưa đổi được vai trò của {{name}}.",
    memberRemoved: "Đã xóa {{name}}.",
    memberRemoveFailed: "Chưa xóa được {{name}}.",
    conflict:
      "Có người vừa thay đổi Không gian này cùng lúc. Phiên bản mới nhất đã được tải; hãy thử lại.",
    forbidden: "Bạn không có quyền làm việc này ở đây.",
    notFound: "Không gian hoặc mục đó không còn khả dụng.",
    network: "Không kết nối được máy chủ. Chưa có gì thay đổi; hãy thử lại.",
    unknown: "Máy chủ gặp sự cố. Chưa có gì thay đổi; hãy thử lại.",
  },
};

export function workspaceSpacesCopy(locale: "vi" | "en"): WorkspaceSpacesCopy {
  return locale === "en" ? en : vi;
}
