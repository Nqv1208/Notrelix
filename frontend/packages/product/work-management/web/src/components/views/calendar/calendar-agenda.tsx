import { cn } from "@notrelix/ui-web";
import type { CalendarCopy } from "./calendar.copy";
import {
  CalendarDot,
  CalendarKindTag,
  CalendarMark,
  entryLabel,
} from "./calendar-event";
import {
  AGENDA_SLOT_HEIGHT,
  agendaHours,
  buildAgenda,
  diffDays,
  formatDayMonth,
  formatMinutes,
  nowLineOffset,
  type CalendarEntry,
  type CalendarTone,
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

const ITEM_TONE: Readonly<Record<CalendarTone, string>> = {
  todo: "border-line-strong bg-popover dark:bg-card",
  doing: "border-transparent bg-calendar-event-doing",
  review: "border-transparent bg-calendar-event-review",
  done: "border-transparent bg-calendar-event-done",
  overdue: "border-line-strong bg-popover dark:bg-card",
};

const ROW =
  "flex items-center gap-2 rounded-[7px] border px-2 py-1.5 text-compact leading-[1.45] text-foreground";

function AgendaItem({
  entry,
  detail,
  onOpen,
}: {
  entry: CalendarEntry;
  /** Right-hand text: the due time, the range or the days overdue. */
  detail: ReadonlyArray<{ text: string; bad?: boolean }>;
  onOpen: (id: string) => void;
}) {
  return (
    <button
      type="button"
      data-calendar-event={entry.id}
      aria-label={entryLabel(entry)}
      className={cn(
        ROW,
        ITEM_TONE[entry.tone],
        "w-full cursor-pointer text-left hover:border-meta-foreground focus-visible:outline-2 focus-visible:outline-ring",
      )}
      onClick={() => onOpen(entry.id)}
    >
      <CalendarDot tone={entry.tone} />
      {entry.itemKey ? (
        <span className="flex-none font-mono text-micro leading-[1.45] text-meta-foreground">
          {entry.itemKey}
        </span>
      ) : null}
      <span
        className={cn(
          "min-w-0 flex-1 truncate font-medium",
          entry.tone === "done" && "line-through",
        )}
      >
        {entry.title}
      </span>
      {detail.map((part) => (
        <small
          key={part.text}
          className={cn(
            "text-xs leading-[1.45] whitespace-nowrap text-meta-foreground",
            part.bad && "font-semibold text-danger-text",
          )}
        >
          {part.text}
        </small>
      ))}
      {entry.assignee ? <CalendarMark assignee={entry.assignee} /> : null}
    </button>
  );
}

function Section({
  title,
  overdue = false,
  children,
}: {
  title: string;
  overdue?: boolean;
  children: React.ReactNode;
}) {
  return (
    <section
      role="row"
      className={cn(
        "flex flex-col gap-1.5 border-b border-border px-3 py-2.5",
        overdue && "bg-calendar-event-overdue",
      )}
    >
      <h3
        className={cn(
          "text-xs leading-[1.45] font-semibold",
          overdue ? "text-danger-text" : "text-meta-foreground",
        )}
      >
        {title}
      </h3>
      {children}
    </section>
  );
}

export interface CalendarAgendaProps {
  copy: CalendarCopy;
  day: DayKey;
  today: DayKey;
  /** Minutes of the day now, for the "now" line (only shown on today). */
  nowMinutes: number;
  entries: readonly CalendarEntry[];
  onOpenItem: (id: string) => void;
}

/**
 * The day view: overdue entries (today only), entries that cover the day
 * without a time, then the hour slots with the entries due in each and the
 * "now" line on today.
 */
export function CalendarAgenda({
  copy,
  day,
  today,
  nowMinutes,
  entries,
  onOpenItem,
}: CalendarAgendaProps) {
  const agenda = buildAgenda(entries, day, today);
  const nowOffset = day === today ? nowLineOffset(nowMinutes) : null;

  const rangeText = (entry: CalendarEntry) => {
    const from = formatDayMonth(entry.startDay);
    const to = formatDayMonth(entry.endDay);
    return fill(entry.endDay >= day ? copy.agenda.span : copy.agenda.spanPast, {
      from,
      to,
    });
  };

  return (
    <div role="rowgroup" className="flex min-h-0 flex-1 flex-col overflow-auto">
      {agenda.overdue.length > 0 ? (
        <Section title={copy.agenda.overdue} overdue>
          {agenda.overdue.map((entry) => {
            const days = Math.max(1, diffDays(entry.endDay, today));
            return (
              <AgendaItem
                key={entry.id}
                entry={entry}
                onOpen={onOpenItem}
                detail={[
                  {
                    text:
                      entry.dueMinutes != null
                        ? fill(copy.agenda.due, {
                            date: formatDayMonth(entry.endDay),
                            time: formatMinutes(entry.dueMinutes),
                          })
                        : fill(copy.agenda.dueDate, {
                            date: formatDayMonth(entry.endDay),
                          }),
                  },
                  {
                    text: fill(
                      days === 1
                        ? copy.agenda.overdueBy_one
                        : copy.agenda.overdueBy_other,
                      { days },
                    ),
                    bad: true,
                  },
                ]}
              />
            );
          })}
        </Section>
      ) : null}
      {agenda.allDay.length > 0 ? (
        <Section title={copy.agenda.allDay}>
          {agenda.allDay.map((entry) => {
            if (entry.kind !== "item") {
              return (
                <div
                  key={entry.id}
                  data-calendar-marker={entry.kind}
                  className={cn(ROW, ITEM_TONE[entry.tone])}
                >
                  <CalendarKindTag>
                    {entry.kind === "milestone"
                      ? copy.kind.milestone
                      : copy.kind.phase}
                  </CalendarKindTag>
                  <span className="min-w-0 flex-1 truncate font-medium">
                    {entry.title}
                  </span>
                  <small className="text-xs leading-[1.45] whitespace-nowrap text-meta-foreground">
                    {rangeText(entry)}
                  </small>
                </div>
              );
            }
            return (
              <AgendaItem
                key={entry.id}
                entry={entry}
                onOpen={onOpenItem}
                detail={[{ text: rangeText(entry) }]}
              />
            );
          })}
        </Section>
      ) : null}
      <div className="relative">
        {agendaHours().map((hour) => (
          <div
            key={hour}
            role="row"
            className="grid grid-cols-[64px_minmax(0,1fr)] border-b border-border"
            style={{ minHeight: AGENDA_SLOT_HEIGHT }}
          >
            <time
              role="rowheader"
              className="px-2.5 pt-1 text-micro leading-[1.45] text-meta-foreground tabular-nums"
            >
              {formatMinutes(hour * 60)}
            </time>
            <div role="gridcell" className="flex flex-col gap-1.5 py-1.5 pr-3">
              {(agenda.byHour.get(hour) ?? []).map((entry) => (
                <AgendaItem
                  key={entry.id}
                  entry={entry}
                  onOpen={onOpenItem}
                  detail={[
                    {
                      text: fill(copy.agenda.dueTime, {
                        time: formatMinutes(entry.dueMinutes ?? 0),
                      }),
                    },
                  ]}
                />
              ))}
            </div>
          </div>
        ))}
        {nowOffset !== null ? (
          <span
            data-calendar-now=""
            aria-label={fill(copy.agenda.now, {
              time: formatMinutes(nowMinutes),
            })}
            className="absolute right-0 left-16 z-10 h-0.5 bg-primary"
            style={{ top: nowOffset }}
          >
            <b className="absolute -top-2 -left-14.5 bg-popover px-[3px] text-hint leading-4 font-semibold text-brand-ink dark:bg-card">
              {formatMinutes(nowMinutes)}
            </b>
          </span>
        ) : null}
      </div>
    </div>
  );
}
