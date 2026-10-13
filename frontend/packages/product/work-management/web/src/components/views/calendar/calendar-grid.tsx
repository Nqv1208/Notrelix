import {
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type DragEvent,
  type FormEvent,
  type KeyboardEvent,
  type MouseEvent,
} from "react";
import { cn } from "@notrelix/ui-web";
import type { CalendarCopy } from "./calendar.copy";
import {
  CalendarEvent,
  CalendarGhost,
  type CalendarEventProps,
} from "./calendar-event";
import {
  addDays,
  diffDays,
  formatDayMonth,
  isWeekend,
  layoutWeek,
  parseDayKey,
  sameMonth,
  weekRowCapacity,
  type CalendarEntry,
  type DayKey,
} from "./calendar-model";

const WEEKDAY_KEYS = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"] as const;

/** What a drag is carrying and over which day it is. */
export interface CalendarDragState {
  /** `entry` is a chip of the grid, `undated` an item of the side list. */
  readonly source: "entry" | "undated";
  readonly id: string;
  readonly text: string;
  readonly assignee?: CalendarEntry["assignee"];
  /** Where the entry stands now; absent for an undated item. */
  readonly fromDay?: DayKey;
  readonly overDay: DayKey | null;
}

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** The day under a pointer in a week row, by column. */
function dayAt(
  element: HTMLElement,
  days: readonly DayKey[],
  clientX: number,
  fallback: EventTarget | null,
): DayKey | null {
  const rect = element.getBoundingClientRect();
  if (rect.width > 0) {
    const column = Math.floor(((clientX - rect.left) / rect.width) * 7);
    return days[Math.min(6, Math.max(0, column))] ?? null;
  }
  const cell =
    fallback instanceof Element
      ? fallback.closest<HTMLElement>("[data-day]")
      : null;
  return cell?.dataset.day ?? null;
}

/**
 * How many event rows fit a week of the month grid, measured from the grid's
 * height. `null` until measured (and in tests without layout): nothing is
 * collapsed then, except by the caller's default.
 */
function useWeekCapacity(weekCount: number, enabled: boolean) {
  const ref = useRef<HTMLDivElement>(null);
  const [capacity, setCapacity] = useState<number | null>(null);
  useLayoutEffect(() => {
    const element = ref.current;
    if (!enabled || !element || typeof ResizeObserver === "undefined") {
      return undefined;
    }
    const measure = () => {
      const height = element.clientHeight;
      if (height > 0) setCapacity(weekRowCapacity(height / weekCount));
    };
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(element);
    return () => observer.disconnect();
  }, [enabled, weekCount]);
  return { ref, capacity: enabled ? capacity : null };
}

export interface CalendarGridProps {
  copy: CalendarCopy;
  range: "month" | "week";
  /** The weeks to draw, Monday first. */
  weeks: readonly (readonly DayKey[])[];
  /** The month the days belong to (days outside it are muted). */
  monthOf: DayKey;
  anchorDay: DayKey;
  today: DayKey;
  entries: readonly CalendarEntry[];
  canEdit: boolean;
  drag: CalendarDragState | null;
  /** The day whose quick-create popover is open. */
  quickCreateDay: DayKey | null;
  onOpenItem: CalendarEventProps["onOpen"];
  onShift: CalendarEventProps["onShift"];
  onDragStartEntry: CalendarEventProps["onDragStart"];
  onDragEnd: () => void;
  onDragOverDay: (day: DayKey | null) => void;
  onDropDay: (day: DayKey) => void;
  onRequestQuickCreate: (day: DayKey) => void;
  onSubmitQuickCreate: (title: string) => void;
  onCancelQuickCreate: () => void;
  onOpenDay: (day: DayKey) => void;
  onMoveFocus: (day: DayKey) => void;
}

/**
 * The month or week grid: the weekday header and one row per week. Each week
 * has a background of day columns and a foreground grid with the day numbers
 * in row 1 and the entries under them. A multi-day entry spans columns; an
 * entry that does not fit the row collapses into "+N".
 */
export function CalendarGrid(props: CalendarGridProps) {
  const { copy, range, weeks, canEdit } = props;
  const month = range === "month";
  const { ref, capacity } = useWeekCapacity(weeks.length, month);
  // Without a measured height a month week shows three rows, the 1440 x 900
  // layout; the week view grows with its content.
  const maxRows = month ? (capacity ?? 3) : null;

  return (
    <>
      <div
        role="row"
        className="grid grid-cols-7 border-b border-line-strong bg-calendar-weekend"
      >
        {WEEKDAY_KEYS.map((key) => (
          <span
            key={key}
            role="columnheader"
            className="flex h-8 items-center px-2.5 text-xs leading-[1.45] font-semibold text-meta-foreground"
          >
            {copy.weekdays[key]}
          </span>
        ))}
      </div>
      <div
        ref={ref}
        className={cn(
          "flex min-h-0 flex-1 flex-col",
          !month && "overflow-y-auto",
        )}
      >
        {weeks.map((days) => (
          <CalendarWeek
            key={days[0]}
            {...props}
            days={days}
            wrap={!month}
            maxRows={maxRows}
            canEdit={canEdit}
          />
        ))}
      </div>
    </>
  );
}

function CalendarWeek({
  copy,
  days,
  monthOf,
  anchorDay,
  today,
  entries,
  wrap,
  maxRows,
  canEdit,
  drag,
  quickCreateDay,
  onOpenItem,
  onShift,
  onDragStartEntry,
  onDragEnd,
  onDragOverDay,
  onDropDay,
  onRequestQuickCreate,
  onSubmitQuickCreate,
  onCancelQuickCreate,
  onOpenDay,
  onMoveFocus,
  range,
}: CalendarGridProps & {
  days: readonly DayKey[];
  wrap: boolean;
  maxRows: number | null;
}) {
  const weekRef = useRef<HTMLDivElement>(null);
  const layout = layoutWeek(days, entries, maxRows);
  const first = days[0] as DayKey;
  const columnOf = (day: DayKey) => diffDays(first, day) + 1;

  const usedRows = (col: number) =>
    new Set(
      layout.placements
        .filter(
          (placement) =>
            col >= placement.col && col < placement.col + placement.span,
        )
        .map((placement) => placement.row),
    );
  const freeRow = (col: number) => {
    const used = usedRows(col);
    let row = 0;
    while (used.has(row)) row += 1;
    return row;
  };

  const overDay =
    drag?.overDay && days.includes(drag.overDay) ? drag.overDay : null;
  const showGhost =
    overDay !== null &&
    (drag?.source === "undated" || overDay !== drag?.fromDay);

  const canDropHere = canEdit && drag !== null;
  const handleDragOver = (event: DragEvent<HTMLElement>) => {
    if (!canDropHere) return;
    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    const day = weekRef.current
      ? dayAt(weekRef.current, days, event.clientX, event.target)
      : null;
    if (day) onDragOverDay(day);
  };
  const handleDrop = (event: DragEvent<HTMLElement>) => {
    if (!canDropHere) return;
    event.preventDefault();
    const day = weekRef.current
      ? dayAt(weekRef.current, days, event.clientX, event.target)
      : null;
    if (day) onDropDay(day);
  };
  const handleClick = (event: MouseEvent<HTMLElement>) => {
    if (!canEdit) return;
    if ((event.target as Element).closest("button, input, form")) return;
    const day = weekRef.current
      ? dayAt(weekRef.current, days, event.clientX, event.target)
      : null;
    if (day) onRequestQuickCreate(day);
  };

  const handleDayKey = (event: KeyboardEvent<HTMLElement>, day: DayKey) => {
    const step =
      event.key === "ArrowLeft"
        ? -1
        : event.key === "ArrowRight"
          ? 1
          : event.key === "ArrowUp"
            ? -7
            : event.key === "ArrowDown"
              ? 7
              : 0;
    if (step !== 0) {
      event.preventDefault();
      onMoveFocus(addDays(day, step));
      return;
    }
    if ((event.key === "Enter" || event.key === " ") && canEdit) {
      event.preventDefault();
      onRequestQuickCreate(day);
    }
  };

  return (
    <div
      ref={weekRef}
      role="row"
      data-calendar-week={first}
      className="relative min-h-0 flex-1 border-b border-border last:border-b-0"
      onDragOver={handleDragOver}
      onDrop={handleDrop}
      onClick={handleClick}
    >
      <div aria-hidden="true" className="absolute inset-0 grid grid-cols-7">
        {days.map((day) => {
          const outside = range === "month" && !sameMonth(day, monthOf);
          return (
            <div
              key={day}
              data-calendar-day-bg={day}
              className={cn(
                "border-r border-border last:border-r-0",
                isWeekend(day) && "bg-calendar-weekend",
                outside && "bg-calendar-outside",
                day === today && "bg-calendar-today",
                overDay === day &&
                  "bg-state-selected shadow-[inset_0_0_0_2px_var(--primary)]",
              )}
            />
          );
        })}
      </div>
      <div className="relative grid grid-flow-row-dense grid-cols-7 gap-y-[3px] pt-1 pb-1.5">
        {days.map((day) => {
          const outside = range === "month" && !sameMonth(day, monthOf);
          const isToday = day === today;
          return (
            <div
              key={day}
              role="gridcell"
              data-day={day}
              tabIndex={day === anchorDay ? 0 : -1}
              aria-current={isToday ? "date" : undefined}
              className={cn(
                "flex h-6.5 items-center gap-1.5 px-2 text-crumb leading-[1.45] font-semibold text-muted-foreground focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring",
                outside && "font-medium text-calendar-faint",
              )}
              style={{ gridColumn: columnOf(day), gridRow: 1 }}
              onKeyDown={(event) => handleDayKey(event, day)}
            >
              <b
                className={cn(
                  "font-bold",
                  isToday &&
                    "grid h-6 min-w-6 place-items-center rounded-full bg-primary px-1 text-primary-foreground",
                )}
              >
                {parseDayKey(day).day}
              </b>
              {isToday ? (
                <small className="text-micro leading-[1.45] font-medium text-brand-ink">
                  {copy.todayMarker}
                </small>
              ) : null}
            </div>
          );
        })}

        {layout.placements.map((placement) => (
          <CalendarEvent
            key={`${placement.entry.id}:${placement.col}`}
            entry={placement.entry}
            copy={copy}
            col={placement.col}
            span={placement.span}
            row={placement.row}
            wrap={wrap}
            lifting={drag?.source === "entry" && drag.id === placement.entry.id}
            canDrag={canEdit}
            onOpen={onOpenItem}
            onDragStart={onDragStartEntry}
            onDragEnd={onDragEnd}
            onShift={onShift}
          />
        ))}

        {layout.more.map((more) => (
          <button
            key={`more-${more.col}`}
            type="button"
            className="mx-1 flex h-6 min-w-0 cursor-pointer items-center rounded-[6px] border-0 bg-transparent px-1.5 text-xs leading-[1.45] font-semibold text-brand-ink hover:shadow-[inset_0_0_0_1px_var(--line-strong)] focus-visible:outline-2 focus-visible:outline-ring"
            style={{
              gridColumn: `${more.col} / span 1`,
              gridRow: (maxRows ?? 3) + 1,
            }}
            aria-label={fill(copy.more.open, {
              count: more.count,
              date: formatDayMonth(more.day),
            })}
            onClick={(event) => {
              event.stopPropagation();
              onOpenDay(more.day);
            }}
          >
            {fill(
              more.count === 1 ? copy.more.label_one : copy.more.label_other,
              {
                count: more.count,
              },
            )}
          </button>
        ))}

        {showGhost && overDay && drag ? (
          <CalendarGhost
            text={drag.text}
            col={columnOf(overDay)}
            row={freeRow(columnOf(overDay))}
            assignee={drag.assignee}
          />
        ) : null}

        {quickCreateDay && days.includes(quickCreateDay) ? (
          <QuickCreate
            copy={copy}
            day={quickCreateDay}
            placement={{
              col: columnOf(quickCreateDay),
              row: freeRow(columnOf(quickCreateDay)),
            }}
            onSubmit={onSubmitQuickCreate}
            onCancel={onCancelQuickCreate}
          />
        ) : null}
      </div>
      {showGhost && overDay && drag ? (
        <div
          role="status"
          data-calendar-tip=""
          className="absolute z-10 rounded-[7px] bg-calendar-tip px-[9px] py-[5px] text-xs leading-[1.45] font-semibold whitespace-nowrap text-calendar-tip-foreground shadow-popover"
          style={{
            left: `calc(100% / 7 * ${columnOf(overDay) - 1} + 8px)`,
            top: -14,
          }}
        >
          {drag.fromDay
            ? fill(copy.dragTip.reschedule, {
                from: formatDayMonth(drag.fromDay),
                to: formatDayMonth(overDay),
              })
            : fill(copy.dragTip.setDue, { to: formatDayMonth(overDay) })}
        </div>
      ) : null}
    </div>
  );
}

/**
 * The quick-create popover: a title field and a hint. In the grid it takes a
 * cell of the week (`placement`); in the day view it stands alone.
 */
export function QuickCreate({
  copy,
  day,
  placement,
  onSubmit,
  onCancel,
}: {
  copy: CalendarCopy;
  day: DayKey;
  placement?: { col: number; row: number };
  onSubmit: (title: string) => void;
  onCancel: () => void;
}) {
  const [title, setTitle] = useState("");
  const input = useRef<HTMLInputElement>(null);
  useEffect(() => {
    input.current?.focus();
  }, [day]);
  const date = formatDayMonth(day);
  const submit = (event: FormEvent) => {
    event.preventDefault();
    const value = title.trim();
    if (value) onSubmit(value);
  };
  return (
    <form
      data-calendar-quick-create=""
      className="z-10 mx-1 flex flex-col gap-[3px] rounded-[7px] border-[1.5px] border-primary bg-popover px-1.5 py-[5px] shadow-popover dark:bg-card"
      style={
        placement
          ? {
              gridColumn: `${placement.col} / span 1`,
              gridRow: placement.row + 2,
            }
          : undefined
      }
      onSubmit={submit}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.stopPropagation();
          onCancel();
        }
      }}
    >
      <input
        ref={input}
        type="text"
        value={title}
        placeholder={copy.quickCreate.placeholder}
        aria-label={fill(copy.quickCreate.label, { date })}
        className="w-full min-w-0 border-0 bg-transparent px-0.5 py-px text-crumb leading-[1.45] text-foreground outline-none placeholder:text-meta-foreground"
        onChange={(event) => setTitle(event.target.value)}
      />
      <small className="text-hint leading-[1.45] text-meta-foreground">
        {fill(copy.quickCreate.hint, { date })}
      </small>
    </form>
  );
}
