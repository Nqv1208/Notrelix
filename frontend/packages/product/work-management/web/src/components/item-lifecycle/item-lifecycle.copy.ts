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
  deleteSubItems_one: "{{count}} sub-item is deleted with it.",
  deleteSubItems_other: "{{count}} sub-items are deleted with it.",
  deleteLinks_one:
    "{{count}} dependency link ({{keys}}) is hidden until it is restored.",
  deleteLinks_other:
    "{{count}} dependency links ({{keys}}) are hidden until it is restored.",
  deleted: "“{{title}}” was deleted",
  deletedUndo: "Restore",
  deleteFailed: "Couldn't delete “{{title}}”. Nothing changed.",
  deleteConflictBody:
    "It changed after you opened it, so it was not deleted. Review it, then delete again.",
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
  archiveTitle: "Archive this item?",
  archiveBody: "“{{title}}” will be hidden from the board.",
  archiveSubItemsLead_one: "{{count}} sub-item",
  archiveSubItemsLead_other: "{{count}} sub-items",
  archiveSubItemsTail_one: " is archived with it.",
  archiveSubItemsTail_other: " are archived with it.",
  archiveLinksLead_one: "{{count}} dependency link",
  archiveLinksLead_other: "{{count}} dependency links",
  archiveLinksTail: " ({{keys}}) stay and show as archived.",
  archiveRestore: "Review them under “Archived items” and unarchive any time.",
  archiveCancel: "Cancel",
  archiveClose: "Close",
  archiveConfirm: "Archive item",
  archiveConfirmMany: "Archive {{count}} items",
  archiveBusy: "Archiving…",
  archiveRetry: "Try again",
  archiveChecking: "Checking links…",
  archiveErrorTitle: "Couldn't archive",
  archiveErrorBody:
    "The item is unchanged. Check your connection and try again.",
  archiveConflictTitle: "Someone changed this item",
  archiveConflictBody:
    "It changed after you opened it, and it is refreshed now. Review it, then archive again.",
  archiveDeniedTitle: "You can't archive this item",
  archiveDeniedBody: "Archiving needs permission to edit items on this board.",
  archiveGoneTitle: "This item is no longer here",
  archiveGoneBody: "It was deleted or moved, so there is nothing to archive.",
  archived: "“{{title}}” was archived",
  archivedUndo: "Unarchive",
  trashTitle: "Deleted items",
  trashBody: "Items deleted from this board in the last 30 days.",
  trashEmpty: "Nothing was deleted from this board in the last 30 days.",
  trashLoading: "Loading deleted items",
  trashError: "Deleted items couldn't be loaded.",
  trashRestore: "Restore",
  trashUntil: "Can be restored until {{day}}",
  trashClose: "Close",
  archiveListTitle: "Archived items",
  archiveListBody:
    "Items archived on this board. Unarchive one to put it back.",
  archiveListEmpty: "No item is archived on this board.",
  archiveListLoading: "Loading archived items",
  archiveListError: "Archived items couldn't be loaded.",
  archiveListAction: "Unarchive",
  archiveListSince: "Archived on {{day}}",
  unarchived: "“{{title}}” was unarchived",
  unarchiveFailed: "Couldn't unarchive “{{title}}”.",
  listSearchTrash: "Search deleted items",
  listSearchArchive: "Search archived items",
  listEmptyTrashTitle: "Nothing was deleted",
  listEmptyArchiveTitle: "No item is archived yet",
  listEmptyArchiveBody:
    "Items you archive are kept here; unarchive them any time.",
  listNoResultsTitle: "No results",
  listNoResultsBody: "No item here matches “{{query}}”.",
  listErrorTitle: "The list couldn't be loaded",
  listErrorBody: "Check your connection and try again.",
  listRetry: "Try again",
  listBusy: "Working…",
  listSubItems_one: "{{count}} sub-item",
  listSubItems_other: "{{count}} sub-items",
  listMore: "Show {{count}} more",
  listShown: "Showing {{shown}} of {{total}}",
  trashBy: "Deleted by {{who}} · {{day}} · {{days}} left",
  trashByNobody: "Deleted {{day}} · {{days}} left",
  archiveBy: "Archived by {{who}} · {{when}}",
  archiveByNobody: "Archived {{when}}",
  restoreWindowClosed:
    "It was deleted more than 30 days ago, so it can't be restored any more.",
  restoreParentDeleted:
    "Not restored yet: the parent item is still in Deleted items. Restore the parent to get both back.",
  restoreGone: "This item is no longer in Deleted items.",
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
  deleteSubItems_one: "{{count}} việc con xoá cùng việc này.",
  deleteSubItems_other: "{{count}} việc con xoá cùng việc này.",
  deleteLinks_one:
    "{{count}} liên kết phụ thuộc ({{keys}}) tạm ẩn, quay lại khi khôi phục.",
  deleteLinks_other:
    "{{count}} liên kết phụ thuộc ({{keys}}) tạm ẩn, quay lại khi khôi phục.",
  deleted: "Đã xoá “{{title}}”",
  deletedUndo: "Khôi phục",
  deleteFailed: "Không xoá được “{{title}}”. Chưa có gì thay đổi.",
  deleteConflictBody:
    "Việc đã thay đổi sau khi bạn mở nên chưa bị xoá. Xem lại rồi xoá lần nữa.",
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
  archiveTitle: "Lưu trữ việc này?",
  archiveBody: "Việc “{{title}}” sẽ ẩn khỏi board.",
  archiveSubItemsLead_one: "{{count}} việc con",
  archiveSubItemsLead_other: "{{count}} việc con",
  archiveSubItemsTail_one: " được lưu trữ cùng.",
  archiveSubItemsTail_other: " được lưu trữ cùng.",
  archiveLinksLead_one: "{{count}} liên kết phụ thuộc",
  archiveLinksLead_other: "{{count}} liên kết phụ thuộc",
  archiveLinksTail: " ({{keys}}) vẫn giữ và hiện là đã lưu trữ.",
  archiveRestore:
    "Xem lại trong “Việc đã lưu trữ” và bỏ lưu trữ bất cứ lúc nào.",
  archiveCancel: "Huỷ",
  archiveClose: "Đóng",
  archiveConfirm: "Lưu trữ việc",
  archiveConfirmMany: "Lưu trữ {{count}} việc",
  archiveBusy: "Đang lưu trữ…",
  archiveRetry: "Thử lại",
  archiveChecking: "Đang kiểm tra liên kết…",
  archiveErrorTitle: "Chưa lưu trữ được",
  archiveErrorBody: "Việc vẫn như cũ. Kiểm tra kết nối rồi thử lại.",
  archiveConflictTitle: "Có người vừa sửa việc này",
  archiveConflictBody:
    "Việc đã thay đổi sau khi bạn mở và vừa được tải lại. Xem lại rồi lưu trữ lần nữa.",
  archiveDeniedTitle: "Bạn không lưu trữ được việc này",
  archiveDeniedBody: "Lưu trữ cần quyền sửa việc trên board này.",
  archiveGoneTitle: "Việc này không còn ở đây",
  archiveGoneBody: "Việc đã bị xoá hoặc chuyển đi, nên không có gì để lưu trữ.",
  archived: "Đã lưu trữ “{{title}}”",
  archivedUndo: "Bỏ lưu trữ",
  trashTitle: "Việc đã xoá",
  trashBody: "Các việc đã xoá khỏi board này trong 30 ngày qua.",
  trashEmpty: "Không có việc nào bị xoá khỏi board này trong 30 ngày qua.",
  trashLoading: "Đang tải việc đã xoá",
  trashError: "Không tải được danh sách việc đã xoá.",
  trashRestore: "Khôi phục",
  trashUntil: "Khôi phục được đến {{day}}",
  trashClose: "Đóng",
  archiveListTitle: "Việc đã lưu trữ",
  archiveListBody:
    "Các việc đã lưu trữ trên board này. Bỏ lưu trữ để đưa việc về lại board.",
  archiveListEmpty: "Board này chưa có việc nào được lưu trữ.",
  archiveListLoading: "Đang tải việc đã lưu trữ",
  archiveListError: "Không tải được danh sách việc đã lưu trữ.",
  archiveListAction: "Bỏ lưu trữ",
  archiveListSince: "Lưu trữ ngày {{day}}",
  unarchived: "Đã bỏ lưu trữ “{{title}}”",
  unarchiveFailed: "Không bỏ lưu trữ được “{{title}}”.",
  listSearchTrash: "Tìm trong việc đã xoá",
  listSearchArchive: "Tìm trong việc đã lưu trữ",
  listEmptyTrashTitle: "Chưa có việc nào bị xoá",
  listEmptyArchiveTitle: "Chưa có việc nào được lưu trữ",
  listEmptyArchiveBody:
    "Việc bạn lưu trữ sẽ nằm ở đây, bỏ lưu trữ bất cứ lúc nào.",
  listNoResultsTitle: "Không có kết quả",
  listNoResultsBody: "Không việc nào ở đây khớp “{{query}}”.",
  listErrorTitle: "Không tải được danh sách",
  listErrorBody: "Kiểm tra kết nối rồi thử lại.",
  listRetry: "Thử lại",
  listBusy: "Đang xử lý…",
  listSubItems_one: "{{count}} việc con",
  listSubItems_other: "{{count}} việc con",
  listMore: "Hiện thêm {{count}} việc",
  listShown: "Đã hiện {{shown}} trên {{total}}",
  trashBy: "Xoá bởi {{who}} · {{day}} · còn {{days}} ngày",
  trashByNobody: "Xoá {{day}} · còn {{days}} ngày",
  archiveBy: "Lưu trữ bởi {{who}} · {{when}}",
  archiveByNobody: "Lưu trữ {{when}}",
  restoreWindowClosed: "Đã quá 30 ngày nên không khôi phục được nữa.",
  restoreParentDeleted:
    "Chưa khôi phục được: việc cha vẫn đang trong Việc đã xoá. Khôi phục việc cha để lấy lại cả hai.",
  restoreGone: "Việc này không còn trong Việc đã xoá.",
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
