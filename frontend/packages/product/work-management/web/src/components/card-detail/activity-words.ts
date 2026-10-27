import type {
  ActivityDetail,
  CardActivity,
} from "@notrelix/work-management-core";

/**
 * How an item's history lines read in a language. The backend sends a stable type and a small change
 * (field, before, after, person, board names); the words live here so one line reads the same on the item
 * page and in the side sheet. Placeholders: {{field}} {{from}} {{to}} {{person}}.
 */
export type ActivityEventWords = {
  readonly someone: string;
  readonly empty: string;
  readonly loadError: string;
  readonly retry: string;
  readonly created: string;
  readonly renamed: string;
  readonly statusChanged: string;
  readonly statusSet: string;
  readonly statusChangedPlain: string;
  readonly fieldChanged: string;
  readonly fieldSet: string;
  readonly fieldChangedPlain: string;
  readonly assigned: string;
  readonly unassigned: string;
  readonly movedToGroup: string;
  readonly movedToGroupPlain: string;
  readonly movedToBoard: string;
  readonly movedToBoardPlain: string;
  readonly archived: string;
  readonly unarchived: string;
  readonly deleted: string;
  readonly restored: string;
};

export const ACTIVITY_WORDS_EN: ActivityEventWords = {
  someone: "someone",
  empty: "—",
  loadError: "The history could not be loaded.",
  retry: "Retry",
  created: "created this item",
  renamed: "renamed this item",
  statusChanged: "changed the status from {{from}} to {{to}}",
  statusSet: "set the status to {{to}}",
  statusChangedPlain: "changed the status",
  fieldChanged: "changed {{field}}",
  fieldSet: "set {{field}}",
  fieldChangedPlain: "changed a field",
  assigned: "assigned {{person}}",
  unassigned: "removed {{person}} from the assignees",
  movedToGroup: "moved this item from {{from}} to {{to}}",
  movedToGroupPlain: "moved this item to another group",
  movedToBoard: "moved this item from {{from}} to {{to}}",
  movedToBoardPlain: "moved this item to another board",
  archived: "archived this item",
  unarchived: "restored this item from the archive",
  deleted: "deleted this item",
  restored: "restored this item from deleted",
};

export const ACTIVITY_WORDS_VI: ActivityEventWords = {
  someone: "một người",
  empty: "—",
  loadError: "Không tải được lịch sử.",
  retry: "Thử lại",
  created: "tạo việc này",
  renamed: "đổi tên việc",
  statusChanged: "đổi trạng thái từ {{from}} sang {{to}}",
  statusSet: "đặt trạng thái thành {{to}}",
  statusChangedPlain: "đổi trạng thái",
  fieldChanged: "đổi {{field}}",
  fieldSet: "đặt {{field}}",
  fieldChangedPlain: "đổi một trường",
  assigned: "giao việc cho {{person}}",
  unassigned: "bỏ {{person}} khỏi người phụ trách",
  movedToGroup: "chuyển việc từ {{from}} sang {{to}}",
  movedToGroupPlain: "chuyển việc sang nhóm khác",
  movedToBoard: "chuyển việc từ bảng {{from}} sang bảng {{to}}",
  movedToBoardPlain: "chuyển việc sang bảng khác",
  archived: "lưu trữ việc này",
  unarchived: "khôi phục việc này từ kho lưu trữ",
  deleted: "xóa việc này",
  restored: "khôi phục việc này đã xóa",
};

export interface DescribedActivity {
  /** The whole sentence after the actor's name. */
  readonly verb: string;
  /** A before and after drawn under the line (renames and field changes). */
  readonly detail?: ActivityDetail;
}

function fill(template: string, values: Record<string, string>): string {
  return template.replace(
    /\{\{(\w+)\}\}/g,
    (_, key: string) => values[key] ?? "",
  );
}

const ISO_DAY = /^(\d{4})-(\d{2})-(\d{2})(?:T[\d:.]+(?:Z|[+-]\d{2}:\d{2})?)?$/;

/** A stored date reads as dd/MM/yyyy; any other value reads as it is. */
function readable(value: string): string {
  const match = ISO_DAY.exec(value);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : value;
}

/**
 * The sentence and detail of a history line from its type and change, or `null` when the type is unknown
 * (the line then reads from its own `verb`/`action`, as lines of older sources do).
 */
export function describeActivityEvent(
  event: Pick<CardActivity, "code" | "change">,
  words: ActivityEventWords,
): DescribedActivity | null {
  const change = event.change ?? {};
  const from = change.from === undefined ? undefined : readable(change.from);
  const to = change.to === undefined ? undefined : readable(change.to);
  const person = change.personName ?? words.someone;
  const diff = (label?: string): ActivityDetail | undefined =>
    from !== undefined || to !== undefined
      ? {
          kind: "diff",
          ...(label ? { label } : {}),
          from: from ?? words.empty,
          to: to ?? words.empty,
        }
      : undefined;
  const sentence = (
    template: string,
    detail?: ActivityDetail,
  ): DescribedActivity => ({
    verb: fill(template, {
      field: change.fieldName ?? "",
      from: from ?? "",
      to: to ?? "",
      person,
    }).trim(),
    ...(detail ? { detail } : {}),
  });

  switch (event.code) {
    case "work.item-created":
      return sentence(words.created);
    case "work.item-renamed":
      return sentence(words.renamed, diff());
    case "work.item-status-changed":
      return sentence(
        from !== undefined && to !== undefined
          ? words.statusChanged
          : to !== undefined
            ? words.statusSet
            : words.statusChangedPlain,
      );
    case "work.item-field-changed":
      return change.fieldName === undefined
        ? sentence(words.fieldChangedPlain)
        : sentence(
            to !== undefined && from === undefined
              ? words.fieldSet
              : words.fieldChanged,
            diff(),
          );
    case "work.item-assigned":
      return sentence(words.assigned);
    case "work.item-unassigned":
      return sentence(words.unassigned);
    case "work.item-moved":
      return sentence(
        from !== undefined && to !== undefined
          ? words.movedToGroup
          : words.movedToGroupPlain,
      );
    case "work.item-moved-to-board":
      return sentence(
        from !== undefined && to !== undefined
          ? words.movedToBoard
          : words.movedToBoardPlain,
      );
    case "work.item-archived":
      return sentence(words.archived);
    case "work.item-unarchived":
      return sentence(words.unarchived);
    case "work.item-deleted":
      return sentence(words.deleted);
    case "work.item-restored":
      return sentence(words.restored);
    default:
      return null;
  }
}
