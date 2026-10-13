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
};

export function statusOptionFormCopy(
  locale: "vi" | "en",
): StatusOptionFormCopy {
  return locale === "vi" ? vi : en;
}
