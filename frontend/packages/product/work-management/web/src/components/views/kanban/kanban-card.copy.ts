/**
 * Kanban card copy, vi and en.
 *
 * Work Management web has no locale wiring yet and may not import
 * `@notrelix/i18n` (architecture manifest), so this is plain data keyed by
 * locale: the card takes a `copy` prop and defaults to English. Messages use
 * the same `{{name}}` placeholders as the feature catalogs
 * (`packages/features/workspace/src/web/members/workspace-members.copy.ts`)
 * so registering a catalog later is a move, not a rewrite.
 */
export const en = {
  /** BCP 47 tag used to format the due date. */
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
  comments: "{{count}} comments",
  attachments: "{{count}} attachments",
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
  comments: "{{count}} bình luận",
  attachments: "{{count}} tệp đính kèm",
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
