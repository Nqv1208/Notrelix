/**
 * Kanban card copy, vi and en, registered as the `kanban-card` catalog
 * (`kanban-card.catalog.ts`). The pure surface takes a `copy` prop;
 * `useKanbanCardCopy` resolves it for the active locale in the container.
 * Placeholders use the catalog `{{name}}` form and are filled by `fillCopy`.
 * Dates are written as day/month ("18/10") in both locales, never with a
 * month name.
 */
export const en = {
  /** BCP 47 tag of this copy. */
  locale: "en",
  /** Screen-reader prefix before the label chip. */
  label: "Label",
  /** Screen-reader prefix before a due date or "Today". */
  due: "Due",
  dueToday: "Today",
  overdueDays_one: "Overdue {{count}} day",
  overdueDays_other: "Overdue {{count}} days",
  /** Screen-reader prefix before the priority word. */
  priorityPrefix: "Priority",
  priority: {
    urgent: "Urgent",
    high: "High",
    medium: "Medium",
    low: "Low",
  },
  startAndDue: "Starts {{start}}, due {{due}}",
  checklistProgress: "Checklist {{done}} of {{total}} items done",
  fileCount: "{{count}} files",
  blocked: "Blocked",
  blockedBy: "Blocked by {{count}} items",
  selectCard: "Select {{title}}",
  /** Shown instead of a number above 99. */
  overflowCount: "99+",
  childProgress: "{{done}} of {{total}} child items done",
  assignedTo: "Assigned to {{name}}",
  assignedToMore: "Assigned to {{name}} and {{count}} more",
  commentCount: "{{count}} comments",
  moreLabels: "and {{count}} more labels",
  assignMe: "Assign to me",
  setDue: "Set due date",
  dueDay: "Due date",
  clearDue: "Clear due date",
  moreOptions: "More options",
  copyLink: "Copy link",
  duplicate: "Duplicate",
  archive: "Archive",
  delete: "Delete…",
  moveToBoard: "Move to another board…",
};

export type KanbanCardCopy = typeof en;

export const vi: KanbanCardCopy = {
  locale: "vi",
  label: "Nhãn",
  due: "Hạn",
  dueToday: "Hôm nay",
  // Vietnamese has no singular/plural distinction; both forms stay complete.
  overdueDays_one: "Quá hạn {{count}} ngày",
  overdueDays_other: "Quá hạn {{count}} ngày",
  priorityPrefix: "Ưu tiên",
  priority: {
    urgent: "Khẩn",
    high: "Cao",
    medium: "Trung bình",
    low: "Thấp",
  },
  startAndDue: "Bắt đầu {{start}}, hạn {{due}}",
  checklistProgress: "Checklist đã xong {{done}} trên {{total}} mục",
  fileCount: "{{count}} tệp",
  blocked: "Bị chặn",
  blockedBy: "Đang bị chặn bởi {{count}} việc",
  selectCard: "Chọn {{title}}",
  overflowCount: "99+",
  childProgress: "Đã xong {{done}} trên {{total}} việc con",
  assignedTo: "Được giao cho {{name}}",
  assignedToMore: "Được giao cho {{name}} và {{count}} người khác",
  commentCount: "{{count}} bình luận",
  moreLabels: "và {{count}} nhãn khác",
  assignMe: "Giao cho tôi",
  setDue: "Đặt ngày hạn",
  dueDay: "Ngày hạn",
  clearDue: "Xoá ngày hạn",
  moreOptions: "Thêm tuỳ chọn",
  copyLink: "Sao chép liên kết",
  duplicate: "Nhân bản",
  archive: "Lưu trữ",
  delete: "Xoá…",
  moveToBoard: "Chuyển sang board khác…",
};

export function kanbanCardCopy(locale: "vi" | "en"): KanbanCardCopy {
  return locale === "vi" ? vi : en;
}

/** Fills `{{name}}` placeholders; an unknown placeholder is left visible. */
export function fillCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** Picks the `_one` form for exactly 1 and `_other` otherwise, then fills it. */
export function pluralCopy(
  forms: { readonly one: string; readonly other: string },
  count: number,
): string {
  return fillCopy(count === 1 ? forms.one : forms.other, { count });
}
