import { useState } from "react";
import { Clock } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { CalendarDot, entryLabel } from "./calendar-event";
import { weekdayName } from "./calendar-format";
import {
  AGENDA_FIRST_HOUR,
  AGENDA_LAST_HOUR,
  agendaHours,
  buildAgenda,
  formatDayMonth,
  formatMinutes,
  type CalendarEntry,
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

/** One hour slot is 34 px: a 33 px row and its 1 px line. */
const SLOT = 34;

const TONE: Readonly<Record<CalendarEntry["tone"], string>> = {
  todo: "bg-popover ring-1 ring-line-strong ring-inset dark:bg-card",
  doing: "bg-calendar-event-doing",
  review: "bg-calendar-event-review",
  done: "bg-calendar-event-done",
  overdue: "bg-calendar-event-overdue",
};

function Mark({ entry }: { entry: CalendarEntry }) {
  return entry.assignee ? (
    <span
      aria-hidden="true"
      className={cn(
        "grid size-7 flex-none place-items-center rounded-full text-[10.5px] font-bold",
        entry.assignee.markClass,
      )}
    >
      {entry.assignee.initials}
    </span>
  ) : null;
}

function EntryCard({
  copy,
  entry,
  onOpenItem,
  style,
}: {
  copy: CalendarCopy;
  entry: CalendarEntry;
  onOpenItem: (id: string) => void;
  style?: React.CSSProperties;
}) {
  return (
    <button
      type="button"
      data-calendar-event={entry.id}
      aria-label={entryLabel(entry)}
      onClick={() => onOpenItem(entry.id)}
      style={style}
      className={cn(
        "box-border flex min-h-11 w-full cursor-pointer items-center gap-2 rounded-[10px] leading-[1.45] border-0 py-1 pr-2 pl-2.5 text-left text-foreground focus-visible:outline-2 focus-visible:outline-ring",
        TONE[entry.tone],
        style && "absolute left-13 w-auto right-0",
      )}
    >
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="flex items-center gap-1.5 text-[12px] whitespace-nowrap text-muted-foreground">
          <CalendarDot tone={entry.tone} className="size-2" />
          {entry.itemKey ? (
            <span className="font-mono text-[12px]">{entry.itemKey}</span>
          ) : null}
          {entry.dueMinutes != null ? (
            <b className="font-semibold text-warning-text">
              {fill(copy.agenda.dueTime, {
                time: formatMinutes(entry.dueMinutes),
              })}
            </b>
          ) : null}
        </span>
        <span className="truncate text-[14px] font-semibold">
          {entry.title}
        </span>
      </span>
      <Mark entry={entry} />
    </button>
  );
}

export interface CalendarPhoneDayProps {
  copy: CalendarCopy;
  locale: string;
  day: DayKey;
  today: DayKey;
  nowMinutes: number;
  /** "giờ Hà Nội": the zone, lower case, after the item count. */
  zoneName: string;
  entries: readonly CalendarEntry[];
  onOpenItem: (id: string) => void;
}

/**
 * The phone Calendar's day (`Final-M-Calendar` `ngay`): the day's name with its count and the "Theo giờ" switch, the
 * all-day phases and milestones, and the hours 08:00 to 18:00 with the items placed at their due time and the now
 * line on today. "Theo giờ" off lists the same items without the hours.
 */
export function CalendarPhoneDay({
  copy,
  locale,
  day,
  today,
  nowMinutes,
  zoneName,
  entries,
  onOpenItem,
}: CalendarPhoneDayProps) {
  const t = copy.phone;
  const [byHour, setByHour] = useState(true);
  const agenda = buildAgenda(entries, day, today);
  const timed = [...agenda.byHour.values()].flat();
  const total = agenda.allDay.length + timed.length + agenda.overdue.length;
  const first = AGENDA_FIRST_HOUR * 60;
  const last = AGENDA_LAST_HOUR * 60;
  const nowTop =
    day === today && nowMinutes >= first && nowMinutes < last + 60
      ? ((nowMinutes - first) / 60) * SLOT
      : null;
  const name = `${weekdayName(day, locale)} ${formatDayMonth(day)}`;
  return (
    <section
      data-phone-day=""
      aria-labelledby="calendar-day-heading"
      aria-describedby="calendar-day-hint"
      className="flex min-h-0 flex-1 flex-col overflow-y-auto px-4"
    >
      <h3
        id="calendar-day-heading"
        className="m-0 flex min-h-11 flex-none items-center gap-2 text-[15px] leading-[1.45] font-[650] text-foreground"
      >
        {day === today ? (
          <span className="inline-flex h-5.5 items-center rounded-[6px] bg-state-selected px-2 text-[12px] font-semibold text-state-selected-foreground">
            {copy.todayMarker}
          </span>
        ) : null}
        <span className="text-[13px] font-medium text-meta-foreground">
          {fill(t.dayItems, {
            count: fill(total === 1 ? t.items_one : t.items_other, {
              count: total,
            }),
            zone: zoneName,
          })}
        </span>
        <button
          type="button"
          aria-pressed={byHour}
          aria-label={fill(t.byHourLabel, { day: name })}
          onClick={() => setByHour((on) => !on)}
          className={cn(
            "-mr-1 ml-auto inline-flex h-11 cursor-pointer items-center gap-1.5 rounded-[12px] border px-3 text-[14px]",
            byHour
              ? "border-foreground bg-foreground font-semibold text-background"
              : "border-line-strong bg-popover font-medium text-foreground dark:bg-card",
          )}
        >
          <Clock className="size-3.5" aria-hidden="true" />
          {t.byHour}
        </button>
      </h3>
      <p id="calendar-day-hint" className="sr-only">
        {t.dayHint}
      </p>
      {agenda.allDay.length > 0 ? (
        <div className="flex flex-none items-stretch gap-2 border-b border-line pt-0.5 pb-2.5">
          <span className="w-11 flex-none pt-2.25 text-[11.5px] font-medium whitespace-nowrap text-meta-foreground">
            {copy.agenda.allDay}
          </span>
          <div className="flex min-w-0 flex-1 flex-col gap-1.5">
            {agenda.allDay.map((entry) =>
              entry.kind === "item" ? (
                <EntryCard
                  key={entry.id}
                  copy={copy}
                  entry={entry}
                  onOpenItem={onOpenItem}
                />
              ) : (
                <div
                  key={entry.id}
                  data-calendar-marker={entry.kind}
                  className={cn(
                    "flex flex-col gap-0.5 rounded-[10px] px-2.5 py-1.75",
                    TONE[entry.tone],
                  )}
                >
                  <b className="flex items-center gap-1.5 overflow-hidden text-[14px] font-semibold whitespace-nowrap">
                    <span className="flex-none rounded-[5px] border border-line-strong bg-popover px-1.25 text-[11px] leading-[18px] font-semibold text-muted-foreground dark:bg-card">
                      {entry.kind === "milestone"
                        ? copy.kind.milestone
                        : copy.kind.phase}
                    </span>
                    <span className="truncate">{entry.title}</span>
                  </b>
                  <small className="text-[12.5px] text-muted-foreground">
                    {fill(
                      entry.endDay >= day
                        ? copy.agenda.span
                        : copy.agenda.spanPast,
                      {
                        from: formatDayMonth(entry.startDay),
                        to: formatDayMonth(entry.endDay),
                      },
                    )}
                  </small>
                </div>
              ),
            )}
          </div>
        </div>
      ) : null}
      {byHour ? (
        <div
          data-phone-hours=""
          className="relative mt-3 flex-none"
          style={{ height: (AGENDA_LAST_HOUR - AGENDA_FIRST_HOUR) * SLOT }}
        >
          {agendaHours()
            .slice(0, -1)
            .map((hour) => (
              <div
                key={hour}
                aria-hidden="true"
                className="flex h-8.5 border-t border-line"
              >
                <span className="-mt-2.25 w-11 flex-none bg-background text-[12px] leading-4 font-medium text-meta-foreground tabular-nums">
                  {formatMinutes(hour * 60)}
                </span>
              </div>
            ))}
          <span aria-hidden="true" className="-mt-2 block border-t border-line">
            <b className="relative -top-px -mt-2.25 inline-block bg-background pr-1 text-[12px] leading-4 font-medium text-meta-foreground">
              {formatMinutes(last)}
            </b>
          </span>
          {timed.map((entry) => (
            <EntryCard
              key={entry.id}
              copy={copy}
              entry={entry}
              onOpenItem={onOpenItem}
              style={{
                top:
                  ((Math.min(last, Math.max(first, entry.dueMinutes ?? first)) -
                    first) /
                    60) *
                  SLOT,
              }}
            />
          ))}
          {nowTop !== null ? (
            <span
              data-calendar-now=""
              className="pointer-events-none absolute right-0 left-11.5 z-10 h-0.5 bg-primary before:absolute before:-top-1 before:-left-1.25 before:size-2.5 before:rounded-full before:bg-primary before:content-['']"
              style={{ top: nowTop }}
            >
              <span className="sr-only">
                {fill(copy.agenda.now, { time: "" }).trim()}{" "}
              </span>
              <b className="absolute -top-2.5 right-0 rounded-[6px] bg-primary px-1.5 text-[11px] leading-5 font-[650] text-primary-foreground tabular-nums">
                {formatMinutes(nowMinutes)}
              </b>
            </span>
          ) : null}
        </div>
      ) : (
        <ul className="m-0 mt-3 flex list-none flex-col gap-1.5 p-0">
          {timed.length === 0 && agenda.allDay.length === 0 ? (
            <li className="py-4 text-[14px] text-meta-foreground">
              {t.nothingOn}
            </li>
          ) : null}
          {timed.map((entry) => (
            <li key={entry.id}>
              <EntryCard copy={copy} entry={entry} onOpenItem={onOpenItem} />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
