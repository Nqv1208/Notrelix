import { Lock } from "lucide-react";
import {
  cn,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetTitle,
} from "@notrelix/ui-web";
import { entryLabel } from "./calendar-event";
import { weekdayName } from "./calendar-format";
import {
  diffDays,
  formatDayMonth,
  monthWeeks,
  sameMonth,
  weekDays,
  type CalendarEntry,
  type CalendarRange,
  type CalendarTone,
  type CalendarUndated,
  type DayKey,
} from "./calendar-model";
import type { CalendarCopy } from "./calendar.copy";

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** The days the 320 px agenda lists: the anchor's month, its week, or the day itself. */
export function agendaPeriod(range: CalendarRange, anchor: DayKey): DayKey[] {
  if (range === "month")
    return monthWeeks(anchor)
      .flat()
      .filter((day) => sameMonth(day, anchor));
  if (range === "week") return weekDays(anchor);
  return [anchor];
}

export interface AgendaGroup {
  readonly day: DayKey;
  readonly entries: readonly CalendarEntry[];
}

/**
 * The 320 px agenda (`WM-TLCal-320`, Lịch 320): every entry of the period under the day it starts on, or under the
 * period's first day when it started before; days in order, entries by start, then by title.
 */
export function agendaGroups(
  entries: readonly CalendarEntry[],
  period: readonly DayKey[],
): AgendaGroup[] {
  const first = period[0];
  const last = period[period.length - 1];
  if (!first || !last) return [];
  const byDay = new Map<DayKey, CalendarEntry[]>();
  for (const entry of entries) {
    if (entry.endDay < first || entry.startDay > last) continue;
    const day = entry.startDay < first ? first : entry.startDay;
    const list = byDay.get(day) ?? [];
    list.push(entry);
    byDay.set(day, list);
  }
  return [...byDay.entries()]
    .sort(([a], [b]) => (a < b ? -1 : a > b ? 1 : 0))
    .map(([day, list]) => ({
      day,
      entries: [...list].sort(
        (a, b) =>
          (a.startDay < b.startDay ? -1 : a.startDay > b.startDay ? 1 : 0) ||
          a.title.localeCompare(b.title),
      ),
    }));
}

const GLYPH_TONE: Readonly<Record<CalendarTone, string>> = {
  todo: "text-work-status-todo",
  doing: "text-work-status-doing",
  review: "text-calendar-dot-review",
  done: "text-work-status-done",
  overdue: "text-destructive",
};

/** The 13 px status mark of a row (`.sg`): empty, half, ringed, filled with a tick. */
export function StatusGlyph({ tone }: { tone: CalendarTone }) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      data-status-glyph={tone}
      className={cn(
        "size-3.25 flex-none fill-none stroke-current stroke-2 [stroke-linecap:round] [stroke-linejoin:round]",
        GLYPH_TONE[tone],
      )}
    >
      {tone === "done" ? (
        <>
          <circle cx="12" cy="12" r="9" fill="currentColor" />
          <path d="m8.5 12 2.5 2.5 4.5-5" className="stroke-popover" />
        </>
      ) : (
        <>
          <circle cx="12" cy="12" r="8" />
          {tone === "doing" || tone === "overdue" ? (
            <path d="M12 4a8 8 0 0 1 0 16z" fill="currentColor" />
          ) : null}
          {tone === "review" ? (
            <circle cx="12" cy="12" r="3" fill="currentColor" />
          ) : null}
        </>
      )}
    </svg>
  );
}

export interface CalendarNarrowAgendaProps {
  copy: CalendarCopy;
  locale: string;
  range: CalendarRange;
  anchorDay: DayKey;
  entries: readonly CalendarEntry[];
  canEdit: boolean;
  onOpenItem: (id: string) => void;
  /** "Đổi ngày…": opens the dates dialog of the entry. */
  onEditDates?: ((entry: CalendarEntry) => void) | undefined;
}

/**
 * Lịch 320 (`WM-TLCal-320`, frame 3): no grid and no drag. The period's entries as 44 px touch rows under their
 * day, each with its span and an "Đổi ngày…" button that opens the dates dialog; a failed save keeps the new
 * dates shown and the container's toast offers Thử lại.
 */
export function CalendarNarrowAgenda({
  copy,
  locale,
  range,
  anchorDay,
  entries,
  canEdit,
  onOpenItem,
  onEditDates,
}: CalendarNarrowAgendaProps) {
  const t = copy.phone;
  const groups = agendaGroups(entries, agendaPeriod(range, anchorDay));
  if (groups.length === 0)
    return (
      <p className="m-0 px-3 py-4 text-[14px] text-meta-foreground">
        {t.nothingInRange}
      </p>
    );
  return (
    <>
      {groups.map((group) => (
        <section key={group.day} data-phone-agenda-day={group.day}>
          <h3 className="m-0 px-3 pt-2.5 pb-1.5 text-[12px] leading-[1.45] font-semibold text-meta-foreground">
            {`${weekdayName(group.day, locale)} ${formatDayMonth(group.day)}`}
          </h3>
          <ul className="m-0 list-none p-0">
            {group.entries.map((entry) => {
              const name = entry.itemKey ?? entry.title;
              const days = diffDays(entry.startDay, entry.endDay) + 1;
              return (
                <li
                  key={entry.id}
                  data-phone-row={entry.id}
                  className="box-border flex min-h-16 flex-wrap items-center gap-2 border-b border-line bg-popover px-3 py-1.5 dark:bg-card"
                >
                  <button
                    type="button"
                    data-calendar-event={entry.id}
                    aria-label={entryLabel(entry)}
                    onClick={() => onOpenItem(entry.id)}
                    className="flex w-full min-w-0 cursor-pointer items-center gap-2 border-0 bg-transparent p-0 text-left text-[14px] leading-[1.45] text-foreground focus-visible:outline-2 focus-visible:outline-ring"
                  >
                    <StatusGlyph tone={entry.tone} />
                    {entry.itemKey ? (
                      <span className="flex-none font-mono text-[11.5px] leading-[1.45] text-meta-foreground">
                        {entry.itemKey}
                      </span>
                    ) : null}
                    <span className="min-w-0 flex-1 truncate text-[13px] font-medium">
                      {entry.title}
                    </span>
                  </button>
                  <span className="flex-1 text-[12px] leading-[1.45] text-meta-foreground tabular-nums">
                    {days > 1
                      ? fill(t.span, {
                          from: formatDayMonth(entry.startDay),
                          to: formatDayMonth(entry.endDay),
                          days,
                        })
                      : formatDayMonth(entry.endDay)}
                  </span>
                  {canEdit && entry.movable && onEditDates ? (
                    <button
                      type="button"
                      aria-label={fill(t.changeDatesLabel, { key: name })}
                      onClick={() => onEditDates(entry)}
                      // 28 px drawn, 44 px to the finger: the hit area reaches 8 px above and below.
                      className="relative box-border inline-flex h-7 flex-none cursor-pointer items-center rounded-[7px] border border-line-strong bg-popover px-2.5 text-[12.5px] leading-[1.45] font-medium whitespace-nowrap text-foreground before:absolute before:inset-x-0 before:-inset-y-2 before:content-[''] focus-visible:outline-2 focus-visible:outline-ring dark:bg-card"
                    >
                      {t.changeDates}
                    </button>
                  ) : null}
                </li>
              );
            })}
          </ul>
        </section>
      ))}
    </>
  );
}

export interface CalendarUndatedSheetProps {
  copy: CalendarCopy;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  undatedTotal: number;
  undated: readonly CalendarUndated[];
  filterActive: boolean;
  canEdit: boolean;
  onSetDue: (id: string) => void;
}

/**
 * Khay 320 (`WM-TLCal-320`, frame 4): the undated items in a sheet from the bottom. A viewer reads them only: a
 * note says why, and each item is a locked chip. An editor gets "Đặt ngày" on each item instead (the artboard's
 * own note), which sets the shown day.
 */
export function CalendarUndatedSheet({
  copy,
  open,
  onOpenChange,
  undatedTotal,
  undated,
  filterActive,
  canEdit,
  onSetDue,
}: CalendarUndatedSheetProps) {
  const t = copy.phone;
  const hidden = undatedTotal - undated.length;
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="bottom"
        showCloseButton={false}
        data-phone-tray=""
        className="box-border max-h-[85dvh] gap-2.5 overflow-y-auto rounded-t-[14px] border border-b-0 border-line bg-popover px-4 pt-2.5 pb-4 font-body text-[14px] leading-[1.45] text-foreground shadow-[0_12px_32px_rgba(15,23,42,.16)] dark:bg-card dark:shadow-[0_12px_32px_rgba(0,0,0,.55)]"
      >
        <span
          aria-hidden="true"
          data-phone-tray-grab=""
          className="h-1 w-9 flex-none self-center rounded-[2px] bg-line-strong"
        />
        <div className="flex items-center gap-2">
          <SheetTitle className="m-0 flex-1 text-[14px] leading-[1.45] font-bold text-foreground">
            {t.trayTitle}
          </SheetTitle>
          <span
            data-phone-tray-count=""
            className="rounded-full bg-state-hover-strong px-1.75 text-[12px] leading-[1.45] font-semibold text-muted-foreground"
          >
            {undatedTotal}
          </span>
        </div>
        {!canEdit && undated.length > 0 ? (
          <SheetDescription
            role="note"
            data-phone-tray-note=""
            className="m-0 flex items-start gap-2 rounded-[8px] bg-warning-subtle px-3 py-2.5 text-[12.5px] leading-[1.45] text-warning-text dark:bg-work-status-review/12 dark:text-work-status-review-text"
          >
            <Lock className="size-4 flex-none" aria-hidden="true" />
            <span className="min-w-0 flex-1 text-foreground">
              {t.trayReadOnly}
            </span>
          </SheetDescription>
        ) : (
          <SheetDescription className="sr-only">
            {undated.length > 0 ? copy.undated.hint : t.trayEmpty}
          </SheetDescription>
        )}
        {undated.length === 0 ? (
          <p className="m-0 text-[13px] text-meta-foreground">{t.trayEmpty}</p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {undated.map((item) => {
              const name = item.itemKey ?? item.title;
              const tone = item.tone ?? "todo";
              return (
                <li key={item.id}>
                  {canEdit ? (
                    <div
                      data-phone-tray-item={item.id}
                      className="box-border flex min-h-8.5 w-full items-center gap-2 rounded-[8px] border border-line-strong bg-popover pr-1 pl-2.5 text-[12.5px] font-medium text-foreground dark:bg-card"
                    >
                      <StatusGlyph tone={tone} />
                      {item.itemKey ? (
                        <span className="flex-none font-mono text-[11.5px] text-meta-foreground">
                          {item.itemKey}
                        </span>
                      ) : null}
                      <span className="min-w-0 flex-1 truncate">
                        {item.title}
                      </span>
                      <button
                        type="button"
                        aria-label={fill(copy.undated.setDueLabel, {
                          key: name,
                        })}
                        onClick={() => onSetDue(item.id)}
                        className="relative inline-flex h-6.5 flex-none cursor-pointer items-center rounded-[6px] border border-line-strong bg-popover px-2 text-[12px] font-medium whitespace-nowrap text-foreground before:absolute before:inset-x-0 before:-inset-y-2.25 before:content-[''] focus-visible:outline-2 focus-visible:outline-ring dark:bg-card"
                      >
                        {copy.undated.setDue}
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      data-phone-tray-item={item.id}
                      aria-disabled="true"
                      aria-label={fill(t.trayLocked, {
                        key: item.itemKey ?? "",
                        title: item.title,
                      }).trim()}
                      className="box-border flex h-8.5 w-full cursor-default items-center gap-2 rounded-[8px] border border-line-strong bg-state-hover px-2.5 text-left text-[12.5px] leading-[1.45] font-medium text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring"
                    >
                      <StatusGlyph tone={tone} />
                      {item.itemKey ? (
                        <span className="flex-none font-mono text-[11.5px] text-meta-foreground">
                          {item.itemKey}
                        </span>
                      ) : null}
                      <span className="min-w-0 flex-1 truncate">
                        {item.title}
                      </span>
                      <Lock className="size-3 flex-none" aria-hidden="true" />
                    </button>
                  )}
                </li>
              );
            })}
          </ul>
        )}
        {filterActive && hidden > 0 ? (
          <p className="m-0 text-[12.5px] text-meta-foreground">
            {hidden === 1
              ? copy.undated.hiddenByFilter_one
              : copy.undated.hiddenByFilter_other}
          </p>
        ) : null}
      </SheetContent>
    </Sheet>
  );
}
