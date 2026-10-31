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
      "Someone else changed this access first. The list is up to date; try again.",
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
      "Người khác đã đổi quyền này trước. Danh sách đã được cập nhật, hãy thử lại.",
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
