import {
  inferStatusCategory,
  isTableCardDone,
  type Card,
  type KanbanStatusLane,
  type WorkStatusCategory,
} from "@notrelix/work-management-core";
import {
  calendarDaysFromToday,
  formatItemDate,
  itemDueState,
  itemLabelTone,
  type ItemLabelTone,
} from "../../shared/item-display";
import { pluralTableCopy, type MainTableCopy } from "./main-table.copy";

/** A status the table can show or choose: a Status option of the board. */
export interface TableStatusOption {
  readonly id: string;
  readonly name: string;
  readonly category: WorkStatusCategory;
}

export type TableDueKind = "overdue" | "today" | "date" | "none";

/** One row of the table, ready to render (the clock and copy already applied). */
export interface TableRowModel {
  readonly id: string;
  readonly key: string | null;
  readonly title: string;
  readonly status: TableStatusOption | null;
  readonly assignee: {
    readonly userId: string;
    readonly name: string;
    readonly initials: string;
    readonly more: number;
  } | null;
  readonly due: { readonly kind: TableDueKind; readonly text: string };
  /** The due instant as the board gives it; null when none. */
  readonly dueAt: string | null;
  readonly priority: "urgent" | "high" | "medium" | "low" | null;
  readonly label: {
    readonly name: string;
    readonly tone: ItemLabelTone;
  } | null;
  readonly progress: {
    readonly done: number;
    readonly total: number;
    readonly percent: number;
  } | null;
  /** Checklist items done over all items; null when the item has none. */
  readonly checklist: { readonly done: number; readonly total: number } | null;
  readonly commentCount: number;
  readonly fileCount: number;
  readonly updatedText: string;
  readonly startText: string | null;
  readonly createdText: string | null;
  readonly completedText: string | null;
}

/** One group of the table: a lane of the board and its rows. */
export interface TableGroupModel {
  readonly id: string;
  readonly name: string;
  readonly category: WorkStatusCategory | null;
  /** The colour the person gave the option; null when it has none. */
  readonly color: string | null;
  readonly total: number;
  readonly overdueCount: number;
  readonly rows: readonly TableRowModel[];
}

/**
 * "Cập nhật": minutes, hours, "Hôm qua", days, "Tuần trước", then the date.
 * Days are calendar days against `now`; hours only within the same day.
 */
export function formatUpdatedAt(
  iso: string | null | undefined,
  now: Date,
  copy: MainTableCopy["row"]["updated"],
  timeZone?: string,
): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const elapsed = now.getTime() - date.getTime();
  const days = calendarDaysFromToday(date, now, timeZone);
  if (days === null) return "";
  if (elapsed < 60_000 && elapsed > -60_000) return copy.justNow;
  if (days === 0 && elapsed >= 0) {
    const minutes = Math.floor(elapsed / 60_000);
    if (minutes < 60)
      return pluralTableCopy(
        { one: copy.minutes_one, other: copy.minutes_other },
        minutes,
      );
    return pluralTableCopy(
      { one: copy.hours_one, other: copy.hours_other },
      Math.floor(elapsed / 3_600_000),
    );
  }
  const ago = -days;
  if (ago === 1) return copy.yesterday;
  if (ago >= 2 && ago <= 6)
    return pluralTableCopy({ one: copy.days_one, other: copy.days_other }, ago);
  if (ago >= 7 && ago <= 13) return copy.lastWeek;
  return formatItemDate(date, now, timeZone) ?? "";
}

function initialsOf(member: { initials: string; name: string }): string {
  const initials = member.initials.trim() || Array.from(member.name)[0] || "?";
  return initials.toUpperCase();
}

export interface BuildTableRowOptions {
  readonly now: Date;
  readonly copy: MainTableCopy;
  readonly timeZone?: string;
  /** Status options of the board by id. */
  readonly statuses: ReadonlyMap<string, TableStatusOption>;
}

export function buildTableRow(
  card: Card,
  { now, copy, timeZone, statuses }: BuildTableRowOptions,
): TableRowModel {
  const done = isTableCardDone(card);
  const dueIso = card.dueAt ?? card.dueDate;
  const due = itemDueState(dueIso, now, timeZone);
  const dueModel: TableRowModel["due"] = !due
    ? { kind: "none", text: copy.row.dueNone }
    : done
      ? { kind: "date", text: due.text }
      : due.kind === "overdue"
        ? {
            kind: "overdue",
            text: pluralTableCopy(
              {
                one: copy.row.overdueDays_one,
                other: copy.row.overdueDays_other,
              },
              due.days,
            ),
          }
        : due.kind === "today"
          ? { kind: "today", text: copy.row.dueToday }
          : { kind: "date", text: due.text };

  const member = card.members?.[0];
  const label = card.labels?.[0];
  const progress =
    card.childProgress && card.childProgress.total > 0
      ? card.childProgress
      : null;

  return {
    id: card.id,
    key: card.itemKey?.trim() || null,
    title: card.title,
    status: card.statusOptionId
      ? (statuses.get(card.statusOptionId) ?? null)
      : null,
    assignee: member
      ? {
          userId: member.userId || member.id,
          name: member.name,
          initials: initialsOf(member),
          more: Math.max((card.members?.length ?? 0) - 1, 0),
        }
      : null,
    due: dueModel,
    dueAt: dueIso ?? null,
    priority: card.priority ?? null,
    label: label?.name
      ? { name: label.name, tone: itemLabelTone(label) }
      : null,
    progress: progress
      ? {
          done: progress.done,
          total: progress.total,
          percent: Math.max(
            4,
            Math.min(100, Math.round((progress.done / progress.total) * 100)),
          ),
        }
      : null,
    checklist:
      card.checklistProgress && card.checklistProgress.total > 0
        ? card.checklistProgress
        : null,
    commentCount: Math.max(card._count?.comments ?? 0, 0),
    fileCount: Math.max(card._count?.attachments ?? 0, 0),
    updatedText: formatUpdatedAt(
      card.updatedAt,
      now,
      copy.row.updated,
      timeZone,
    ),
    startText: card.startDate
      ? formatItemDate(card.startDate, now, timeZone)
      : null,
    createdText: card.createdAt
      ? formatItemDate(card.createdAt, now, timeZone)
      : null,
    completedText: card.completedAt
      ? formatItemDate(card.completedAt, now, timeZone)
      : null,
  };
}

/** Status options of a board's Status field as table statuses. */
export function tableStatusOptions(
  options: readonly {
    id: string;
    label: string;
    category?: WorkStatusCategory | null;
  }[],
): TableStatusOption[] {
  return options.map((option, index) => ({
    id: option.id,
    name: option.label,
    category: option.category ?? inferStatusCategory(option.label, index),
  }));
}

export function buildTableGroup(
  lane: {
    readonly optionId: string | null;
    readonly name: string;
    readonly category: KanbanStatusLane["category"];
    readonly color: string | null;
    readonly total: number;
    readonly overdueCount: number;
    readonly cards: readonly Card[];
  },
  name: string,
  options: BuildTableRowOptions,
): TableGroupModel {
  return {
    id: lane.optionId ?? "none",
    name,
    category: lane.category,
    color: lane.color,
    total: lane.total,
    overdueCount: lane.overdueCount,
    rows: lane.cards.map((card) => buildTableRow(card, options)),
  };
}
