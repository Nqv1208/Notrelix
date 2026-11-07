import { useState } from "react";
import {
  CalendarDays,
  CalendarX2,
  ChevronLeft,
  ChevronRight,
  ChevronUp,
} from "lucide-react";
import { cn, useMediaQuery } from "@notrelix/ui-web";
import { CalendarAside } from "./calendar-aside";
import {
  CalendarNarrowAgenda,
  CalendarUndatedSheet,
} from "./calendar-phone-narrow";
import { entryLabel } from "./calendar-event";
import {
  addDays,
  diffDays,
  formatDayMonth,
  monthWeeks,
  parseDayKey,
  sameMonth,
  startOfWeek,
  stepAnchor,
  weekDays,
  type CalendarEntry,
  type CalendarRange,
  type CalendarTone,
  type CalendarUndated,
  type DayKey,
} from "./calendar-model";
import { calendarHeading, weekdayName } from "./calendar-format";
import type { CalendarCopy } from "./calendar.copy";

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

const DOT: Readonly<Record<CalendarTone, string>> = {
  todo: "border border-work-status-todo",
  doing: "bg-work-status-doing",
  review: "bg-calendar-dot-review",
  done: "bg-work-status-done",
  overdue: "bg-destructive",
};

const BAR: Readonly<Record<CalendarTone, string>> = {
  todo: "bg-work-status-todo",
  doing: "bg-work-status-doing",
  review: "bg-work-status-review",
  done: "bg-work-status-done",
  overdue: "bg-destructive",
};

const WEEKDAYS = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"] as const;
const LEGEND = ["overdue", "doing", "review", "todo"] as const;
const BAR_LANES = 2;

/** The tones marked under a day: an item's due day, a phase's first and last day. */
function markersOn(entries: readonly CalendarEntry[], day: DayKey) {
  return entries.filter((entry) =>
    entry.kind === "phase"
      ? entry.startDay === day || entry.endDay === day
      : entry.endDay === day,
  );
}

/** The range bars of one week row: entries over more than one day, longest first, in two lanes. */
function weekBars(entries: readonly CalendarEntry[], days: readonly DayKey[]) {
  const first = days[0]!;
  const last = days[6]!;
  return entries
    .filter(
      (entry) =>
        entry.endDay > entry.startDay &&
        entry.startDay <= last &&
        entry.endDay >= first,
    )
    .map((entry) => {
      const from = Math.max(0, diffDays(first, entry.startDay));
      const to = Math.min(6, diffDays(first, entry.endDay));
      return {
        entry,
        from,
        to,
        cutLeft: entry.startDay < first,
        cutRight: entry.endDay > last,
      };
    })
    .sort((a, b) => b.to - b.from - (a.to - a.from) || a.from - b.from)
    .slice(0, BAR_LANES);
}

export interface CalendarPhoneProps {
  copy: CalendarCopy;
  locale: string;
  range: CalendarRange;
  onRangeChange: (range: CalendarRange) => void;
  anchorDay: DayKey;
  onAnchorChange: (day: DayKey) => void;
  today: DayKey;
  entries: readonly CalendarEntry[];
  undatedTotal: number;
  undated: readonly CalendarUndated[];
  filterActive: boolean;
  canEdit: boolean;
  onOpenItem: (id: string) => void;
  onSetDue: (id: string, day: DayKey) => void;
  /** "Đổi ngày…" of a 320 px agenda row: the container opens the dates dialog. */
  onEditDates?: ((entry: CalendarEntry) => void) | undefined;
}

/** Below this width the Calendar is `WM-TLCal-320`'s agenda (no grid), above it `Final-M-Calendar`. */
export const NARROW_PHONE_QUERY = "(max-width: 359px)";

/**
 * The Calendar at phone width (`Final-M-Calendar`): the month as dots and range bars, or one week as a strip,
 * the selected day's agenda, and the bar that opens the undated items. Dragging, resizing and quick create stay
 * on the wide layouts; a day opens its agenda, and the day that is already open opens its week.
 *
 * Under 360 px (`WM-TLCal-320`, frames 3 and 4) there is no grid: the period's entries are listed under their day
 * with "Đổi ngày…" on each row, and the undated bar opens a sheet from the bottom instead of the inline panel.
 */
export function CalendarPhone({
  copy,
  locale,
  range,
  onRangeChange,
  anchorDay,
  onAnchorChange,
  today,
  entries,
  undatedTotal,
  undated,
  filterActive,
  canEdit,
  onOpenItem,
  onSetDue,
  onEditDates,
}: CalendarPhoneProps) {
  const [undatedOpen, setUndatedOpen] = useState(false);
  const narrow = useMediaQuery(NARROW_PHONE_QUERY);
  const month = range === "month";
  const t = copy.phone;
  const heading = month
    ? calendarHeading("month", anchorDay, copy, locale)
    : (() => {
        const start = startOfWeek(anchorDay);
        const end = addDays(start, 6);
        return `${parseDayKey(start).day} – ${formatDayMonth(end)}/${parseDayKey(end).year}`;
      })();
  const selected = anchorDay;
  const onDay = entries.filter(
    (entry) => entry.startDay <= selected && entry.endDay >= selected,
  );
  const overdue = entries.filter((entry) => entry.tone === "overdue");
  const pick = (day: DayKey) => {
    if (month && day === anchorDay) onRangeChange("week");
    onAnchorChange(day);
  };
  const cell = (day: DayKey, outside: boolean) => {
    const marks = markersOn(entries, day);
    const tones = month
      ? [...new Set(marks.map((entry) => entry.tone))]
      : marks.map((entry) => entry.tone);
    const isToday = day === today;
    return (
      <div
        key={day}
        role="gridcell"
        className="relative flex h-13.5 justify-center"
      >
        <button
          type="button"
          aria-label={`${weekdayName(day, locale)} ${formatDayMonth(day)}`}
          aria-current={isToday ? "date" : undefined}
          aria-pressed={day === selected}
          onClick={() => pick(day)}
          className="grid size-11 cursor-pointer place-items-start justify-items-center rounded-[12px] border-0 bg-transparent p-0 focus-visible:outline-2 focus-visible:outline-ring"
        >
          <b
            className={cn(
              "mt-0.75 grid size-7.5 place-items-center rounded-full text-[15px] leading-none font-semibold",
              isToday
                ? "bg-primary text-primary-foreground"
                : day === selected
                  ? "bg-state-selected text-state-selected-foreground"
                  : outside
                    ? "font-medium text-calendar-faint"
                    : "text-foreground",
            )}
          >
            {parseDayKey(day).day}
          </b>
        </button>
        <span className="pointer-events-none absolute top-9 flex gap-1">
          {tones.slice(0, 3).map((tone, index) => (
            <span
              key={index}
              className={cn("size-1.25 rounded-full", DOT[tone])}
            />
          ))}
        </span>
      </div>
    );
  };

  return (
    <div
      data-phone-calendar=""
      className="flex h-full min-h-0 flex-col bg-background font-body"
    >
      <div className="flex h-12 shrink-0 items-center pr-3 pl-1.5">
        <button
          type="button"
          aria-label={copy.previous[range]}
          onClick={() => onAnchorChange(stepAnchor(range, anchorDay, -1))}
          className="grid size-11 cursor-pointer place-items-center rounded-[12px] border-0 bg-transparent text-foreground"
        >
          <ChevronLeft className="size-5" aria-hidden="true" />
        </button>
        <h2 className="m-0 px-0.5 text-[16px] tabular-nums leading-[1.45] font-[650] whitespace-nowrap text-foreground">
          {heading}
        </h2>
        <button
          type="button"
          aria-label={copy.next[range]}
          onClick={() => onAnchorChange(stepAnchor(range, anchorDay, 1))}
          className="grid size-11 cursor-pointer place-items-center rounded-[12px] border-0 bg-transparent text-foreground"
        >
          <ChevronRight className="size-5" aria-hidden="true" />
        </button>
        <span className="flex-1" />
        <button
          type="button"
          aria-pressed={narrow ? undefined : month}
          aria-label={narrow ? t.rangeToggle : undefined}
          onClick={() =>
            onRangeChange(narrow ? (month ? "week" : "month") : "month")
          }
          className={cn(
            "inline-flex h-11 cursor-pointer items-center gap-1.5 rounded-[12px] border text-[14px]",
            narrow ? "px-2.5" : "px-3.5",
            month
              ? "border-foreground bg-foreground font-semibold text-background"
              : "border-line-strong bg-popover font-medium text-foreground dark:bg-card",
          )}
        >
          <CalendarDays className="size-3.5" aria-hidden="true" />
          {narrow && !month ? copy.ranges.week : copy.ranges.month}
        </button>
      </div>

      {narrow ? null : month ? (
        <div role="grid" aria-label={heading} className="shrink-0 px-3">
          <div role="row" className="grid h-5.5 grid-cols-7">
            {WEEKDAYS.map((key) => t.weekdaysShort[key]).map((label) => (
              <span
                key={label}
                role="columnheader"
                className="grid place-items-center text-[12px] font-medium text-meta-foreground"
              >
                {label}
              </span>
            ))}
          </div>
          {monthWeeks(anchorDay).map((days) => (
            <div
              key={days[0]}
              role="row"
              data-calendar-week={days[0]}
              className="relative grid grid-cols-7"
            >
              {days.map((day) => cell(day, !sameMonth(day, anchorDay)))}
              {weekBars(entries, days).map(
                ({ entry, from, to, cutLeft, cutRight }, lane) => (
                  <span
                    key={entry.id}
                    data-phone-bar={entry.id}
                    className={cn(
                      "pointer-events-none absolute h-1 rounded-[2px]",
                      BAR[entry.tone],
                      cutLeft && "rounded-l-none",
                      cutRight && "rounded-r-none",
                    )}
                    style={{
                      top: 44 + lane * 5,
                      left: `calc(${from} * 100% / 7 + ${cutLeft ? 0 : 4}px)`,
                      width: `calc(${to - from + 1} * 100% / 7 - ${(cutLeft ? 0 : 4) + (cutRight ? 0 : 4)}px)`,
                    }}
                  />
                ),
              )}
            </div>
          ))}
        </div>
      ) : (
        <>
          <div role="grid" aria-label={heading} className="shrink-0">
            <div role="row" className="flex h-17.25 justify-between px-4">
              {weekDays(anchorDay).map((day, index) => {
                const isToday = day === today;
                const marks = markersOn(entries, day);
                return (
                  <button
                    key={day}
                    type="button"
                    role="gridcell"
                    aria-label={`${weekdayName(day, locale)} ${formatDayMonth(day)}`}
                    aria-pressed={day === selected}
                    onClick={() => pick(day)}
                    className="relative flex h-15.75 w-11 cursor-pointer flex-col items-start rounded-[12px] border-0 bg-transparent p-0"
                  >
                    <span
                      className={cn(
                        "flex h-13 w-11 flex-col items-center justify-start pt-2 rounded-[12px]",
                        isToday
                          ? "bg-primary text-primary-foreground"
                          : day === selected
                            ? "bg-state-selected text-foreground"
                            : "text-foreground",
                      )}
                    >
                      <small
                        className={cn(
                          "text-[12px] leading-[1.2] font-medium",
                          isToday
                            ? "text-primary-foreground"
                            : "text-meta-foreground",
                        )}
                      >
                        {t.weekdaysShort[WEEKDAYS[index]!]}
                      </small>
                      <b className="mt-px text-[17px] leading-[1.2] font-[650]">
                        {parseDayKey(day).day}
                      </b>
                    </span>
                    <span className="pointer-events-none absolute top-14.25 left-1/2 flex -translate-x-1/2 gap-0.75">
                      {marks.slice(0, 3).map((entry) => (
                        <span
                          key={entry.id}
                          className={cn(
                            "size-1.5 rounded-full",
                            DOT[entry.tone],
                          )}
                        />
                      ))}
                    </span>
                  </button>
                );
              })}
            </div>
          </div>
          {overdue[0] ? (
            <button
              type="button"
              onClick={() => onOpenItem(overdue[0]!.id)}
              data-phone-overdue=""
              className="mx-4 mt-0.5 mb-2 flex h-14 shrink-0 cursor-pointer items-center gap-3 rounded-[12px] border border-destructive/30 bg-calendar-event-overdue px-3 text-left text-[14px] leading-[1.45] text-foreground"
            >
              <span className="min-w-0 flex-1">
                {overdue.length > 1
                  ? fill(t.overdueMore, { count: overdue.length })
                  : entryLabel(overdue[0])}
              </span>
              <ChevronRight className="size-5 shrink-0" aria-hidden="true" />
            </button>
          ) : null}
        </>
      )}

      {month && !narrow ? (
        <p className="m-0 flex shrink-0 flex-wrap gap-x-2.5 gap-y-1 px-4 py-1 text-[12px] leading-[1.45] text-meta-foreground">
          {LEGEND.map((tone) => (
            <span key={tone} className="inline-flex items-center gap-1.5">
              <span className={cn("h-1 w-3 rounded-[2px]", BAR[tone])} />
              {copy.legend[tone]}
            </span>
          ))}
          <span className="inline-flex items-center gap-1.5">
            <span className="size-[7px] rotate-45 rounded-[1px] bg-destructive" />
            {copy.kind.milestone}
          </span>
        </p>
      ) : null}

      {narrow ? (
        <div data-phone-agenda="" className="min-h-0 flex-1 overflow-y-auto">
          <CalendarNarrowAgenda
            copy={copy}
            locale={locale}
            range={range}
            anchorDay={anchorDay}
            entries={entries}
            canEdit={canEdit}
            onOpenItem={onOpenItem}
            onEditDates={onEditDates}
          />
        </div>
      ) : null}
      {narrow ? (
        <CalendarUndatedSheet
          copy={copy}
          open={undatedOpen}
          onOpenChange={setUndatedOpen}
          undatedTotal={undatedTotal}
          undated={undated}
          filterActive={filterActive}
          canEdit={canEdit}
          onSetDue={(id) => onSetDue(id, anchorDay)}
        />
      ) : null}
      {narrow ? null : (
        <div className="min-h-0 flex-1 overflow-y-auto px-4">
          <h3 className="m-0 flex h-11 items-center gap-2 text-[15px] leading-[1.45] font-[650] text-foreground">
            {`${weekdayName(selected, locale)} ${formatDayMonth(selected)}`}
            {selected === today ? (
              <span className="inline-flex h-5.5 items-center rounded-[6px] bg-state-selected px-2 text-[12px] font-semibold text-state-selected-foreground">
                {copy.todayMarker}
              </span>
            ) : null}
            <span className="text-[13px] font-medium text-meta-foreground">
              {fill(onDay.length === 1 ? t.items_one : t.items_other, {
                count: onDay.length,
              })}
            </span>
          </h3>
          {onDay.length === 0 ? (
            <p className="m-0 py-4 text-[14px] text-meta-foreground">
              {t.nothingOn}
            </p>
          ) : (
            <ul className="m-0 mb-4 list-none overflow-hidden rounded-[14px] border border-line bg-popover p-0 dark:bg-card">
              {onDay.map((entry) => (
                <li
                  key={entry.id}
                  className="border-b border-line last:border-b-0"
                >
                  <button
                    type="button"
                    data-calendar-event={entry.id}
                    aria-label={entryLabel(entry)}
                    onClick={() => onOpenItem(entry.id)}
                    className="flex w-full cursor-pointer items-start gap-0 border-0 bg-transparent p-0 text-left"
                  >
                    <span className="w-17 shrink-0 px-3 pt-2 text-[13px] font-semibold text-meta-foreground">
                      {entry.dueMinutes !== null &&
                      entry.dueMinutes !== undefined
                        ? `${String(Math.floor(entry.dueMinutes / 60)).padStart(2, "0")}:${String(entry.dueMinutes % 60).padStart(2, "0")}`
                        : copy.agenda.allDay}
                    </span>
                    <span className="min-w-0 flex-1 py-2 pr-3 text-[15px] leading-[1.45] font-medium text-foreground">
                      {entry.itemKey ? (
                        <span className="mr-1.5 font-mono text-[12px] text-meta-foreground">
                          {entry.itemKey}
                        </span>
                      ) : null}
                      {entry.title}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
          {undatedOpen && !narrow ? (
            <div className="pb-4">
              <CalendarAside
                copy={copy}
                undatedTotal={undatedTotal}
                undated={undated}
                filterActive={filterActive}
                canEdit={canEdit}
                onSetDue={(id) => onSetDue(id, anchorDay)}
                onDragStart={() => {}}
                onDragEnd={() => {}}
              />
            </div>
          ) : null}
        </div>
      )}

      <button
        type="button"
        aria-expanded={undatedOpen}
        aria-haspopup={narrow ? "dialog" : undefined}
        onClick={() => setUndatedOpen((open) => !open)}
        className="flex h-12 shrink-0 cursor-pointer items-center gap-2.5 border-0 border-t border-line bg-popover px-4 text-left text-[14px] text-foreground dark:bg-card"
      >
        <CalendarX2 className="size-5 shrink-0" aria-hidden="true" />
        <span className="min-w-0 flex-1 truncate">
          <b className="font-semibold">
            {fill(
              undatedTotal === 1
                ? copy.undated.title_one
                : copy.undated.title_other,
              { count: undatedTotal },
            )}
          </b>
          {undated[0]?.itemKey ? (
            <>
              {copy.separator}
              <span className="font-mono text-[12px] text-meta-foreground">
                {undated[0].itemKey}
              </span>
            </>
          ) : null}
        </span>
        <span className="inline-flex items-center gap-1 font-semibold text-state-selected-foreground">
          {t.undatedOpen}
          <ChevronUp
            className={cn("size-3.5", undatedOpen && "rotate-180")}
            aria-hidden="true"
          />
        </span>
      </button>
    </div>
  );
}
