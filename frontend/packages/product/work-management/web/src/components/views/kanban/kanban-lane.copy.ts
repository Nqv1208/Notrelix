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
};

export function kanbanLaneCopy(locale: "vi" | "en"): KanbanLaneCopy {
  return locale === "vi" ? vi : en;
}
