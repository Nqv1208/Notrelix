import type { Card, KanbanStatusLane } from "@notrelix/work-management-core";
import {
  calendarParts,
  identityMarkClass,
  toValidDate,
} from "../../shared/item-display";
import type { CalendarCopy } from "./calendar.copy";
import {
  diffDays,
  formatDayMonth,
  toDayKey,
  type CalendarEntry,
  type CalendarTone,
  type CalendarUndated,
  type DayKey,
} from "./calendar-model";

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** The wall-clock day of an ISO date-time in `timeZone`, or `null`. */
export function dayOf(
  iso: string | null | undefined,
  timeZone?: string,
): { day: DayKey; minutes: number } | null {
  if (!iso) return null;
  const date = toValidDate(iso);
  if (!date) return null;
  const parts = calendarParts(date, timeZone);
  return {
    day: toDayKey(parts.year, parts.month, parts.day),
    minutes: parts.hour * 60 + parts.minute,
  };
}

/**
 * The item's due as an ISO date-time. `dueDate` is first: a write patches it
 * at once, while `dueAt` of the board read only changes with the refetch.
 */
export function cardDue(card: Card): string | undefined {
  return card.dueDate ?? card.dueAt ?? undefined;
}

function toneOf(
  category: KanbanStatusLane["category"],
  endDay: DayKey,
  today: DayKey,
): CalendarTone {
  if (category === "done") return "done";
  if (endDay < today) return "overdue";
  if (category === "review") return "review";
  if (category === "in_progress") return "doing";
  return "todo";
}

export interface BuiltCalendar {
  readonly entries: CalendarEntry[];
  readonly undated: CalendarUndated[];
}

/**
 * Turns the board's items into calendar entries: an item with a due day sits
 * on it (over its start-to-due span when it has an earlier start), an item
 * with only a start sits on the start day, an item with neither is undated.
 * The colour comes from the Status category of its lane; an unfinished item
 * whose last day is before today is overdue.
 */
export function buildCalendarEntries({
  lanes,
  today,
  copy,
  timeZone,
}: {
  lanes: readonly KanbanStatusLane[];
  today: DayKey;
  copy: CalendarCopy;
  timeZone?: string;
}): BuiltCalendar {
  const entries: CalendarEntry[] = [];
  const undated: CalendarUndated[] = [];
  for (const lane of lanes) {
    for (const card of lane.cards) {
      const due = dayOf(cardDue(card), timeZone);
      const start = dayOf(card.startDate, timeZone);
      const person = card.members[0];
      if (!due && !start) {
        undated.push({
          id: card.id,
          itemKey: card.itemKey,
          title: card.title,
          assigneeName: person?.name ?? null,
        });
        continue;
      }
      const endDay = due?.day ?? (start?.day as DayKey);
      const startDay = start && start.day < endDay ? start.day : endDay;
      const tone = toneOf(lane.category, endDay, today);
      entries.push({
        id: card.id,
        kind: "item",
        itemKey: card.itemKey,
        title: card.title,
        tone,
        startDay,
        endDay,
        dueMinutes: due?.minutes ?? null,
        assignee: person
          ? {
              initials: person.initials,
              markClass: identityMarkClass(person.userId),
              name: person.name,
            }
          : null,
        note: noteOf({
          card,
          category: lane.category,
          startDay,
          endDay,
          today,
          hasDue: due !== null,
          timeZone,
          copy,
        }),
        movable: due !== null,
      });
    }
  }
  return { entries, undated };
}

function noteOf({
  card,
  category,
  startDay,
  endDay,
  today,
  hasDue,
  timeZone,
  copy,
}: {
  card: Card;
  category: KanbanStatusLane["category"];
  startDay: DayKey;
  endDay: DayKey;
  today: DayKey;
  hasDue: boolean;
  timeZone?: string;
  copy: CalendarCopy;
}): string {
  const { note } = copy;
  const parts: string[] = [];
  if (category === "done") {
    const completed = dayOf(card.completedAt, timeZone);
    parts.push(
      completed
        ? fill(note.done, { date: formatDayMonth(completed.day) })
        : note.completed,
    );
    return parts.join(note.separator);
  }
  if (category === "review") parts.push(note.review);
  if (startDay < endDay) {
    parts.push(fill(note.range, { start: formatDayMonth(startDay) }));
  }
  if (hasDue) {
    parts.push(
      endDay === today
        ? note.dueToday
        : fill(note.due, { date: formatDayMonth(endDay) }),
    );
  }
  if (hasDue && endDay < today) {
    const days = diffDays(endDay, today);
    parts.push(
      fill(days === 1 ? note.overdue_one : note.overdue_other, { days }),
    );
  }
  return parts.join(note.separator);
}

/**
 * The instant of `hour`:`minute` on `day` in `timeZone` (the runtime zone
 * when omitted), as an ISO date-time. The zone's offset is read at that
 * moment, so it holds for zones with a daylight-saving change too.
 */
export function zonedInstant(
  day: DayKey,
  hour: number,
  minute: number,
  timeZone?: string,
): string {
  const [year = 1970, month = 1, date = 1] = day.split("-").map(Number);
  const wall = Date.UTC(year, month - 1, date, hour, minute);
  let instant = wall;
  // Two passes settle the offset around a daylight-saving change.
  for (let pass = 0; pass < 2; pass += 1) {
    const parts = calendarParts(new Date(instant), timeZone);
    const shown = Date.UTC(
      parts.year,
      parts.month - 1,
      parts.day,
      parts.hour,
      parts.minute,
    );
    instant += wall - shown;
  }
  return new Date(instant).toISOString();
}

/** An ISO date-time moved by whole days (the wall time is kept in zones without daylight saving). */
export function shiftInstant(iso: string, days: number): string {
  return new Date(Date.parse(iso) + days * 86_400_000).toISOString();
}
