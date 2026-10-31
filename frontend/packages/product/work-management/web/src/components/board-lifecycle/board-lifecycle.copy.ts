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
