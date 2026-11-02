import { useEffect, useRef, useState } from "react";
import { ChevronLeft, ChevronRight, Clock } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { zoneOffset } from "../../shared/board-clock";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";

/** A calendar day in the viewer's own zone; `month` counts from 1. */
interface Day {
  readonly year: number;
  readonly month: number;
  readonly day: number;
}

const DEFAULT_TIME = "17:00";
const TIMES = Array.from(
  { length: 48 },
  (_, index) =>
    `${String(Math.floor(index / 2)).padStart(2, "0")}:${index % 2 ? "30" : "00"}`,
);

const toDay = (date: Date): Day => ({
  year: date.getFullYear(),
  month: date.getMonth() + 1,
  day: date.getDate(),
});
const addDays = (day: Day, count: number): Day =>
  toDay(new Date(day.year, day.month - 1, day.day + count));
const sameDay = (a: Day | null, b: Day | null) =>
  Boolean(
    a && b && a.year === b.year && a.month === b.month && a.day === b.day,
  );
/** Monday is 0, as the picker draws its weeks. */
const weekdayOf = (day: Day) =>
  (new Date(day.year, day.month - 1, day.day).getDay() + 6) % 7;
const keyOf = (day: Day) =>
  `${day.year}-${String(day.month).padStart(2, "0")}-${String(day.day).padStart(2, "0")}`;
const shortDate = (day: Day) =>
  `${String(day.day).padStart(2, "0")}/${String(day.month).padStart(2, "0")}`;
const timeOf = (date: Date) =>
  `${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;

const NAV =
  "grid size-11 cursor-pointer place-items-center rounded-[10px] border-0 bg-transparent p-0 text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring";
const PRESET =
  "group/preset flex h-11 min-w-0 cursor-pointer items-center justify-between gap-1.5 rounded-[10px] border border-line-strong bg-transparent px-3 text-left text-sm font-medium text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring aria-pressed:border-primary aria-pressed:bg-state-selected aria-pressed:font-semibold aria-pressed:text-state-selected-foreground";

/**
 * The due-date picker of the tablet cell and the phone sheet (`Final-T-Table`, `sua-o`): four presets, a month
 * of 44 px days and the time of day. A day writes at once, with the time chosen below; "Không có hạn" clears
 * the date. Changing the time of an item that has a due date writes it again with the same day.
 */
export function TableDuePicker({
  copy,
  dueAt,
  heading,
  onPick,
}: {
  copy: MainTableCopy;
  /** The due instant the item has; null = none. */
  dueAt: string | null;
  /** The title line ("Hạn chót" and the item key); absent when a sheet draws its own. */
  heading?: { readonly key: string };
  /** "YYYY-MM-DD" and "HH:MM" of the choice, or `null` to clear the date. */
  onPick: (day: string | null, time: string) => void;
}) {
  const t = copy.duePicker;
  const months = Object.values(t.months);
  const [now] = useState(() => new Date());
  const today = toDay(now);
  const due = dueAt ? new Date(dueAt) : null;
  const selected = due && !Number.isNaN(due.getTime()) ? toDay(due) : null;
  const [time, setTime] = useState(
    selected && due ? timeOf(due) : DEFAULT_TIME,
  );
  const [shown, setShown] = useState<Day>(selected ?? today);
  const firstDay = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    // The list opens on the chosen day, or on today, so a keyboard starts inside the month.
    firstDay.current?.focus({ preventScroll: true });
  }, []);

  const monday = addDays(today, 7 - weekdayOf(today));
  const presets: readonly { label: string; day: Day | null }[] = [
    { label: t.today, day: today },
    { label: t.tomorrow, day: addDays(today, 1) },
    { label: t.nextMonday, day: monday },
    { label: t.none, day: null },
  ];
  const lead = weekdayOf({ ...shown, day: 1 });
  const length = new Date(shown.year, shown.month, 0).getDate();
  const cells = Math.ceil((lead + length) / 7) * 7;
  const days = Array.from({ length: cells }, (_, index) =>
    addDays({ ...shown, day: 1 }, index - lead),
  );
  const step = (by: number) => {
    const next = new Date(shown.year, shown.month - 1 + by, 1);
    setShown(toDay(next));
  };
  const options = TIMES.includes(time) ? TIMES : [...TIMES, time].sort();

  return (
    <div className="flex flex-col" data-slot="table-due-picker">
      {heading ? (
        <p className="m-0 flex items-center gap-2 px-1.5 pt-1 pb-2 text-[14.5px] font-semibold text-foreground">
          {t.heading}
          <span className="font-mono text-crumb font-medium text-meta-foreground">
            {heading.key}
          </span>
        </p>
      ) : null}
      <div className="grid grid-cols-2 gap-1.5">
        {presets.map((preset) => (
          <button
            key={preset.label}
            type="button"
            aria-pressed={
              preset.day ? sameDay(preset.day, selected) : selected === null
            }
            className={PRESET}
            onClick={() => onPick(preset.day ? keyOf(preset.day) : null, time)}
          >
            <span className="truncate">{preset.label}</span>
            {preset.day ? (
              <small className="text-crumb font-normal text-meta-foreground tabular-nums group-aria-pressed/preset:text-state-selected-foreground">
                {shortDate(preset.day)}
              </small>
            ) : null}
          </button>
        ))}
      </div>
      <div className="mt-1.5 flex items-center justify-between">
        <button
          type="button"
          aria-label={t.prevMonth}
          className={NAV}
          onClick={() => step(-1)}
        >
          <ChevronLeft className="size-4" aria-hidden="true" />
        </button>
        <b
          aria-live="polite"
          className="text-[15px] font-semibold text-foreground"
        >
          {fillTableCopy(t.month, {
            month: months[shown.month - 1] ?? "",
            year: shown.year,
          })}
        </b>
        <button
          type="button"
          aria-label={t.nextMonth}
          className={NAV}
          onClick={() => step(1)}
        >
          <ChevronRight className="size-4" aria-hidden="true" />
        </button>
      </div>
      <div
        aria-hidden="true"
        className="grid h-6.5 grid-cols-7 items-center text-center text-crumb font-semibold text-meta-foreground"
      >
        {Object.values(t.weekdays).map((name) => (
          <span key={name}>{name}</span>
        ))}
      </div>
      <div
        role="group"
        aria-label={fillTableCopy(t.month, {
          month: months[shown.month - 1] ?? "",
          year: shown.year,
        })}
        className="grid grid-cols-7"
      >
        {days.map((day) => {
          const inMonth = day.month === shown.month;
          const isSelected = sameDay(day, selected);
          const isToday = sameDay(day, today);
          return (
            <button
              key={keyOf(day)}
              ref={isSelected || (!selected && isToday) ? firstDay : undefined}
              type="button"
              aria-label={`${day.day} ${months[day.month - 1] ?? ""} ${day.year}`}
              aria-pressed={isSelected}
              aria-current={isToday ? "date" : undefined}
              onClick={() => onPick(keyOf(day), time)}
              className={cn(
                "size-11 cursor-pointer justify-self-center rounded-full border-0 bg-transparent p-0 text-[14.5px] text-foreground tabular-nums hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
                !inMonth && "text-meta-foreground",
                isToday &&
                  "font-bold shadow-[inset_0_0_0_1.5px_var(--color-primary)]",
                isSelected &&
                  "bg-primary font-bold text-primary-foreground shadow-none hover:bg-primary",
              )}
            >
              {day.day}
            </button>
          );
        })}
      </div>
      <div className="mt-1.5 flex items-center gap-2.5 border-t border-border pt-1.5 pl-2 text-[14.5px] text-muted-foreground">
        <Clock className="size-4 text-meta-foreground" aria-hidden="true" />
        {t.time}
        <select
          aria-label={t.timeLabel}
          value={time}
          onChange={(event) => {
            setTime(event.target.value);
            if (selected) onPick(keyOf(selected), event.target.value);
          }}
          className="ml-auto h-11 cursor-pointer rounded-[10px] border border-line-strong bg-popover px-3 text-[14.5px] font-medium text-foreground tabular-nums focus-visible:outline-2 focus-visible:outline-ring dark:bg-card"
        >
          {options.map((option) => (
            <option key={option} value={option}>
              {option} · {zoneOffset(now)}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
