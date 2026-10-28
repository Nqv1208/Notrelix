/**
 * Copy for deleting, restoring and moving an item, vi and en, registered as
 * the `item-lifecycle` catalog (`item-lifecycle.catalog.ts`). Placeholders use
 * the catalog `{{name}}` form and are filled by `fillLifecycleCopy`.
 */
export const en = {
  locale: "en",
  delete: "Delete…",
  moveToBoard: "Move to another board…",
  deleteTitle: "Delete “{{title}}”?",
  deleteBody:
    "The item and its sub-items leave the board. You can restore it from the board's deleted items for 30 days.",
  deleteCancel: "Cancel",
  deleteConfirm: "Delete item",
  deleted: "“{{title}}” was deleted",
  deletedUndo: "Restore",
  deleteFailed: "Couldn't delete “{{title}}”. Nothing changed.",
  restored: "“{{title}}” was restored",
  restoreFailed: "Couldn't restore “{{title}}”.",
  moveTitle: "Move “{{title}}” to another board",
  moveBody:
    "Status and values carry over by name where the other board has the same field. Comments, files, sub-items and history move with the item; it gets the other board's next key.",
  moveSearch: "Find a board",
  moveEmpty: "No other board where you can add items.",
  moveCancel: "Cancel",
  moveConfirm: "Move item",
  moved: "“{{title}}” moved to {{board}} as {{key}}",
  movedDropped: "Not carried over: {{what}}.",
  droppedLabels_one: "{{count}} label",
  droppedLabels_other: "{{count}} labels",
  droppedAssignees_one: "{{count}} person who cannot see the board",
  droppedAssignees_other: "{{count}} people who cannot see the board",
  moveFailed: "Couldn't move “{{title}}”. It stays where it is.",
  trashTitle: "Deleted items",
  trashBody: "Items deleted from this board in the last 30 days.",
  trashEmpty: "Nothing was deleted from this board in the last 30 days.",
  trashLoading: "Loading deleted items",
  trashError: "Deleted items couldn't be loaded.",
  trashRestore: "Restore",
  trashUntil: "Can be restored until {{day}}",
  trashClose: "Close",
};

export type ItemLifecycleCopy = typeof en;

export const vi: ItemLifecycleCopy = {
  locale: "vi",
  delete: "Xoá…",
  moveToBoard: "Chuyển sang board khác…",
  deleteTitle: "Xoá “{{title}}”?",
  deleteBody:
    "Việc và các việc con sẽ rời khỏi board. Bạn có thể khôi phục từ mục việc đã xoá của board trong 30 ngày.",
  deleteCancel: "Huỷ",
  deleteConfirm: "Xoá việc",
  deleted: "Đã xoá “{{title}}”",
  deletedUndo: "Khôi phục",
  deleteFailed: "Không xoá được “{{title}}”. Chưa có gì thay đổi.",
  restored: "Đã khôi phục “{{title}}”",
  restoreFailed: "Không khôi phục được “{{title}}”.",
  moveTitle: "Chuyển “{{title}}” sang board khác",
  moveBody:
    "Trạng thái và giá trị được giữ theo tên khi board kia có trường tương ứng. Bình luận, tệp, việc con và lịch sử đi cùng việc; việc nhận mã tiếp theo của board kia.",
  moveSearch: "Tìm board",
  moveEmpty: "Không có board nào khác mà bạn được thêm việc.",
  moveCancel: "Huỷ",
  moveConfirm: "Chuyển việc",
  moved: "Đã chuyển “{{title}}” sang {{board}} với mã {{key}}",
  movedDropped: "Không giữ lại: {{what}}.",
  droppedLabels_one: "{{count}} nhãn",
  droppedLabels_other: "{{count}} nhãn",
  droppedAssignees_one: "{{count}} người chưa xem được board",
  droppedAssignees_other: "{{count}} người chưa xem được board",
  moveFailed: "Không chuyển được “{{title}}”. Việc vẫn ở chỗ cũ.",
  trashTitle: "Việc đã xoá",
  trashBody: "Các việc đã xoá khỏi board này trong 30 ngày qua.",
  trashEmpty: "Không có việc nào bị xoá khỏi board này trong 30 ngày qua.",
  trashLoading: "Đang tải việc đã xoá",
  trashError: "Không tải được danh sách việc đã xoá.",
  trashRestore: "Khôi phục",
  trashUntil: "Khôi phục được đến {{day}}",
  trashClose: "Đóng",
};

export function itemLifecycleCopy(locale: string): ItemLifecycleCopy {
  return locale === "vi" ? vi : en;
}

/** Fills `{{name}}` placeholders; an unknown placeholder is left visible. */
export function fillLifecycleCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** Picks the `_one` form for exactly 1 and `_other` otherwise, then fills it. */
export function pluralLifecycleCopy(
  forms: { readonly one: string; readonly other: string },
  count: number,
): string {
  return fillLifecycleCopy(count === 1 ? forms.one : forms.other, { count });
}
