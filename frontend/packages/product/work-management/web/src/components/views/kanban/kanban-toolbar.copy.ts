/**
 * Kanban toolbar copy, vi and en, registered as the `kanban-toolbar` catalog
 * (`kanban-toolbar.catalog.ts`). The pure toolbar takes a `copy` prop;
 * `useKanbanToolbarCopy` resolves it for the active locale in the container.
 * Vietnamese is the mockup wording. Placeholders use the catalog `{{name}}`
 * form; counts use `_one`/`_other` (Vietnamese keeps both complete).
 */
export const en = {
  search: {
    placeholder: "Search this board…",
    label: "Search this board",
  },
  filter: {
    trigger: "Filter",
    status: "Status",
    priority: "Priority",
    assignee: "Assignee",
    label: "Label",
    overdue: "Overdue",
    noOptions: "No options yet",
    priorities: {
      urgent: "Urgent",
      high: "High",
      medium: "Medium",
      low: "Low",
    },
    /** One chip segment per selected value. */
    chip: {
      status: "{{name}}",
      priority: "{{name}} priority",
      assignee: "Assigned to {{name}}",
      label: "Label {{name}}",
      overdue: "Overdue",
    },
    /** Accessible name of the chip's remove button. */
    clear: "Clear filter {{filters}}",
    listSeparator: ", ",
  },
  groupBy: {
    label: "Group by",
  },
  addItem: "Add item",
  noResults: {
    title: "No items match the filter",
    hidden_one: "{{count}} board item is hidden",
    hidden_other: "{{count}} board items are hidden",
    clear: "Clear filter",
  },
  separator: " · ",
};

export type KanbanToolbarCopy = typeof en;

export const vi: KanbanToolbarCopy = {
  search: {
    placeholder: "Tìm trong board…",
    label: "Tìm trong board",
  },
  filter: {
    trigger: "Lọc",
    status: "Trạng thái",
    priority: "Ưu tiên",
    assignee: "Người phụ trách",
    label: "Nhãn",
    overdue: "Quá hạn",
    noOptions: "Chưa có lựa chọn",
    priorities: {
      urgent: "Khẩn",
      high: "Cao",
      medium: "Trung bình",
      low: "Thấp",
    },
    chip: {
      status: "{{name}}",
      priority: "Ưu tiên {{name}}",
      assignee: "Người phụ trách {{name}}",
      label: "Nhãn {{name}}",
      overdue: "Quá hạn",
    },
    clear: "Bỏ lọc {{filters}}",
    listSeparator: ", ",
  },
  groupBy: {
    label: "Nhóm theo",
  },
  addItem: "Thêm công việc",
  noResults: {
    title: "Không có việc khớp bộ lọc",
    // Vietnamese has no singular/plural distinction; both forms stay complete.
    hidden_one: "{{count}} việc của board đang bị ẩn",
    hidden_other: "{{count}} việc của board đang bị ẩn",
    clear: "Bỏ lọc",
  },
  separator: " · ",
};

export function kanbanToolbarCopy(locale: "vi" | "en"): KanbanToolbarCopy {
  return locale === "vi" ? vi : en;
}
