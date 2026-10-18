import {
  calendarDaysFromToday,
  calendarParts,
  formatItemDate,
  itemDueState,
  pad2,
  toValidDate,
} from "../shared/item-display";
import {
  fillSheetCopy,
  pluralSheetCopy,
  type ItemSheetCopy,
} from "./item-sheet.copy";

/**
 * Date wording for the item side sheet. Every function takes the injected
 * `now` (never reads the clock) and an optional IANA `timeZone`; without one
 * the runtime time zone is used, like the Kanban card. Calendar days are
 * compared, not 24-hour spans: an item due earlier today is "today".
 */

export type SheetDueTone = "overdue" | "today" | "upcoming" | "done";

export interface SheetDue {
  readonly text: string;
  readonly tone: SheetDueTone;
}

/**
 * Due pill wording: "10/10 · quá hạn 2 ngày", "12/10 · hôm nay" or "18/10".
 * A done item shows only the date and is never overdue.
 */
export function describeSheetDue(
  dueAt: string | null | undefined,
  now: Date,
  isDone: boolean,
  copy: ItemSheetCopy,
  timeZone?: string,
): SheetDue | null {
  const state = itemDueState(dueAt, now, timeZone);
  if (!state) return null;
  const date = state.text;
  if (isDone) return { text: date, tone: "done" };
  if (state.kind === "overdue") {
    return {
      text: pluralSheetCopy(
        copy.properties.dueOverdue_one,
        copy.properties.dueOverdue_other,
        state.days,
        { date },
      ),
      tone: "overdue",
    };
  }
  if (state.kind === "today") {
    return {
      text: fillSheetCopy(copy.properties.dueToday, { date }),
      tone: "today",
    };
  }
  return { text: date, tone: "upcoming" };
}

/**
 * Comment and history time: "Vừa xong", "5 phút trước", "2 giờ trước",
 * "Hôm qua, 16:40", "5 ngày trước", then the date.
 */
export function describeSheetTime(
  value: string,
  now: Date,
  copy: ItemSheetCopy,
  timeZone?: string,
): string {
  const date = toValidDate(value);
  if (!date) return "";
  const elapsedMinutes = Math.floor((now.getTime() - date.getTime()) / 60_000);
  const days = -(calendarDaysFromToday(date, now, timeZone) ?? 0);
  const { time } = copy;
  if (elapsedMinutes < 1) return time.justNow;
  if (elapsedMinutes < 60) {
    return pluralSheetCopy(
      time.minutesAgo_one,
      time.minutesAgo_other,
      elapsedMinutes,
    );
  }
  if (days <= 0) {
    return pluralSheetCopy(
      time.hoursAgo_one,
      time.hoursAgo_other,
      Math.floor(elapsedMinutes / 60),
    );
  }
  if (days === 1) {
    const parts = calendarParts(date, timeZone);
    return fillSheetCopy(time.yesterday, {
      time: `${pad2(parts.hour)}:${pad2(parts.minute)}`,
    });
  }
  if (days < 7) {
    return pluralSheetCopy(time.daysAgo_one, time.daysAgo_other, days);
  }
  return formatItemDate(date, now, timeZone) ?? "";
}
