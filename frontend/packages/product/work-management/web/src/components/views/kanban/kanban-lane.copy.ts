/**
 * Kanban lane and board copy, vi and en, registered as the `kanban-lane`
 * catalog (`kanban-lane.catalog.ts`). The pure board and lanes take a `copy`
 * prop; `useKanbanLaneCopy` resolves it for the active locale in the
 * container. Vietnamese follows the mockup wording ("Tuỳ chọn cột {lane}",
 * "Thêm vào {lane}", "Xem thêm N việc", "Không có việc khớp bộ lọc").
 * Placeholders use the catalog `{{name}}` form; counts use `_one`/`_other`
 * (Vietnamese keeps both complete).
 */
export const en = {
  /** Lane name for items without a value of the grouping field. */
  noStatus: "No status",
  noValue: "No value",
  /** Screen-reader count after the lane name. */
  count_one: "{{count}} item",
  count_other: "{{count}} items",
  laneOptions: "Options for column {{lane}}",
  collapse: "Collapse column",
  expand: "Expand column {{lane}}",
  hide: "Hide column from this view",
  showHidden_one: "Show {{count}} hidden column",
  showHidden_other: "Show {{count}} hidden columns",
  showMore_one: "Show {{count}} more item",
  showMore_other: "Show {{count}} more items",
  addTo: "Add to {{lane}}",
  newItemLabel: "New item title in {{lane}}",
  newItemPlaceholder: "Item title…",
  addSubmit: "Add",
  addCancel: "Cancel",
  /** The trailing slot after the last lane (a status of Status, a value of another Select field). */
  addStatus: "Add status",
  addValue: "Add value",
  /** Lane menu entry that renames or recolours the lane's option. */
  editStatus: "Edit status",
  editValue: "Edit value",
  createOptionFailed: "Couldn't add “{{name}}”.",
  updateOptionFailed: "Couldn't save “{{name}}”.",
  emptyFiltered: "No items match the filter",
  empty: "No items yet",
  moveFailed: "Couldn't move “{{title}}”. It is back in {{lane}}.",
  createFailed: "Couldn't add “{{title}}”.",
  saveFailed: "Couldn't save the change to “{{title}}”.",
  archiveFailed: "Couldn't archive “{{title}}”.",
  linkCopied: "Item link copied",
  linkCopyFailed: "Couldn't copy the item link",
  /** Live announcement after Alt+Arrow moves the focused card. */
  keyboardMoved: "Moved “{{title}}” to {{lane}}",
  keyboardReordered:
    "Moved “{{title}}” to position {{position}} of {{total}} in {{lane}}",
  /** Settings of the Kanban view (board header "Settings"). */
  settingsTitle: "Kanban settings",
  settingsDescription: "These apply to everyone who opens this Kanban view.",
  settingsClose: "Close",
  laneLimitLabel: "Items shown per column",
  laneLimitHint: "From 1 to 20. The rest sit behind “Show more”.",
  laneLimitDecrease: "Show fewer items per column",
  laneLimitIncrease: "Show more items per column",
  statusRolesLabel: "Who can add and edit statuses",
  statusRolesHint:
    "This only shows or hides the controls; the server still decides every change.",
  roleManager: "Managers (always)",
  roleEditor: "Editors",
  keyboardMoveLabel: "Move cards with the keyboard",
  keyboardMoveHint:
    "Alt + arrow keys move the focused card between columns and within a column.",
};

export type KanbanLaneCopy = typeof en;

export const vi: KanbanLaneCopy = {
  noStatus: "Không có trạng thái",
  noValue: "Chưa chọn",
  // Vietnamese has no singular/plural distinction; both forms stay complete.
  count_one: "{{count}} việc",
  count_other: "{{count}} việc",
  laneOptions: "Tuỳ chọn cột {{lane}}",
  collapse: "Thu gọn cột",
  expand: "Mở rộng cột {{lane}}",
  hide: "Ẩn cột khỏi chế độ xem",
  showHidden_one: "Hiện {{count}} cột đã ẩn",
  showHidden_other: "Hiện {{count}} cột đã ẩn",
  showMore_one: "Xem thêm {{count}} việc",
  showMore_other: "Xem thêm {{count}} việc",
  addTo: "Thêm vào {{lane}}",
  newItemLabel: "Tên việc mới trong {{lane}}",
  newItemPlaceholder: "Nhập tên việc…",
  addSubmit: "Thêm",
  addCancel: "Huỷ",
  addStatus: "Thêm trạng thái",
  addValue: "Thêm giá trị",
  editStatus: "Sửa trạng thái",
  editValue: "Sửa giá trị",
  createOptionFailed: "Không thêm được “{{name}}”.",
  updateOptionFailed: "Không lưu được “{{name}}”.",
  emptyFiltered: "Không có việc khớp bộ lọc",
  empty: "Chưa có việc",
  moveFailed: "Không chuyển được “{{title}}”. Việc đã về lại {{lane}}.",
  createFailed: "Không thêm được “{{title}}”.",
  saveFailed: "Không lưu được thay đổi của “{{title}}”.",
  archiveFailed: "Không lưu trữ được “{{title}}”.",
  linkCopied: "Đã sao chép liên kết",
  linkCopyFailed: "Không sao chép được liên kết",
  keyboardMoved: "Đã chuyển “{{title}}” sang {{lane}}",
  keyboardReordered:
    "Đã chuyển “{{title}}” đến vị trí {{position}}/{{total}} trong {{lane}}",
  settingsTitle: "Thiết lập Kanban",
  settingsDescription: "Áp dụng cho mọi người mở chế độ xem Kanban này.",
  settingsClose: "Đóng",
  laneLimitLabel: "Số việc hiện trong mỗi cột",
  laneLimitHint: "Từ 1 đến 20. Phần còn lại nằm sau “Xem thêm”.",
  laneLimitDecrease: "Hiện ít việc hơn trong mỗi cột",
  laneLimitIncrease: "Hiện nhiều việc hơn trong mỗi cột",
  statusRolesLabel: "Ai được thêm và sửa trạng thái",
  statusRolesHint:
    "Chỉ ẩn hoặc hiện nút thao tác; máy chủ vẫn quyết định mọi thay đổi.",
  roleManager: "Quản lý (luôn có)",
  roleEditor: "Người chỉnh sửa",
  keyboardMoveLabel: "Chuyển thẻ bằng bàn phím",
  keyboardMoveHint:
    "Alt + phím mũi tên chuyển thẻ đang chọn giữa các cột và trong cột.",
};

export function kanbanLaneCopy(locale: "vi" | "en"): KanbanLaneCopy {
  return locale === "vi" ? vi : en;
}
