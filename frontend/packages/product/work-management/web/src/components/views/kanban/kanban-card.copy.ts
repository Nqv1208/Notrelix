/**
 * Kanban card copy, vi and en (WX-BOARD-001), registered as the `kanban-card`
 * catalog (`kanban-card.catalog.ts`). The pure surface takes a `copy` prop;
 * `useKanbanCardCopy` resolves it for the active locale in the container.
 * Placeholders use the catalog `{{name}}` form and are filled by `fillCopy`.
 */
export const en = {
  /** BCP 47 tag the surface formats the due date with. */
  locale: "en",
  moveCard: "Move {{title}}",
  cardMenu: "Open {{title}} menu",
  docLinked: "Doc linked",
  due: "Due",
  overdue: "Overdue",
  assignedTo: "Assigned to {{name}}",
  assignedToMore: "Assigned to {{name}} and {{count}} more",
  priority: {
    urgent: "Urgent priority",
    high: "High priority",
  },
  status: {
    notStarted: "Not started",
    working: "Working on it",
    stuck: "Stuck",
    done: "Done",
    completed: "Completed",
  },
  checklist: "{{done}} of {{total}} checklist items done",
  comments_one: "{{count}} comment",
  comments_other: "{{count}} comments",
  attachments_one: "{{count}} attachment",
  attachments_other: "{{count}} attachments",
};

export type KanbanCardCopy = typeof en;

export const vi: KanbanCardCopy = {
  locale: "vi",
  moveCard: "Di chuyển {{title}}",
  cardMenu: "Mở menu của {{title}}",
  docLinked: "Đã liên kết tài liệu",
  due: "Hạn",
  overdue: "Quá hạn",
  assignedTo: "Được giao cho {{name}}",
  assignedToMore: "Được giao cho {{name}} và {{count}} người khác",
  priority: {
    urgent: "Ưu tiên khẩn cấp",
    high: "Ưu tiên cao",
  },
  status: {
    notStarted: "Chưa bắt đầu",
    working: "Đang thực hiện",
    stuck: "Bị kẹt",
    done: "Hoàn tất",
    completed: "Đã hoàn thành",
  },
  checklist: "Đã xong {{done}} trên {{total}} mục kiểm tra",
  // Vietnamese has no singular/plural distinction; both forms stay complete.
  comments_one: "{{count}} bình luận",
  comments_other: "{{count}} bình luận",
  attachments_one: "{{count}} tệp đính kèm",
  attachments_other: "{{count}} tệp đính kèm",
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
