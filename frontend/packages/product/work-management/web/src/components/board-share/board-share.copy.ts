/**
 * Board share dialog copy (Final-Share), vi and en. Plain data with no
 * imports. `board-share.catalog.ts` registers it as the `board-share`
 * catalog. `{{name}}` placeholders are filled with `fillBoardShareCopy`.
 */
export const en = {
  title: "Share {{name}}",
  subtitle: "Board in {{space}}",
  subtitleWorkspace: "Board in {{workspace}}",
  close: "Close",
  add: {
    placeholder: "Add workspace members",
    level: "Permission when inviting: {{level}}",
    invite: "Invite",
    levelList: "Permission when inviting",
    unavailable: "Inviting people to a Board has no write behind it yet.",
  },
  levels: {
    manage: "Manager",
    edit: "Can edit",
    comment: "Can comment",
    view: "Can view",
  },
  levelHints: {
    view: "See items and attached documents",
    comment: "See, comment, mention; files only inside comments",
    edit: "Create, edit, assign and move items",
    manage: "Board settings, who has access",
    guestViewOnly: "A guest can only view what is shared with them",
  },
  general: {
    label: "Who can open the Board",
    group: "Who can open the Board",
    bySpace: "Through {{name}}",
    bySpaceHint:
      "{{count}} people in the department can open it, with the permission of their department role.",
    bySpaceHintUnknown:
      "The people of the department can open it, with the permission of their department role.",
    byWorkspace: "Through the workspace",
    byWorkspaceHint:
      "Every workspace member can open it, with the permission of their workspace role.",
    private: "Private",
    privateHint:
      "Only people and teams added to the Board. Department roles don't open it, department admins included. Workspace admins also need to be added; only the workspace owner can always open it.",
    mapAdmin: "Admin → Manager",
    mapMember: "Member → Edit",
    mapViewer: "Viewer → View",
    change: "Change",
    changeUnavailable:
      "Changing who can open the Board has no write behind it yet.",
    current: "In use",
  },
  people: {
    label: "Permissions on this Board · {{count}}",
    empty: "No one has been added to this Board on their own.",
    you: "(you)",
    owner: "Board owner",
    ownerRow: "Board owner",
    team: "Workspace team · {{count}} people",
    teamName: "Team",
    guest: "Guest",
    guests: "{{count}} guests",
    guestOnly: "guest, sees only this Board",
    outside: "Outside the company",
    levelCapped: "a guest is capped at Can view",
    counts:
      "{{people}} people and {{guests}} guests can open this Board. Only Board members and managers see who.",
    outsideDepartment: "outside {{space}}",
    inSpace: "{{role}} {{space}}",
    roles: { Admin: "Admin", Member: "Member", Viewer: "Viewer" },
    ownDepartment: "{{name}}",
    levelOf: "Permission of {{name}}: {{level}}",
    levelUnavailable: "Changing permissions has no write behind it yet.",
    more: "Show {{count}} more",
    note: "Items embedded in a Doc still follow the Board's permissions.",
  },
  menu: {
    label: "Permission of {{name}}",
    remove: "Remove access",
    removeHint: "{{name}} no longer sees this Board",
    needManager: "At least one Manager is needed",
    keepCurrent: "Current level, kept",
    removeLast: "The only Manager of the Board can't be removed",
    lastNote:
      "A Board needs at least one person or team at Manager level. Make someone else a Manager first.",
    addManager: "Add a Manager",
  },
  link: {
    title: "Board link",
    hint: "Only people who already have access can open it. Sending the link grants nothing more.",
    publicOff: "Public: workspace admins turned it off",
    publicOn: "Public links have no page here yet",
  },
  footer: {
    log: "Permission history",
    logUnavailable: "The permission history has no read behind it yet.",
    copy: "Copy link",
    copied: "Link copied.",
    copyFailed: "Couldn't copy the link.",
    writeFailed: "Couldn't change the permission.",
    writeConflict:
      "Someone changed this board's access first. The latest list is shown, check it and try again.",
    done: "Done",
  },
  privateConfirm: {
    title: "Make the Board Private?",
    current: "Through {{name}}",
    currentHint: "In use",
    privateTitle: "Private",
    privateHint:
      "Only people and teams added to the Board. Department roles no longer open it, department admins included. Workspace admins also need to be added; only the workspace owner can always open it.",
    warn: "{{count}} people in {{name}} will no longer see the Board.",
    warnWork:
      " {{work}} of them have items that aren't done. Add them to the Board before switching so the work isn't left behind.",
    workLabel: "People with items on the Board",
    workItems: "{{count}} items not done on this Board",
    noPermission: "no permission of their own yet",
    keep: "Permissions kept:",
    keepEnd:
      " Workspace admins also need to be added; only the workspace owner can always open it. Items of people who lose access stay as they are; they just can't open them.",
    cancel: "Cancel",
    switchOnly: "Switch, add no one",
    switchAdd: "Add {{count}} people, then switch",
    keepInSpace: "**{{names}}** (already added on their own)",
    keepGuest: "guest **{{name}}**",
    keepAnd: " and ",
  },
  leave: {
    title: "Leave board",
    private:
      "You will no longer see “{{board}}”: it is Private and your own role is the way in.",
    open: "You only lose your own role on “{{board}}”. You still see it through the workspace.",
    grants:
      "Permissions given to you on the board by name are not changed when you leave; a manager changes them in Share.",
    openShare: "Open Share",
    noRole:
      "You have no role of your own on this board, so there is nothing to leave.",
    confirm: "Leave board",
    cancel: "Cancel",
    left: "You left “{{board}}”.",
    failed: "Couldn't leave the board. Nothing changed.",
    lastManagerTitle: "Couldn't leave the board",
    lastManagerBody:
      "You are the only Manager of “{{board}}”. A board needs at least one Manager, so you stay. Choose another Manager, then leave.",
    lastOwnerBody:
      "You are the only owner of “{{board}}”. A board needs an owner, so you stay. Make someone else an owner, then leave.",
    chooseManager: "Choose another Manager",
  },
  lastManager: {
    lowerTitle: "Couldn't change the permission of {{name}}",
    lowerBody:
      "A Board needs at least one Manager, so the permission stays Manager. Make someone else a Manager first, then lower or remove.",
    removeTitle: "Couldn't remove {{name}}",
    removeBody:
      "{{name}} is the only Manager, so the Board keeps its list. Make someone else a Manager first.",
    teamTitle: "Couldn't change the permission of {{name}}",
    teamBody:
      "{{name}} is the only Manager team of the Board. A Board needs at least one person or team at Manager level. Add another Manager first.",
    viaAdminTitle: "You manage this Board through workspace administration",
    viaAdminBody:
      "The workspace owner and admins can still open the Board's settings when the Board has no Manager.",
  },
  log: {
    title: "Permission history",
    back: "Back to permissions",
    onlyManagers: "Only Board managers can see it",
    backShort: "Back",
    empty: "No permission changes yet.",
    added: "**{{actor}}** added **{{target}}**{{extra}}",
    addedGuest: " (guest, {{level}})",
    addedPerson: " ({{level}})",
    changed: "**{{actor}}** changed **{{target}}**: {{from}} → {{to}}",
    removed: "**{{actor}}** removed **{{target}}**",
    madePrivate: "**{{actor}}** made the Board Private",
    visibility: "**{{actor}}** changed who can open the Board",
    system: "System",
  },
};

export type BoardShareCopy = typeof en;

export const vi: BoardShareCopy = {
  title: "Chia sẻ {{name}}",
  subtitle: "Board trong {{space}}",
  subtitleWorkspace: "Board trong {{workspace}}",
  close: "Đóng",
  add: {
    placeholder: "Thêm thành viên workspace",
    level: "Quyền khi mời: {{level}}",
    invite: "Mời",
    levelList: "Quyền khi mời",
    unavailable: "Mời người vào board chưa có thao tác ghi.",
  },
  levels: {
    manage: "Quản lý",
    edit: "Có thể sửa",
    comment: "Có thể bình luận",
    view: "Có thể xem",
  },
  levelHints: {
    view: "Xem việc, tài liệu đính kèm",
    comment: "Xem, bình luận, nhắc tên; đính kèm tệp chỉ trong bình luận",
    edit: "Tạo, sửa, giao, di chuyển việc",
    manage: "Thiết lập board, ai được vào board",
    guestViewOnly: "Khách chỉ được xem phần được chia sẻ với họ",
  },
  general: {
    label: "Ai vào được board",
    group: "Ai vào được board",
    bySpace: "Theo {{name}}",
    bySpaceHint:
      "{{count}} người trong phòng vào được, quyền theo vai trò ở phòng ban.",
    bySpaceHintUnknown:
      "Người trong phòng vào được, quyền theo vai trò ở phòng ban.",
    byWorkspace: "Theo workspace",
    byWorkspaceHint:
      "Mọi thành viên workspace vào được, quyền theo vai trò ở workspace.",
    private: "Riêng tư",
    privateHint:
      "Chỉ người và nhóm được thêm vào board. Vai trò ở phòng ban không còn mở được board này, kể cả Quản trị phòng. Quản trị workspace cũng cần được thêm riêng; chỉ chủ sở hữu workspace luôn vào được.",
    mapAdmin: "Quản trị → Quản lý",
    mapMember: "Thành viên → Sửa",
    mapViewer: "Người xem → Xem",
    change: "Đổi",
    changeUnavailable: "Đổi ai vào được board chưa có thao tác ghi.",
    current: "Đang dùng",
  },
  people: {
    label: "Quyền riêng trên board này · {{count}}",
    empty: "Chưa có ai được thêm riêng vào board này.",
    you: "(bạn)",
    owner: "Chủ board",
    ownerRow: "Chủ board",
    team: "Nhóm workspace · {{count}} người",
    teamName: "Nhóm",
    guest: "Khách",
    guests: "{{count}} khách",
    guestOnly: "khách, chỉ thấy board này",
    outside: "Ngoài công ty",
    levelCapped: "khách tối đa Có thể xem",
    counts:
      "{{people}} người và {{guests}} khách mở được board này. Chỉ thành viên và người quản lý board thấy là ai.",
    outsideDepartment: "ngoài {{space}}",
    inSpace: "{{role}} {{space}}",
    roles: { Admin: "Quản trị", Member: "Thành viên", Viewer: "Người xem" },
    ownDepartment: "{{name}}",
    levelOf: "Quyền của {{name}}: {{level}}",
    levelUnavailable: "Đổi quyền chưa có thao tác ghi.",
    more: "Hiện thêm {{count}}",
    note: "Việc nhúng trong tài liệu vẫn theo quyền của board.",
  },
  menu: {
    label: "Quyền của {{name}}",
    remove: "Gỡ quyền",
    removeHint: "{{name}} không còn thấy board này",
    needManager: "Cần ít nhất một Quản lý",
    keepCurrent: "Mức hiện tại, giữ nguyên",
    removeLast: "Không gỡ được người Quản lý duy nhất của board",
    lastNote:
      "Board cần ít nhất một người hoặc nhóm ở mức Quản lý. Thêm người khác làm Quản lý trước.",
    addManager: "Thêm Quản lý",
  },
  link: {
    title: "Liên kết của board",
    hint: "Chỉ người đã có quyền mở được. Gửi liên kết không cấp thêm quyền.",
    publicOff: "Công khai: quản trị workspace đã tắt",
    publicOn: "Liên kết công khai chưa có trang ở đây",
  },
  footer: {
    log: "Nhật ký thay đổi quyền",
    logUnavailable: "Nhật ký thay đổi quyền chưa có dữ liệu.",
    copy: "Sao chép liên kết",
    copied: "Đã sao chép liên kết.",
    copyFailed: "Chưa sao chép được liên kết.",
    writeFailed: "Chưa đổi được quyền.",
    writeConflict:
      "Người khác vừa đổi quyền truy cập board trước. Danh sách mới nhất đang hiện, hãy xem lại rồi thử lại.",
    done: "Xong",
  },
  privateConfirm: {
    title: "Chuyển board sang Riêng tư?",
    current: "Theo {{name}}",
    currentHint: "Đang dùng",
    privateTitle: "Riêng tư",
    privateHint:
      "Chỉ người và nhóm được thêm vào board. Vai trò ở phòng ban không còn mở được board này, kể cả Quản trị phòng. Quản trị workspace cũng cần được thêm riêng; chỉ chủ sở hữu workspace luôn vào được.",
    warn: "{{count}} người trong {{name}} sẽ không còn thấy board.",
    warnWork:
      " Trong đó {{work}} người đang được giao việc chưa xong. Thêm họ vào board trước khi chuyển để việc không bị bỏ dở.",
    workLabel: "Người đang có việc trên board",
    workItems: "{{count}} việc chưa xong trên board này",
    noPermission: "chưa có quyền riêng",
    keep: "Giữ nguyên quyền:",
    keepEnd:
      " Quản trị workspace cũng cần được thêm riêng; chỉ chủ sở hữu workspace luôn vào được. Việc của người mất quyền vẫn còn nguyên, chỉ họ không mở được.",
    cancel: "Huỷ",
    switchOnly: "Chuyển, không thêm ai",
    switchAdd: "Thêm {{count}} người rồi chuyển",
    keepInSpace: "**{{names}}** (đã được thêm riêng)",
    keepGuest: "khách **{{name}}**",
    keepAnd: " và ",
  },
  leave: {
    title: "Rời board",
    private:
      "Bạn sẽ không còn thấy “{{board}}”: board Riêng tư và vai trò riêng của bạn là lối vào duy nhất.",
    open: "Bạn chỉ mất vai trò riêng của mình trên “{{board}}”. Bạn vẫn thấy board qua workspace.",
    grants:
      "Quyền được cấp riêng cho bạn trên board không đổi khi bạn rời; người quản lý đổi chúng ở hộp Chia sẻ.",
    openShare: "Mở hộp Chia sẻ",
    noRole: "Bạn không có vai trò riêng trên board này nên không có gì để rời.",
    confirm: "Rời board",
    cancel: "Huỷ",
    left: "Bạn đã rời “{{board}}”.",
    failed: "Chưa rời được board. Không có gì thay đổi.",
    lastManagerTitle: "Chưa rời được board",
    lastManagerBody:
      "Bạn là người Quản lý duy nhất của “{{board}}”. Board cần ít nhất một Quản lý nên bạn vẫn ở lại. Chọn người khác làm Quản lý rồi rời board.",
    lastOwnerBody:
      "Bạn là chủ duy nhất của “{{board}}”. Board cần một chủ nên bạn vẫn ở lại. Chọn người khác làm chủ rồi rời board.",
    chooseManager: "Chọn Quản lý khác",
  },
  lastManager: {
    lowerTitle: "Chưa đổi được quyền của {{name}}",
    lowerBody:
      "Board cần ít nhất một Quản lý nên quyền vẫn là Quản lý. Thêm người khác làm Quản lý trước, rồi hạ hoặc gỡ.",
    removeTitle: "Chưa gỡ được {{name}}",
    removeBody:
      "{{name}} là người Quản lý duy nhất, nên board giữ nguyên danh sách. Thêm người khác làm Quản lý trước.",
    teamTitle: "Chưa đổi được quyền của {{name}}",
    teamBody:
      "{{name}} là nhóm Quản lý duy nhất của board. Board cần ít nhất một người hoặc nhóm ở mức Quản lý. Thêm Quản lý khác trước.",
    viaAdminTitle: "Bạn quản lý board này qua Quản trị workspace",
    viaAdminBody:
      "Chủ sở hữu và Quản trị workspace vẫn vào được thiết lập board khi board không còn Quản lý.",
  },
  log: {
    title: "Nhật ký thay đổi quyền",
    back: "Quay lại danh sách quyền",
    onlyManagers: "Chỉ người quản lý board xem được",
    backShort: "Quay lại",
    empty: "Chưa có thay đổi quyền nào.",
    added: "**{{actor}}** thêm **{{target}}**{{extra}}",
    addedGuest: " (khách, {{level}})",
    addedPerson: " ({{level}})",
    changed: "**{{actor}}** đổi **{{target}}**: {{from}} → {{to}}",
    removed: "**{{actor}}** gỡ quyền của **{{target}}**",
    madePrivate: "**{{actor}}** chuyển board sang Riêng tư",
    visibility: "**{{actor}}** đổi người được mở board",
    system: "Hệ thống",
  },
};

export function boardShareCopy(locale: "vi" | "en"): BoardShareCopy {
  return locale === "en" ? en : vi;
}

export function fillBoardShareCopy(
  template: string,
  values: Record<string, string | number>,
): string {
  return template.replace(/\{\{(\w+)\}\}/g, (_, key: string) =>
    String(values[key] ?? ""),
  );
}
