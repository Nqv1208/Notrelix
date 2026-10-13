import {
  en as kanbanToolbarEn,
  vi as kanbanToolbarVi,
} from "../kanban/kanban-toolbar.copy";

/**
 * Board table copy, vi and en, registered as the `main-table` catalog
 * (`main-table.catalog.ts`). The pure surfaces take a `copy` prop;
 * `useMainTableCopy` resolves it for the active locale in the container.
 * Vietnamese is the mockup wording (`Final-Table`). Placeholders use the
 * catalog `{{name}}` form; counts use `_one`/`_other` (Vietnamese keeps both
 * complete). The "Lọc" menu is the Kanban's, so its copy is shared.
 */
export const en = {
  /** BCP 47 tag of this copy. */
  locale: "en",
  /** Accessible name of the table; the board title fills `{{board}}`. */
  tableLabel: "Items of the board {{board}}",
  toolbar: {
    search: {
      placeholder: "Search the table…",
      label: "Search the table",
    },
    /** The default "hide finished items" chip. */
    doneChip: "Status: not done",
    chip: {
      status: "Status: {{names}}",
      priority: "Priority: {{names}}",
      assignee: "Assignee: {{names}}",
      label: "Label: {{names}}",
      overdue: "Overdue",
    },
    chipClear: "Clear filter {{filter}}",
    filter: kanbanToolbarEn.filter,
    sort: {
      label: "Sort",
      /** Screen-reader name of the direction in the trigger value. */
      ascending: "ascending",
      descending: "descending",
      byColumn: "Sort by",
      ascendingLabel: "Ascending",
      descendingLabel: "Descending",
    },
    groupBy: {
      label: "Group by",
    },
    columns: {
      label: "Columns",
      heading: "Visible columns",
      density: "Row height",
      comfortable: "Comfortable",
      compact: "Compact",
    },
    addItem: "Add item",
  },
  columns: {
    select: "Select",
    key: "Key",
    title: "Item title",
    status: "Status",
    assignee: "Assignee",
    due: "Due",
    priority: "Priority",
    label: "Label",
    progress: "Child items",
    updated: "Updated",
    start: "Start",
    created: "Created",
    completed: "Completed",
  },
  header: {
    selectAll: "Select all",
    resize: "Resize column {{name}}",
    sortedAscending: "sorted ascending",
    sortedDescending: "sorted descending",
  },
  group: {
    collapse: "Collapse group {{name}}",
    expand: "Expand group {{name}}",
    noStatus: "No status",
    noValue: "No value",
    overdue_one: "{{count}} overdue",
    overdue_other: "{{count}} overdue",
    addTo: "Add to {{name}}",
    newItemLabel: "New item title in {{name}}",
    newItemPlaceholder: "Item title…",
    addSubmit: "Add",
    addCancel: "Cancel",
    addStatus: "Add status",
    addGroup: "Add group",
    groupForm: {
      title: "New group",
      nameLabel: "Group name",
      namePlaceholder: "Group name…",
      submit: "Add group",
      errorEmpty: "Enter a group name.",
      errorDuplicate: "A group named “{{name}}” already exists.",
    },
  },
  row: {
    select: "Select {{key}}",
    unassigned: "Unassigned",
    dueNone: "Not set",
    dueToday: "Today",
    overdueDays_one: "Overdue {{count}} day",
    overdueDays_other: "Overdue {{count}} days",
    noPriority: "—",
    priority: {
      urgent: "Urgent",
      high: "High",
      medium: "Medium",
      low: "Low",
    },
    childProgress: "{{done}} of {{total}} child items done",
    changeStatus: "Change status of {{key}}",
    updated: {
      justNow: "Just now",
      minutes_one: "{{count}} minute ago",
      minutes_other: "{{count}} minutes ago",
      hours_one: "{{count}} hour ago",
      hours_other: "{{count}} hours ago",
      yesterday: "Yesterday",
      days_one: "{{count}} day ago",
      days_other: "{{count}} days ago",
      lastWeek: "Last week",
    },
  },
  status: {
    change: "Change status",
    search: "Search statuses…",
    searchLabel: "Search statuses",
    none: "No matching status",
  },
  footer: {
    summary: "{{shown}} items shown · {{hidden}} hidden by filters",
    showDone: "Show finished items",
    move: "move",
    open: "open details",
    select: "select row",
  },
  bulk: {
    label: "Actions for {{count}} selected items",
    selected: "{{count}} selected",
    status: "Status",
    assign: "Assign to",
    due: "Set due date",
    move: "Move to board",
    archive: "Archive",
    clear: "Clear selection (Esc)",
    unavailable: "Not available yet",
    dueLabel: "Due date",
    dueApply: "Apply",
    dueClear: "No due date",
    statusLabel: "Set status",
  },
  archived: {
    done_one: "Archived {{count}} item",
    done_other: "Archived {{count}} items",
    undo: "Undo",
    close: "Close notice",
  },
  noResults: {
    title: "No items match the filter",
    description: "No item matches the current filters on this board.",
    clear: "Clear filter",
  },
  empty: {
    title: "No items yet",
    description: "Add the first item with “Add item”.",
  },
  loading: "Loading the table",
  unavailable: {
    title: "Table unavailable",
    description: "The board table could not be loaded.",
  },
  failed: {
    status: "Couldn't change the status of “{{title}}”.",
    archive: "Couldn't archive “{{title}}”.",
    create: "Couldn't add “{{title}}”.",
    save: "Couldn't save the change to “{{title}}”.",
    addGroup: "Couldn't add “{{name}}”.",
  },
};

export type MainTableCopy = typeof en;

export const vi: MainTableCopy = {
  locale: "vi",
  tableLabel: "Công việc của board {{board}}",
  toolbar: {
    search: {
      placeholder: "Tìm trong bảng…",
      label: "Tìm trong bảng",
    },
    doneChip: "Trạng thái: chưa xong",
    chip: {
      status: "Trạng thái: {{names}}",
      priority: "Ưu tiên: {{names}}",
      assignee: "Người phụ trách: {{names}}",
      label: "Nhãn: {{names}}",
      overdue: "Quá hạn",
    },
    chipClear: "Bỏ lọc {{filter}}",
    filter: kanbanToolbarVi.filter,
    sort: {
      label: "Sắp xếp",
      ascending: "tăng dần",
      descending: "giảm dần",
      byColumn: "Sắp xếp theo",
      ascendingLabel: "Tăng dần",
      descendingLabel: "Giảm dần",
    },
    groupBy: {
      label: "Nhóm theo",
    },
    columns: {
      label: "Cột",
      heading: "Cột hiển thị",
      density: "Chiều cao hàng",
      comfortable: "Thoải mái",
      compact: "Gọn",
    },
    addItem: "Thêm công việc",
  },
  columns: {
    select: "Chọn",
    key: "Mã",
    title: "Tên công việc",
    status: "Trạng thái",
    assignee: "Người phụ trách",
    due: "Hạn",
    priority: "Ưu tiên",
    label: "Nhãn",
    progress: "Việc con",
    updated: "Cập nhật",
    start: "Bắt đầu",
    created: "Ngày tạo",
    completed: "Hoàn thành",
  },
  header: {
    selectAll: "Chọn tất cả",
    resize: "Đổi độ rộng cột {{name}}",
    sortedAscending: "sắp xếp tăng dần",
    sortedDescending: "sắp xếp giảm dần",
  },
  group: {
    collapse: "Thu gọn nhóm {{name}}",
    expand: "Mở nhóm {{name}}",
    noStatus: "Không có trạng thái",
    noValue: "Chưa chọn",
    // Vietnamese has no singular/plural distinction; both forms stay complete.
    overdue_one: "{{count}} quá hạn",
    overdue_other: "{{count}} quá hạn",
    addTo: "Thêm vào {{name}}",
    newItemLabel: "Tên việc mới trong {{name}}",
    newItemPlaceholder: "Nhập tên việc…",
    addSubmit: "Thêm",
    addCancel: "Huỷ",
    addStatus: "Thêm trạng thái",
    addGroup: "Thêm nhóm",
    groupForm: {
      title: "Nhóm mới",
      nameLabel: "Tên nhóm",
      namePlaceholder: "Nhập tên nhóm…",
      submit: "Thêm nhóm",
      errorEmpty: "Nhập tên nhóm.",
      errorDuplicate: "Đã có nhóm tên “{{name}}”.",
    },
  },
  row: {
    select: "Chọn {{key}}",
    unassigned: "Chưa giao",
    dueNone: "Chưa đặt",
    dueToday: "Hôm nay",
    overdueDays_one: "Quá hạn {{count}} ngày",
    overdueDays_other: "Quá hạn {{count}} ngày",
    noPriority: "—",
    priority: {
      urgent: "Khẩn",
      high: "Cao",
      medium: "Trung bình",
      low: "Thấp",
    },
    childProgress: "Đã xong {{done}} trên {{total}} việc con",
    changeStatus: "Đổi trạng thái của {{key}}",
    updated: {
      justNow: "Vừa xong",
      minutes_one: "{{count}} phút trước",
      minutes_other: "{{count}} phút trước",
      hours_one: "{{count}} giờ trước",
      hours_other: "{{count}} giờ trước",
      yesterday: "Hôm qua",
      days_one: "{{count}} ngày trước",
      days_other: "{{count}} ngày trước",
      lastWeek: "Tuần trước",
    },
  },
  status: {
    change: "Đổi trạng thái",
    search: "Tìm trạng thái…",
    searchLabel: "Tìm trạng thái",
    none: "Không có trạng thái phù hợp",
  },
  footer: {
    summary: "{{shown}} công việc đang hiện · {{hidden}} ẩn bởi bộ lọc",
    showDone: "Hiện việc đã xong",
    move: "di chuyển",
    open: "mở chi tiết",
    select: "chọn dòng",
  },
  bulk: {
    label: "Thao tác với {{count}} công việc đã chọn",
    selected: "{{count}} đã chọn",
    status: "Trạng thái",
    assign: "Giao cho",
    due: "Đặt hạn",
    move: "Chuyển board",
    archive: "Lưu trữ",
    clear: "Bỏ chọn (Esc)",
    unavailable: "Chưa khả dụng",
    dueLabel: "Hạn",
    dueApply: "Áp dụng",
    dueClear: "Bỏ hạn",
    statusLabel: "Đặt trạng thái",
  },
  archived: {
    done_one: "Đã lưu trữ {{count}} việc",
    done_other: "Đã lưu trữ {{count}} việc",
    undo: "Hoàn tác",
    close: "Đóng thông báo",
  },
  noResults: {
    title: "Không có việc khớp bộ lọc",
    description: "Không có việc nào khớp bộ lọc hiện tại trên board này.",
    clear: "Bỏ lọc",
  },
  empty: {
    title: "Chưa có việc",
    description: "Thêm việc đầu tiên bằng “Thêm công việc”.",
  },
  loading: "Đang tải bảng",
  unavailable: {
    title: "Không mở được bảng",
    description: "Không tải được bảng công việc của board này.",
  },
  failed: {
    status: "Không đổi được trạng thái của “{{title}}”.",
    archive: "Không lưu trữ được “{{title}}”.",
    create: "Không thêm được “{{title}}”.",
    save: "Không lưu được thay đổi của “{{title}}”.",
    addGroup: "Không thêm được “{{name}}”.",
  },
};

export function mainTableCopy(locale: "vi" | "en"): MainTableCopy {
  return locale === "vi" ? vi : en;
}

/** Fills `{{name}}` placeholders; an unknown placeholder is left visible. */
export function fillTableCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** Picks the `_one` form for exactly 1 and `_other` otherwise, then fills it. */
export function pluralTableCopy(
  forms: { readonly one: string; readonly other: string },
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return fillTableCopy(count === 1 ? forms.one : forms.other, {
    ...values,
    count,
  });
}
