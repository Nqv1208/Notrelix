/**
 * Copy of the status option form (name, colour, category), vi and en,
 * registered as the `status-option-form` catalog. The pure form takes a
 * `copy` prop; `useStatusOptionFormCopy` resolves it for the active locale.
 * Placeholders use the catalog `{{name}}` form.
 */
export const en = {
  titleCreate: "New status",
  titleEdit: "Edit status",
  nameLabel: "Status name",
  namePlaceholder: "Status name…",
  colorLabel: "Colour",
  categoryLabel: "Group",
  color_slate: "Slate",
  color_blue: "Blue",
  color_amber: "Amber",
  color_green: "Green",
  color_red: "Red",
  color_orange: "Orange",
  color_teal: "Teal",
  color_ink: "Charcoal",
  category_todo: "Not started",
  category_in_progress: "In progress",
  category_done: "Done",
  submitCreate: "Add status",
  submitEdit: "Save",
  saving: "Saving…",
  cancel: "Cancel",
  errorEmpty: "Enter a status name.",
  errorDuplicate: "A status named “{{name}}” already exists.",
  errorTooLong: "Use 200 characters or fewer.",
  /** The same form for an option of another Select field (no group): "value". */
  valueTitleCreate: "New value",
  valueTitleEdit: "Edit value",
  valueNameLabel: "Value name",
  valueNamePlaceholder: "Value name…",
  valueSubmitCreate: "Add value",
  valueErrorEmpty: "Enter a value name.",
  valueErrorDuplicate: "A value named “{{name}}” already exists.",
  /** Deleting a status (or a value of another Select field) and re-homing its items. */
  removeTitle: "Delete status “{{name}}”?",
  removeValueTitle: "Delete value “{{name}}”?",
  removeEmptyBody: "No items use it.",
  removeItemsBody_one:
    "{{count}} item is in “{{name}}”. Choose where it moves.",
  removeItemsBody_other:
    "{{count}} items are in “{{name}}”. Choose where they move.",
  removeReplacementLabel: "Move items to",
  removeNoValue: "No value",
  removeConfirm: "Delete status",
  removeValueConfirm: "Delete value",
  removeMoveConfirm: "Move items and delete",
  removing: "Deleting…",
  removeBlockedStatus:
    "A board keeps at least one Not started and one Done status.",
  removeBlockedValue: "A field keeps at least one value.",
  /** "Add group": a group of the board (it holds items), not a status. */
  groupTitleCreate: "New group",
  groupNameLabel: "Group name",
  groupNamePlaceholder: "Group name…",
  groupSubmit: "Add group",
  groupErrorEmpty: "Enter a group name.",
  groupErrorDuplicate: "A group named “{{name}}” already exists.",
  groupAdded: "Group “{{name}}” added. Choose it in an item's Group field.",
  groupAddFailed: "Couldn't add the group.",
};

export type StatusOptionFormCopy = typeof en;

export const vi: StatusOptionFormCopy = {
  titleCreate: "Trạng thái mới",
  titleEdit: "Sửa trạng thái",
  nameLabel: "Tên trạng thái",
  namePlaceholder: "Nhập tên trạng thái…",
  colorLabel: "Màu",
  categoryLabel: "Nhóm",
  color_slate: "Xám",
  color_blue: "Xanh dương",
  color_amber: "Vàng nghệ",
  color_green: "Xanh lá",
  color_red: "Đỏ",
  color_orange: "Cam",
  color_teal: "Xanh ngọc",
  color_ink: "Than",
  category_todo: "Chưa bắt đầu",
  category_in_progress: "Đang làm",
  category_done: "Hoàn thành",
  submitCreate: "Thêm trạng thái",
  submitEdit: "Lưu",
  saving: "Đang lưu…",
  cancel: "Huỷ",
  errorEmpty: "Nhập tên trạng thái.",
  errorDuplicate: "Đã có trạng thái tên “{{name}}”.",
  errorTooLong: "Tối đa 200 ký tự.",
  valueTitleCreate: "Giá trị mới",
  valueTitleEdit: "Sửa giá trị",
  valueNameLabel: "Tên giá trị",
  valueNamePlaceholder: "Nhập tên giá trị…",
  valueSubmitCreate: "Thêm giá trị",
  valueErrorEmpty: "Nhập tên giá trị.",
  valueErrorDuplicate: "Đã có giá trị tên “{{name}}”.",
  removeTitle: "Xoá trạng thái “{{name}}”?",
  removeValueTitle: "Xoá giá trị “{{name}}”?",
  removeEmptyBody: "Chưa có việc nào dùng mục này.",
  removeItemsBody_one:
    "{{count}} việc đang ở “{{name}}”. Chọn nơi chuyển việc đó sang.",
  removeItemsBody_other:
    "{{count}} việc đang ở “{{name}}”. Chọn nơi chuyển các việc đó sang.",
  removeReplacementLabel: "Chuyển các việc sang",
  removeNoValue: "Không có giá trị",
  removeConfirm: "Xoá trạng thái",
  removeValueConfirm: "Xoá giá trị",
  removeMoveConfirm: "Chuyển việc và xoá",
  removing: "Đang xoá…",
  removeBlockedStatus:
    "Bảng luôn giữ ít nhất một trạng thái Chưa bắt đầu và một trạng thái Hoàn thành.",
  removeBlockedValue: "Trường luôn giữ ít nhất một giá trị.",
  groupTitleCreate: "Nhóm mới",
  groupNameLabel: "Tên nhóm",
  groupNamePlaceholder: "Nhập tên nhóm…",
  groupSubmit: "Thêm nhóm",
  groupErrorEmpty: "Nhập tên nhóm.",
  groupErrorDuplicate: "Đã có nhóm tên “{{name}}”.",
  groupAdded: "Đã thêm nhóm “{{name}}”. Chọn nhóm ở trường Nhóm của việc.",
  groupAddFailed: "Chưa thêm được nhóm.",
};

/**
 * The form copy for naming a board group: the same form (name, colour) with
 * the group wording, so "Add group" never reads like "Add status".
 */
export function groupFormCopy(
  copy: StatusOptionFormCopy,
): StatusOptionFormCopy {
  return {
    ...copy,
    titleCreate: copy.groupTitleCreate,
    nameLabel: copy.groupNameLabel,
    namePlaceholder: copy.groupNamePlaceholder,
    submitCreate: copy.groupSubmit,
    errorEmpty: copy.groupErrorEmpty,
    errorDuplicate: copy.groupErrorDuplicate,
  };
}

export function statusOptionFormCopy(
  locale: "vi" | "en",
): StatusOptionFormCopy {
  return locale === "vi" ? vi : en;
}
