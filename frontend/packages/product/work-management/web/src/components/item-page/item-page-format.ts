import type {
  CardActivity,
  CardFile,
  CardUpdate,
} from "@notrelix/work-management-core";
import {
  calendarDaysFromToday,
  calendarParts,
  formatItemDate,
  pad2,
  toValidDate,
} from "../shared/item-display";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type { ItemPageCopy } from "./item-page.copy";
import type { ItemPageTab } from "./item-page-types";

/**
 * Wording and ordering for the item page. Pure: the clock and the time zone
 * are injected, like the side sheet's formatters.
 */

/** "23/09/2026" (day, month, year). */
export function formatFullDate(
  value: string | Date | null | undefined,
  timeZone?: string,
): string | null {
  const date = value ? toValidDate(value) : null;
  if (!date) return null;
  const parts = calendarParts(date, timeZone);
  return `${pad2(parts.day)}/${pad2(parts.month)}/${parts.year}`;
}

/** "23/09/2026, 15:10". */
export function formatDateTime(
  value: string | Date | null | undefined,
  timeZone?: string,
): string | null {
  const date = value ? toValidDate(value) : null;
  if (!date) return null;
  const parts = calendarParts(date, timeZone);
  return `${formatFullDate(date, timeZone)}, ${pad2(parts.hour)}:${pad2(parts.minute)}`;
}

/** "10:06 hôm nay", "16:40 hôm qua", then the date. */
export function formatEditedAt(
  value: string | Date | null | undefined,
  now: Date,
  copy: ItemPageCopy,
  timeZone?: string,
): string | null {
  const date = value ? toValidDate(value) : null;
  if (!date) return null;
  const parts = calendarParts(date, timeZone);
  const time = `${pad2(parts.hour)}:${pad2(parts.minute)}`;
  const days = calendarDaysFromToday(date, now, timeZone);
  if (days === 0) return fillSheetCopy(copy.meta.today, { time });
  if (days === -1) return fillSheetCopy(copy.meta.yesterday, { time });
  return formatItemDate(date, now, timeZone);
}

/** "16:40". */
export function formatClock(value: string, timeZone?: string): string {
  const date = toValidDate(value);
  if (!date) return "";
  const parts = calendarParts(date, timeZone);
  return `${pad2(parts.hour)}:${pad2(parts.minute)}`;
}

/** Day heading of the activity list: "Hôm nay", "Hôm qua", else "05/10". */
export function formatDayHeading(
  value: string,
  now: Date,
  copy: ItemPageCopy,
  timeZone?: string,
): string {
  const days = calendarDaysFromToday(value, now, timeZone);
  if (days === 0) return copy.activity.today;
  if (days === -1) return copy.activity.yesterday;
  return formatItemDate(value, now, timeZone) ?? "";
}

/** A day key that groups entries of one calendar day. */
function dayKey(value: string, timeZone?: string): string {
  const date = toValidDate(value);
  if (!date) return "";
  const parts = calendarParts(date, timeZone);
  return `${parts.year}-${pad2(parts.month)}-${pad2(parts.day)}`;
}

/** "18,4 MB", "340 KB". */
export function formatFileSize(bytes: number, locale: string): string {
  const units = ["B", "KB", "MB", "GB"] as const;
  let value = Math.max(0, bytes);
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }
  const text = new Intl.NumberFormat(locale, {
    maximumFractionDigits: unit === 0 ? 0 : 1,
  }).format(value);
  return `${text} ${units[unit]}`;
}

export type FileKind = "sheet" | "pdf" | "other";

const SHEET_EXTENSIONS = new Set(["xls", "xlsx", "xlsm", "csv", "ods"]);

/** The file type chip: its label ("XLSX") and the tone it is drawn in. */
export function describeFile(file: Pick<CardFile, "name" | "contentType">): {
  readonly label: string;
  readonly kind: FileKind;
} {
  const dot = file.name.lastIndexOf(".");
  const extension = dot >= 0 ? file.name.slice(dot + 1).toLowerCase() : "";
  const label = (extension || "FILE").slice(0, 4).toUpperCase();
  if (
    SHEET_EXTENSIONS.has(extension) ||
    /spreadsheet|excel/.test(file.contentType)
  )
    return { label, kind: "sheet" };
  if (extension === "pdf" || file.contentType === "application/pdf")
    return { label, kind: "pdf" };
  return { label, kind: "other" };
}

export type ActivityEntry =
  | {
      readonly kind: "comment";
      readonly at: string;
      readonly update: CardUpdate;
    }
  | {
      readonly kind: "event";
      readonly at: string;
      readonly event: CardActivity;
    };

export interface ActivityDay {
  readonly key: string;
  readonly heading: string;
  readonly entries: readonly ActivityEntry[];
}

export interface ActivityCounts {
  readonly all: number;
  readonly comments: number;
  readonly history: number;
}

export function countActivity(
  updates: readonly CardUpdate[],
  activity: readonly CardActivity[],
): ActivityCounts {
  return {
    all: updates.length + activity.length,
    comments: updates.length,
    history: activity.length,
  };
}

/**
 * The activity list for one tab, grouped by calendar day. Oldest first by
 * default; ties keep the comments before the history lines of the same time.
 * With `limit`, only the newest `limit` entries are listed and the older ones
 * come back in `hidden` (oldest first), for "Hiện N hoạt động cũ hơn".
 */
export function buildActivityDays({
  updates,
  activity,
  tab,
  newestFirst,
  now,
  copy,
  timeZone,
  limit,
}: {
  updates: readonly CardUpdate[];
  activity: readonly CardActivity[];
  tab: ItemPageTab;
  newestFirst: boolean;
  now: Date;
  copy: ItemPageCopy;
  timeZone?: string;
  limit?: number;
}): { days: ActivityDay[]; hidden: ActivityEntry[] } {
  const entries: ActivityEntry[] = [];
  if (tab !== "history") {
    for (const update of updates)
      entries.push({ kind: "comment", at: update.createdAt, update });
  }
  if (tab !== "comments") {
    for (const event of activity)
      entries.push({ kind: "event", at: event.createdAt, event });
  }
  const time = (entry: ActivityEntry) => toValidDate(entry.at)?.getTime() ?? 0;
  entries.sort((a, b) => time(a) - time(b));
  const hidden =
    limit !== undefined && entries.length > limit
      ? entries.splice(0, entries.length - limit)
      : [];
  if (newestFirst) entries.reverse();

  const days: { key: string; heading: string; entries: ActivityEntry[] }[] = [];
  for (const entry of entries) {
    const key = dayKey(entry.at, timeZone);
    const last = days[days.length - 1];
    if (last && last.key === key) last.entries.push(entry);
    else
      days.push({
        key,
        heading: formatDayHeading(entry.at, now, copy, timeZone),
        entries: [entry],
      });
  }
  return { days, hidden };
}

/** "dd/MM" of an entry's day, for the range of the hidden entries. */
export function formatEntryDay(
  value: string,
  now: Date,
  timeZone?: string,
): string {
  return formatItemDate(value, now, timeZone) ?? "";
}
