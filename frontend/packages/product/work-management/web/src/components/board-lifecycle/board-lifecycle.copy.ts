/**
 * Board lifecycle dialogs copy (move with access impact, duplicate, import,
 * export), vi and en. Plain data with no imports; `board-lifecycle.catalog.ts`
 * registers it as the `board-lifecycle` catalog. `{{name}}` placeholders are
 * filled with `fillLifecycleCopy`.
 */
export const en = {
  move: {
    loading: "Working out who gains and who loses access…",
    failed: "The impact could not be read. Nothing was moved.",
    retry: "Try again",
    losing_one: "{{count}} person loses access to the board",
    losing_other: "{{count}} people lose access to the board",
    losingEveryone:
      "Everyone outside {{target}} who was not added to the board loses access",
    losingBody:
      "They reach the board only through {{from}}. After the move the board is “Not found” to them.",
    losingBodyRoot:
      "They reach the board only because it sits at the top of the workspace. After the move the board is “Not found” to them.",
    strandedTitle_one: "{{count}} person who loses access has unfinished items",
    strandedTitle_other:
      "{{count}} people who lose access have unfinished items",
    strandedBody:
      "{{items}} items, {{overdue}} overdue. Add them to the board before the move so the work is not left behind; nobody is removed from an item.",
    stranded_one: "{{count}} unfinished item",
    stranded_other: "{{count}} unfinished items",
    strandedOverdue: "{{count}} overdue",
    noOwnAccess: "no access of their own",
    moreStranded: "and {{count}} more",
    noneStranded: "Nobody with unfinished items loses access",
    noneStrandedBody: "You can move it now.",
    gaining_one: "{{count}} person gains access to the board",
    gaining_other: "{{count}} people gain access to the board",
    gainingEveryone:
      "Everyone in the workspace who is not a guest reaches the board",
    gainingBody:
      "People in {{target}} reach it by their role there: Admin is Manager, Member is Editor, Viewer is Viewer.",
    kept: "Access given on the board itself stays",
    keptBody: "{{names}}. Their items stay as they are.",
    keptMore: "{{names}} and {{count}} more",
    noChange: "Nobody gains or loses access.",
    moveWithout: "Move without adding anyone",
    moveAdding_one: "Add {{count}} person, then move",
    moveAdding_other: "Add {{count}} people, then move",
    moving: "Moving…",
    moved: "{{name}} was moved.",
    movedAdded_one: "{{name}} was moved and {{count}} person was added.",
    movedAdded_other: "{{name}} was moved and {{count}} people were added.",
  },
  duplicate: {
    title: "Duplicate board",
    intro: "Copy the structure of “{{name}}” into a new board.",
    nameLabel: "Name of the copy",
    defaultName: "{{name}} (copy)",
    nameRequired: "Give the copy a name.",
    placeLabel: "Place in",
    placeRoot: "No Space (workspace level)",
    placeHint: "Defaults to where the original board sits.",
    alwaysTitle: "Always copied",
    alwaysBody:
      "Fields, groups, views, labels and the board settings. The copy is its own board with its own key and item numbers.",
    itemsTitle: "Copy the items too",
    itemsCount_one: "{{count}} item",
    itemsCount_other: "{{count}} items",
    itemsHint: "Off by default: the copy has the structure but no items.",
    itemsOn:
      "The items come with their group and order; each gets a new key in the copy. Comments, history and files are not copied. At most {{max}} items.",
    tooMany:
      "This board has {{count}} items, over the limit of {{max}} for a copy with items. Duplicate without items, then use Import to bring the items over.",
    dropped_one:
      "{{count}} assignment goes: that person has no access to the new board and is removed from the copied item.",
    dropped_other:
      "{{count}} assignments go: those people have no access to the new board and are removed from the copied items.",
    membersTitle: "Members",
    membersBody:
      "Not copied. You are the Manager of the copy; invite others with Share.",
    automationsTitle: "Automations",
    automationsBody: "Not copied. The copy starts with no rules.",
    fixed: "Fixed",
    footer:
      "The copy is an independent board with its own key and item numbers.",
    cancel: "Cancel",
    submit: "Duplicate board",
    working: "Copying…",
    workingItems:
      "Copying {{count}} items… nothing is created unless all of it is copied.",
    failed: "The board could not be copied. Nothing was created; try again.",
    done: "{{name}} was created.",
    doneDropped_one: "{{name}} was created; {{count}} assignment was left out.",
    doneDropped_other:
      "{{name}} was created; {{count}} assignments were left out.",
    open: "Open the copy",
    denied: "You cannot place a board there.",
    previewFailed: "The size of the copy could not be read.",
  },
};

export type BoardLifecycleCopy = {
  [K in keyof typeof en]: { [P in keyof (typeof en)[K]]: string };
};

export const vi: BoardLifecycleCopy = {
  move: {
    loading: "Đang tính ai được và mất quyền…",
    failed: "Chưa đọc được tác động. Chưa chuyển gì.",
    retry: "Thử lại",
    losing_one: "{{count}} người mất quyền vào board",
    losing_other: "{{count}} người mất quyền vào board",
    losingEveryone:
      "Mọi người ngoài {{target}} chưa được thêm vào board đều mất quyền vào board",
    losingBody:
      "Họ chỉ vào board qua {{from}}. Sau khi chuyển, board là “Không tìm thấy” với họ.",
    losingBodyRoot:
      "Họ vào được board vì board đang ở cấp workspace. Sau khi chuyển, board là “Không tìm thấy” với họ.",
    strandedTitle_one:
      "{{count}} người mất quyền đang có việc chưa xong trên board",
    strandedTitle_other:
      "{{count}} người mất quyền đang có việc chưa xong trên board",
    strandedBody:
      "{{items}} việc, {{overdue}} quá hạn. Thêm họ vào board trước khi chuyển để việc không bị bỏ dở; không ai bị gỡ khỏi việc.",
    stranded_one: "{{count}} việc chưa xong",
    stranded_other: "{{count}} việc chưa xong",
    strandedOverdue: "{{count}} quá hạn",
    noOwnAccess: "chưa có quyền riêng",
    moreStranded: "và {{count}} người nữa",
    noneStranded: "Không ai đang có việc chưa xong sẽ mất quyền",
    noneStrandedBody: "Có thể chuyển ngay.",
    gaining_one: "{{count}} người được vào board",
    gaining_other: "{{count}} người được vào board",
    gainingEveryone: "Mọi người trong workspace (trừ khách) đều vào được board",
    gainingBody:
      "Người trong {{target}} vào được theo vai trò ở đó: Quản trị là Quản lý, Thành viên là Sửa, Người xem là Xem.",
    kept: "Giữ nguyên quyền riêng",
    keptBody: "{{names}}. Việc của họ vẫn còn nguyên.",
    keptMore: "{{names}} và {{count}} người nữa",
    noChange: "Không ai được hay mất quyền.",
    moveWithout: "Chuyển, không thêm ai",
    moveAdding_one: "Thêm {{count}} người rồi chuyển",
    moveAdding_other: "Thêm {{count}} người rồi chuyển",
    moving: "Đang chuyển…",
    moved: "Đã chuyển {{name}}.",
    movedAdded_one: "Đã chuyển {{name}} và thêm {{count}} người.",
    movedAdded_other: "Đã chuyển {{name}} và thêm {{count}} người.",
  },
  duplicate: {
    title: "Nhân bản board",
    intro: "Sao chép cấu trúc của “{{name}}” sang một board mới.",
    nameLabel: "Tên bản sao",
    defaultName: "{{name}} (bản sao)",
    nameRequired: "Hãy đặt tên cho bản sao.",
    placeLabel: "Đặt trong",
    placeRoot: "Không thuộc phòng ban nào (cấp workspace)",
    placeHint: "Mặc định là nơi board gốc đang đặt.",
    alwaysTitle: "Luôn sao chép",
    alwaysBody:
      "Trường, nhóm việc, chế độ xem, nhãn và thiết lập board. Bản sao là board độc lập, nhận khoá và mã việc riêng.",
    itemsTitle: "Sao chép cả việc",
    itemsCount_one: "{{count}} việc",
    itemsCount_other: "{{count}} việc",
    itemsHint: "Mặc định tắt: bản sao có cấu trúc nhưng chưa có việc.",
    itemsOn:
      "Việc đi kèm nhóm và thứ tự hiện tại; mỗi việc nhận mã mới của bản sao. Không sao chép bình luận, lịch sử và tệp. Tối đa {{max}} việc.",
    tooMany:
      "Board có {{count}} việc, vượt giới hạn {{max}} việc khi sao chép kèm việc. Nhân bản không kèm việc rồi dùng Nhập dữ liệu để đưa việc sang.",
    dropped_one:
      "{{count}} người không có quyền ở board mới sẽ được bỏ khỏi việc đã sao chép.",
    dropped_other:
      "{{count}} người không có quyền ở board mới sẽ được bỏ khỏi việc đã sao chép.",
    membersTitle: "Thành viên",
    membersBody:
      "Không sao chép. Bạn là Quản lý của bản sao; mời người khác ở nút Chia sẻ.",
    automationsTitle: "Tự động hoá",
    automationsBody: "Không sao chép. Bản sao bắt đầu không có quy tắc nào.",
    fixed: "Cố định",
    footer: "Bản sao là board độc lập, nhận khoá và mã việc riêng.",
    cancel: "Huỷ",
    submit: "Nhân bản board",
    working: "Đang sao chép…",
    workingItems:
      "Đang sao chép {{count}} việc… không tạo gì nếu chưa sao chép đủ.",
    failed: "Chưa sao chép được board. Chưa tạo gì, hãy thử lại.",
    done: "Đã tạo {{name}}.",
    doneDropped_one: "Đã tạo {{name}}; bỏ {{count}} người khỏi việc.",
    doneDropped_other: "Đã tạo {{name}}; bỏ {{count}} người khỏi việc.",
    open: "Mở bản sao",
    denied: "Bạn không đặt được board vào đó.",
    previewFailed: "Chưa đọc được cỡ của bản sao.",
  },
};

export function boardLifecycleCopy(locale: "vi" | "en"): BoardLifecycleCopy {
  return locale === "vi" ? vi : en;
}

export function fillLifecycleCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** The `_one` form for exactly 1, else `_other`, then fills it. */
export function pluralLifecycle(
  copy: Readonly<Record<string, string>>,
  key: string,
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return fillLifecycleCopy(
    copy[`${key}_${count === 1 ? "one" : "other"}`] ?? "",
    {
      ...values,
      count,
    },
  );
}
