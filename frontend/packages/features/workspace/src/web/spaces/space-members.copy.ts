/**
 * Copy of the department members tab (Final-Members): the toolbar, the list,
 * the role menu, the aside, the add dialog and the remove dialog. Plain data
 * with no imports; `workspace-spaces.copy.ts` spreads it into the
 * `workspace-spaces` catalog. `{{name}}` placeholders are filled with
 * `formatCopy`.
 */
export const spaceMembersEn = {
  members: {
    listLabel: "Members of {{name}}",
    searchLabel: "Find a member",
    searchPlaceholder: "Search by name or email",
    roleFilter: "Role",
    roleAll: "All",
    viaFilter: "Joined through",
    viaAny: "Any way",
    viaDirect: "Directly",
    viaTeam: "Whole team",
    summary: "{{admins}} · {{members}} · {{viewers}}",
    summaryInvites: "{{count}} invitation waiting",
    creator: "department creator",
    activeNow: "active now",
    head: {
      who: "Person or team",
      role: "Role in the department",
      via: "Joined through",
      activity: "Activity",
    },
    you: "You",
    teamMeta: "Workspace team · {{count}} people",
    teamMetaNoCount: "Workspace team",
    workspaceRole: {
      owner: "Workspace owner",
      admin: "Workspace admin",
      member: "Workspace member",
      guest: "Guest",
    },
    direct: "Directly",
    directTeam: "Whole team added directly",
    since: "since {{date}}",
    expand: "Expand {{name}}",
    collapse: "Collapse {{name}}",
    alsoDirect: "also added directly",
    teamHint:
      "Anyone who joins the team joins the department; leaving the team loses access at once. Change who is in a team in ",
    teamHintLink: "Workspace settings › Teams",
    teamHintEnd: " (workspace admins).",
    teamLoading: "Loading the team",
    teamError: "The team's people couldn't load.",
    teamEmpty: "No one is in this team.",
    rowActions: "Actions for {{name}}",
    moreRows: "Show {{count}} more",
    empty: "No one has been added to this department yet.",
    noMatches: "No one matches.",
    loading: "Loading members",
    error: "Members couldn't load.",
    retry: "Try again",
    pending: "Saving…",
    invitePending:
      "Waiting for the workspace invitation · joins the department on accepting",
    inviteSent: "invited {{when}}",
    inviteResend: "Resend",
    inviteRevoke: "Revoke",
    inviteUnavailable: "Invitations are not tied to a department yet.",
    readOnly:
      "Only department admins and workspace admins can change members. You can see them here.",
    roleMenu: {
      label: "Change the role of {{name}}",
      Admin: "Admin",
      AdminHint:
        "Add and remove people, switch Private or Open, manage every sub-department",
      Member: "Member",
      MemberHint: "Create and edit Boards, Docs and items in the department",
      Viewer: "Viewer",
      ViewerHint: "View only, no editing and no comments",
      lastAdminRole: "Can't lower: {{name}} is the only admin",
      remove: "Remove from the department",
      removeHint: "Removes only what was added directly",
      removeBlocked: "Make someone else an admin first",
      noteLast:
        "A department keeps at least one admin. To change the role of {{name}}, raise someone else to Admin first.",
      noteAlso:
        "{{name}} also joins through {{team}} as {{role}}. Lowering the role here keeps {{role}}, because the system uses the highest role.",
      noteHighest:
        "Someone who joins in more than one way (directly and through a team) uses the highest role.",
    },
    aside: {
      guests: "Guests of single Boards ({{count}})",
      guestsSub: "not department members",
      guestsNote:
        "People outside the department added to a Board on their own are shown in that Board's Share.",
      guestOutside: "Outside the company",
      guestInside: "In the company",
      guestLevel: {
        viewer: "Can view",
        commenter: "Can comment",
        editor: "Can edit",
        manager: "Manages",
      },
      guestLine: "{{company}} · {{level}} in {{board}}",
      label: "What the roles mean",
      roles: "Roles in the department",
      adminHint:
        "Everything a Member can do, plus: add and remove people, change roles, switch Private or Open, create and manage every sub-department.",
      memberHint:
        "Create Boards and Docs in the department; edit, assign and comment on every item in the department's open Boards.",
      viewerHint:
        "See the department's open Boards and Docs. No editing, no comments.",
      map: "On the department's open Boards: Admin → Board manager · Member → Edit · Viewer → View.",
      mapAdmins:
        "Workspace admins see every department without being on the list.",
      misread:
        "Two things people often misread. A private Board inside the department still needs its own people added; a department role does not open it. Someone who joins in several ways (directly and through a team) uses the highest role.",
      misreadLead: "Two things people often misread.",
      misreadText:
        " A private Board inside the department still needs its own people added; a department role does not open it. Someone who joins in several ways (directly and through a team) uses the highest role.",
      inherit: "Sub-departments inherit",
      inheritOpen: "Members here join {{names}} automatically.",
      inheritPrivate:
        "{{names}} is Private and keeps its own list; only admins here join it automatically.",
      inheritPrivatePlural:
        "{{names}} are Private and keep their own lists; only admins here join them automatically.",
      inheritNone: "This department has no sub-departments.",
    },
    add: {
      title: "Add to {{name}}",
      close: "Close",
      subtitle: "Type a name, email or team in the workspace {{workspace}}.",
      inputLabel: "Add people or teams",
      placeholder: "Type a name, email or team",
      suggestions: "Suggestions",
      none: "No one matches.",
      remove: "Remove {{name}}",
      roleLabel: "Role in the department",
      outsider:
        "{{email}} isn't a workspace member. A department only adds workspace members.",
      outsiderGuest:
        "{{name}} is a guest of the Board {{board}} and not a workspace member. A department only adds workspace members.",
      outsiderLink: "Invite to the workspace",
      preview: "{{count}} people will see it at once",
      previewOne: "1 person will see it at once",
      previewOpen: "{{boards}} open Boards and {{docs}} Docs of {{name}}",
      previewChildren: "Sub-departments {{names}}",
      previewHidden: "They won't see {{names}} (private)",
      hiddenAnd: " and ",
      hiddenBoard: "the Board {{name}}",
      foot: "They see the department as soon as they are added.",
      cancel: "Cancel",
      submit: "Add {{count}} people",
      submitOne: "Add 1 person",
      submitSkip: " · skip {{count}} emails",
      submitting: "Adding…",
      progress: "Added {{done}} of {{total}}",
      teamPeople: "{{count}} people",
    },
    remove: {
      teamTitle: "Remove {{name}} from the department?",
      userTitle: "Remove {{name}} from {{space}}?",
      userText:
        "They lose the access this department gives them. Boards and Docs they were granted directly keep that access.",
      teamText: "{{lose}} people will lose access to the department.",
      teamNoCount:
        "Everyone who joined through this team loses access to the department.",
      teamTextKeep:
        "{{lose}} people will lose access to the department; {{keep}} stays because they were added directly.",
      loses: "· loses access to the department",
      keeps: "· stays a {{role}}, added directly",
      note: "Items assigned to them keep their assignee. The team stays in the workspace.",
      cancel: "Cancel",
      confirmTeam: "Remove team",
      confirmUser: "Remove",
      close: "Close",
    },
  },
};

export type SpaceMembersCopy = typeof spaceMembersEn;

export const spaceMembersVi: SpaceMembersCopy = {
  members: {
    listLabel: "Thành viên {{name}}",
    searchLabel: "Tìm thành viên",
    searchPlaceholder: "Tìm theo tên hoặc email",
    roleFilter: "Vai trò",
    roleAll: "Tất cả",
    viaFilter: "Vào qua",
    viaAny: "Mọi cách",
    viaDirect: "Trực tiếp",
    viaTeam: "Cả nhóm",
    summary: "{{admins}} · {{members}} · {{viewers}}",
    summaryInvites: "{{count}} lời mời chờ",
    creator: "người tạo phòng",
    activeNow: "đang hoạt động",
    head: {
      who: "Người hoặc nhóm",
      role: "Vai trò trong phòng",
      via: "Vào qua",
      activity: "Hoạt động",
    },
    you: "Bạn",
    teamMeta: "Nhóm workspace · {{count}} người",
    teamMetaNoCount: "Nhóm workspace",
    workspaceRole: {
      owner: "Chủ sở hữu workspace",
      admin: "Quản trị workspace",
      member: "Thành viên workspace",
      guest: "Khách",
    },
    direct: "Trực tiếp",
    directTeam: "Thêm trực tiếp cả nhóm",
    since: "từ {{date}}",
    expand: "Mở rộng {{name}}",
    collapse: "Thu gọn {{name}}",
    alsoDirect: "cũng được thêm trực tiếp",
    teamHint:
      "Ai vào nhóm thì vào phòng, rời nhóm thì mất quyền ngay. Đổi người trong nhóm ở ",
    teamHintLink: "Thiết lập workspace › Nhóm",
    teamHintEnd: " (quản trị workspace).",
    teamLoading: "Đang tải nhóm",
    teamError: "Chưa tải được người trong nhóm.",
    teamEmpty: "Nhóm chưa có ai.",
    rowActions: "Thao tác với {{name}}",
    moreRows: "Hiện {{count}} người khác",
    empty: "Phòng ban này chưa có ai được thêm.",
    noMatches: "Không có ai khớp.",
    loading: "Đang tải thành viên",
    error: "Chưa tải được thành viên.",
    retry: "Thử lại",
    pending: "Đang lưu…",
    invitePending: "Chờ nhận lời mời vào workspace · vào phòng khi nhận lời",
    inviteSent: "mời {{when}}",
    inviteResend: "Gửi lại",
    inviteRevoke: "Thu hồi",
    inviteUnavailable: "Lời mời chưa gắn với phòng ban.",
    readOnly:
      "Chỉ quản trị phòng và quản trị workspace đổi được thành viên. Bạn xem được danh sách này.",
    roleMenu: {
      label: "Đổi vai trò của {{name}}",
      Admin: "Quản trị",
      AdminHint:
        "Thêm bớt người, đổi Riêng tư hay Mở, quản lý mọi phòng ban con",
      Member: "Thành viên",
      MemberHint: "Tạo và sửa board, tài liệu, công việc trong phòng",
      Viewer: "Người xem",
      ViewerHint: "Chỉ xem, không sửa và không bình luận",
      lastAdminRole: "Không hạ được: {{name}} là quản trị duy nhất",
      remove: "Gỡ khỏi phòng",
      removeHint: "Chỉ gỡ phần thêm trực tiếp",
      removeBlocked: "Cần đặt người khác làm quản trị trước",
      noteLast:
        "Phòng ban phải còn ít nhất 1 quản trị. Muốn đổi vai trò của {{name}}, nâng một người khác lên Quản trị trước.",
      noteAlso:
        "{{name}} còn vào qua {{team}} với vai trò {{role}}. Hạ xuống ở đây thì {{name}} vẫn là {{role}}, vì hệ thống dùng vai trò cao nhất.",
      noteHighest:
        "Một người vào bằng nhiều cách (trực tiếp và qua nhóm) thì dùng vai trò cao nhất.",
    },
    aside: {
      guests: "Khách ở từng board ({{count}})",
      guestsSub: "không phải thành viên phòng",
      guestsNote:
        "Người ngoài phòng được thêm riêng vào board xem ở Chia sẻ của từng board.",
      guestOutside: "Ngoài công ty",
      guestInside: "Trong công ty",
      guestLevel: {
        viewer: "Chỉ xem",
        commenter: "Có thể bình luận",
        editor: "Có thể sửa",
        manager: "Quản lý",
      },
      guestLine: "{{company}} · {{level}} ở {{board}}",
      label: "Giải thích vai trò",
      roles: "Vai trò trong phòng ban",
      adminHint:
        "Mọi quyền của Thành viên, thêm: thêm bớt người, đổi vai trò, đổi Riêng tư hay Mở, tạo và quản lý mọi phòng ban con.",
      memberHint:
        "Tạo board và tài liệu trong phòng; sửa, giao, bình luận mọi việc trong các board mở của phòng.",
      viewerHint:
        "Xem board và tài liệu mở của phòng. Không sửa, không bình luận.",
      map: "Ở các board mở của phòng: Quản trị → Quản lý board · Thành viên → Sửa · Người xem → Xem.",
      mapAdmins:
        "Quản trị workspace thấy mọi phòng ban, không cần có trong danh sách.",
      misread:
        "Hai điều hay bị hiểu nhầm. Board riêng tư trong phòng vẫn phải thêm người riêng; vai trò phòng ban không mở được nó. Một người vào bằng nhiều cách (trực tiếp và qua nhóm) thì dùng vai trò cao nhất.",
      misreadLead: "Hai điều hay bị hiểu nhầm.",
      misreadText:
        " Board riêng tư trong phòng vẫn phải thêm người riêng; vai trò phòng ban không mở được nó. Một người vào bằng nhiều cách (trực tiếp và qua nhóm) thì dùng vai trò cao nhất.",
      inherit: "Phòng ban con thừa hưởng",
      inheritOpen: "Thành viên ở đây tự vào {{names}}.",
      inheritPrivate:
        "{{names}} là Riêng tư nên có danh sách riêng; chỉ Quản trị ở đây là tự vào được.",
      inheritPrivatePlural:
        "{{names}} là Riêng tư nên có danh sách riêng; chỉ Quản trị ở đây là tự vào được.",
      inheritNone: "Phòng ban này chưa có phòng ban con.",
    },
    add: {
      title: "Thêm vào {{name}}",
      close: "Đóng",
      subtitle: "Gõ tên, email hoặc tên nhóm trong workspace {{workspace}}.",
      inputLabel: "Thêm người hoặc nhóm",
      placeholder: "Gõ tên, email hoặc tên nhóm",
      suggestions: "Gợi ý",
      none: "Không có ai khớp.",
      remove: "Bỏ {{name}}",
      roleLabel: "Vai trò trong phòng",
      outsider:
        "{{email}} chưa là thành viên workspace. Phòng ban chỉ thêm được thành viên workspace.",
      outsiderGuest:
        "{{name}} đang là khách của board {{board}}, chưa là thành viên workspace. Phòng ban chỉ thêm được thành viên workspace.",
      outsiderLink: "Mời vào workspace",
      preview: "{{count}} người sẽ thấy ngay",
      previewOne: "1 người sẽ thấy ngay",
      previewOpen: "{{boards}} board mở và {{docs}} tài liệu của {{name}}",
      previewChildren: "Phòng ban con {{names}}",
      previewHidden: "Không thấy {{names}} (riêng tư)",
      hiddenAnd: " và ",
      hiddenBoard: "board {{name}}",
      foot: "Họ thấy phòng ban này ngay sau khi được thêm.",
      cancel: "Huỷ",
      submit: "Thêm {{count}} người",
      submitOne: "Thêm 1 người",
      submitSkip: " · bỏ qua {{count}} email",
      submitting: "Đang thêm…",
      progress: "Đã thêm {{done}} trên {{total}}",
      teamPeople: "{{count}} người",
    },
    remove: {
      teamTitle: "Gỡ {{name}} khỏi phòng?",
      userTitle: "Gỡ {{name}} khỏi {{space}}?",
      userText:
        "Họ mất quyền mà phòng ban này cấp. Board và tài liệu họ được thêm trực tiếp vẫn giữ quyền đó.",
      teamText: "{{lose}} người sẽ mất quyền vào phòng.",
      teamNoCount: "Mọi người vào phòng qua nhóm này sẽ mất quyền vào phòng.",
      teamTextKeep:
        "{{lose}} người sẽ mất quyền vào phòng; {{keep}} vẫn còn vì được thêm trực tiếp.",
      loses: "· mất quyền vào phòng",
      keeps: "· vẫn là {{role}}, thêm trực tiếp",
      note: "Việc đang giao cho họ vẫn giữ người phụ trách. Nhóm vẫn còn trong workspace.",
      cancel: "Huỷ",
      confirmTeam: "Gỡ nhóm",
      confirmUser: "Gỡ",
      close: "Đóng",
    },
  },
};
