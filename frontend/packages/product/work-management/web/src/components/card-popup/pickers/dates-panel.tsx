import { useEffect, useRef, useState } from "react";
import {
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Clock,
  TriangleAlert,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { calendarParts } from "../../shared/item-display";
import { zonedInstant } from "../../item-page/item-pickers";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import { IconBtn, PanelBtn } from "../popup-ui";
import { Hint, PickerHead } from "./picker-bits";

interface Day {
  readonly year: number;
  readonly month: number;
  readonly day: number;
}

const WEEKDAYS = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"] as const;
const MONTHS = [
  "jan",
  "feb",
  "mar",
  "apr",
  "may",
  "jun",
  "jul",
  "aug",
  "sep",
  "oct",
  "nov",
  "dec",
] as const;
const DEFAULT_TIME = "17:00";
const DEFAULT_START_TIME = "09:00";
const TIMES = Array.from(
  { length: 48 },
  (_, i) =>
    `${String(Math.floor(i / 2)).padStart(2, "0")}:${i % 2 ? "30" : "00"}`,
);

const num = (d: Day) => Date.UTC(d.year, d.month - 1, d.day);
const key = (d: Day) => `${d.year}-${d.month}-${d.day}`;
const addDays = (d: Day, n: number): Day => {
  const next = new Date(num(d) + n * 86_400_000);
  return {
    year: next.getUTCFullYear(),
    month: next.getUTCMonth() + 1,
    day: next.getUTCDate(),
  };
};
const weekdayOf = (d: Day) => (new Date(num(d)).getUTCDay() + 6) % 7;
const format = (d: Day) =>
  `${String(d.day).padStart(2, "0")}/${String(d.month).padStart(2, "0")}/${d.year}`;
function parse(text: string): Day | null {
  const m = /^\s*(\d{1,2})\/(\d{1,2})\/(\d{4})\s*$/.exec(text);
  if (!m) return null;
  const day = { year: +m[3]!, month: +m[2]!, day: +m[1]! };
  const check = new Date(num(day));
  return check.getUTCFullYear() === day.year &&
    check.getUTCMonth() + 1 === day.month &&
    check.getUTCDate() === day.day
    ? day
    : null;
}
const dayOf = (value: string | null | undefined, tz?: string): Day | null => {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  const p = calendarParts(date, tz);
  return { year: p.year, month: p.month, day: p.day };
};

const INPUT =
  "box-border h-9 w-full min-w-0 rounded-[8px] border border-line-strong bg-popover px-3 text-[14px] text-foreground focus:border-primary focus:ring-[3px] focus:ring-brand-subtle focus:outline-none disabled:bg-background disabled:text-meta-foreground";

/** The time of the due date: a list of half hours. */
function TimeSelect({
  value,
  label,
  disabled,
  placeholder,
  onChange,
}: {
  value: string;
  label: string;
  disabled: boolean;
  /** Shown instead of the time while the date is off. */
  placeholder?: string;
  onChange: (value: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!open) return;
    ref.current
      ?.querySelector<HTMLElement>("[aria-selected=true]")
      ?.scrollIntoView({ block: "center" });
  }, [open]);
  const options = TIMES.includes(value) ? TIMES : [value, ...TIMES].sort();
  return (
    <div className="relative min-w-0 flex-1 px-3">
      <button
        type="button"
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-label={label}
        onClick={() => setOpen(!open)}
        onKeyDown={(event) => {
          if (event.key === "Escape" && open) {
            event.stopPropagation();
            setOpen(false);
          }
        }}
        className={cn(
          INPUT,
          "-mx-3 flex w-[calc(100%+24px)] items-center gap-2 pr-2.5 text-left disabled:opacity-60",
        )}
      >
        <Clock
          aria-hidden="true"
          className="size-4 shrink-0 text-meta-foreground"
        />
        {disabled && placeholder ? (
          <span className="text-meta-foreground">{placeholder}</span>
        ) : (
          value
        )}
        <ChevronDown
          aria-hidden="true"
          className="ml-auto size-4 text-meta-foreground"
        />
      </button>
      {open ? (
        <div
          ref={ref}
          role="listbox"
          aria-label={label}
          className="absolute inset-x-0 top-[calc(100%+4px)] z-20 max-h-[220px] overflow-y-auto rounded-[10px] border border-line-strong bg-popover p-1.5 shadow-popover"
        >
          {options.map((time) => (
            <button
              key={time}
              type="button"
              role="option"
              aria-selected={time === value}
              onClick={() => {
                onChange(time);
                setOpen(false);
              }}
              className={cn(
                "flex min-h-9 w-full items-center rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] hover:bg-state-hover-strong",
                time === value &&
                  "bg-brand-subtle font-semibold text-brand-subtle-foreground",
              )}
            >
              {time}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

/**
 * The dates panel: a calendar with month and year steps, an optional start
 * date, the due date with its time, and Save or Remove. Days are those of the
 * card's time zone; the due date is written as an instant (DueAt).
 */
export function DatesPanel({ onClose }: { onClose: () => void }) {
  const { card, copy, callbacks, now, timeZone, slots } = useCardPopup();
  const t = copy.pickers.dates;
  const today = dayOf(now.toISOString(), timeZone) as Day;
  const dueInit = dayOf(card.dueAt ?? card.dueDate, timeZone);
  const startInit = dayOf(card.startDate, timeZone);
  const timeOf = (value: string | null | undefined, fallback: string) => {
    if (!value) return fallback;
    const p = calendarParts(new Date(value), timeZone);
    return p.hour === 0 && p.minute === 0
      ? fallback
      : `${String(p.hour).padStart(2, "0")}:${String(p.minute).padStart(2, "0")}`;
  };
  const timeInit = timeOf(card.dueAt ?? card.dueDate, DEFAULT_TIME);

  const [startOn, setStartOn] = useState(startInit !== null);
  const [dueOn, setDueOn] = useState(dueInit !== null || startInit === null);
  const [startText, setStartText] = useState(
    startInit ? format(startInit) : "",
  );
  const [dueText, setDueText] = useState(
    dueInit ? format(dueInit) : format(today),
  );
  const [time, setTime] = useState(timeInit);
  const [startTime, setStartTime] = useState(
    timeOf(card.startDate, DEFAULT_START_TIME),
  );
  const [target, setTarget] = useState<"start" | "due">("due");
  const [shown, setShown] = useState({
    year: (dueInit ?? startInit ?? today).year,
    month: (dueInit ?? startInit ?? today).month,
  });
  const [focused, setFocused] = useState<Day>(dueInit ?? startInit ?? today);
  const grid = useRef<HTMLDivElement>(null);
  const moveFocus = useRef(false);
  useEffect(() => {
    if (!moveFocus.current) return;
    moveFocus.current = false;
    grid.current
      ?.querySelector<HTMLButtonElement>(`[data-day="${key(focused)}"]`)
      ?.focus();
  });

  const start = startOn ? parse(startText) : null;
  const due = dueOn ? parse(dueText) : null;
  const startBad = startOn && start === null;
  const dueBad = dueOn && due === null;
  const minutes = (value: string) => {
    const [h, m] = value.split(":").map(Number);
    return (h ?? 0) * 60 + (m ?? 0);
  };
  // The due moment must come after the start moment: the day first, the time on the same day.
  const orderBad =
    start !== null &&
    due !== null &&
    (num(due) < num(start) ||
      (num(due) === num(start) && minutes(time) <= minutes(startTime)));
  const blocked = startBad || dueBad || orderBad;

  const setTargetDay = (day: Day) => {
    if (target === "start" && startOn) setStartText(format(day));
    else {
      setDueOn(true);
      setDueText(format(day));
    }
    setFocused(day);
  };
  const step = (months: number) => {
    const next = new Date(Date.UTC(shown.year, shown.month - 1 + months, 1));
    setShown({ year: next.getUTCFullYear(), month: next.getUTCMonth() + 1 });
  };

  const first: Day = { year: shown.year, month: shown.month, day: 1 };
  const lead = weekdayOf(first);
  const count = new Date(Date.UTC(shown.year, shown.month, 0)).getUTCDate();
  const days = Array.from(
    { length: Math.ceil((lead + count) / 7) * 7 },
    (_, i) => addDays(first, i - lead),
  );

  const write = (startDate: string | null, dueDate: string | null) => {
    callbacks.onUpdateItem?.(card.id, { startDate, dueDate });
    onClose();
  };
  const save = () => {
    if (blocked) return;
    const [h, m] = time.split(":").map(Number);
    const [sh, sm] = startTime.split(":").map(Number);
    write(
      start
        ? zonedInstant(start, sh ?? 0, sm ?? 0, timeZone).toISOString()
        : null,
      due ? zonedInstant(due, h ?? 0, m ?? 0, timeZone).toISOString() : null,
    );
  };

  const navBtn = (
    label: string,
    onClick: () => void,
    icon: React.ReactNode,
  ) => (
    <IconBtn small aria-label={label} onClick={onClick}>
      {icon}
    </IconBtn>
  );
  const ic = "size-4";

  return (
    <>
      <PickerHead title={t.title} onClose={onClose} />
      <div className="mb-1 flex items-center justify-between">
        {navBtn(
          t.previousYear,
          () => step(-12),
          <ChevronsLeft aria-hidden="true" className={ic} />,
        )}
        {navBtn(
          t.previousMonth,
          () => step(-1),
          <ChevronLeft aria-hidden="true" className={ic} />,
        )}
        <b aria-live="polite" className="text-[14px]">
          {fillPopupCopy(t.month, {
            month: t.months[MONTHS[shown.month - 1] ?? "jan"],
            year: shown.year,
          })}
        </b>
        {navBtn(
          t.nextMonth,
          () => step(1),
          <ChevronRight aria-hidden="true" className={ic} />,
        )}
        {navBtn(
          t.nextYear,
          () => step(12),
          <ChevronsRight aria-hidden="true" className={ic} />,
        )}
      </div>
      <div
        ref={grid}
        role="grid"
        aria-label={t.title}
        className="grid grid-cols-7 text-center"
        onKeyDown={(event) => {
          const delta =
            { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 }[
              event.key
            ] ?? 0;
          if (!delta) return;
          event.preventDefault();
          const next = addDays(focused, delta);
          setFocused(next);
          setShown({ year: next.year, month: next.month });
          moveFocus.current = true;
        }}
      >
        {WEEKDAYS.map((name) => (
          <span
            key={name}
            role="columnheader"
            className="grid h-6 place-items-center text-[12px] font-semibold text-muted-foreground"
          >
            {t.weekdays[name]}
          </span>
        ))}
        {days.map((day) => {
          const outside = day.month !== shown.month;
          const isStart = start !== null && key(day) === key(start);
          const isDue = due !== null && key(day) === key(due);
          const selected = isStart || isDue;
          const inRange =
            start !== null &&
            due !== null &&
            num(day) > num(start) &&
            num(day) < num(due);
          const isToday = key(day) === key(today);
          return (
            <button
              key={key(day)}
              type="button"
              role="gridcell"
              data-day={key(day)}
              aria-selected={selected}
              aria-current={isToday ? "date" : undefined}
              aria-label={fillPopupCopy(t.dayLabel, {
                day: day.day,
                month: t.months[MONTHS[day.month - 1] ?? "jan"].toLowerCase(),
                year: day.year,
              })}
              tabIndex={key(day) === key(focused) ? 0 : -1}
              onFocus={() => setFocused(day)}
              onClick={() => setTargetDay(day)}
              className={cn(
                "relative grid h-[34px] place-items-center border-0 bg-transparent p-0 text-[13.5px] text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-primary",
                "rounded-[6px]",
                outside && "text-meta-foreground",
                inRange && "rounded-none bg-brand-subtle",
                selected &&
                  "bg-primary font-bold text-primary-foreground hover:bg-primary",
                isToday &&
                  "after:absolute after:inset-x-2 after:bottom-[3px] after:h-0.5 after:rounded-[1px] after:bg-primary",
                isToday && selected && "after:bg-primary-foreground",
              )}
            >
              {day.day}
            </button>
          );
        })}
      </div>

      <div className="mt-3.5 flex min-w-0 flex-col gap-1.5">
        <span
          id="popup-date-start"
          className="text-[12px] leading-[18px] font-semibold whitespace-nowrap text-muted-foreground"
        >
          {t.start}
        </span>
        <div className="flex items-center gap-2.5">
          <input
            type="checkbox"
            checked={startOn}
            aria-label={t.useStart}
            onChange={(event) => {
              setStartOn(event.target.checked);
              if (event.target.checked) {
                setTarget("start");
                setStartText(startText || dueText || format(today));
              } else setTarget("due");
            }}
            className="m-0 size-[18px] shrink-0 accent-primary"
          />
          <input
            type="text"
            inputMode="numeric"
            disabled={!startOn}
            value={startText}
            placeholder={t.placeholder}
            aria-labelledby="popup-date-start"
            aria-invalid={startBad || orderBad || undefined}
            onFocus={() => setTarget("start")}
            onChange={(event) => {
              setStartText(event.target.value);
              const day = parse(event.target.value);
              if (day) setShown({ year: day.year, month: day.month });
            }}
            className={cn(
              INPUT,
              "flex-1",
              (startBad || orderBad) &&
                "border-danger ring-[3px] ring-danger-subtle",
            )}
            style={{ flex: "1.2 1 0%" }}
          />
          <TimeSelect
            value={startTime}
            label={t.startTime}
            placeholder={t.timePlaceholder}
            disabled={!startOn}
            onChange={setStartTime}
          />
        </div>
        {orderBad ? (
          <Hint error className="mt-1.5">
            <TriangleAlert
              aria-hidden="true"
              className="mt-px size-3.5 shrink-0"
            />
            <span>{t.orderError}</span>
          </Hint>
        ) : null}
      </div>

      <div className="mt-3 flex min-w-0 flex-col gap-1.5">
        <span
          id="popup-date-due"
          className="text-[12px] leading-[18px] font-semibold whitespace-nowrap text-muted-foreground"
        >
          {t.due}
        </span>
        <div className="flex items-center gap-2.5">
          <input
            type="checkbox"
            checked={dueOn}
            aria-label={t.useDue}
            onChange={(event) => {
              setDueOn(event.target.checked);
              setTarget("due");
            }}
            className="m-0 size-[18px] shrink-0 accent-primary"
          />
          <input
            type="text"
            inputMode="numeric"
            disabled={!dueOn}
            value={dueText}
            placeholder={t.placeholder}
            aria-labelledby="popup-date-due"
            aria-invalid={dueBad || undefined}
            onFocus={() => setTarget("due")}
            onChange={(event) => {
              setDueText(event.target.value);
              const day = parse(event.target.value);
              if (day) setShown({ year: day.year, month: day.month });
            }}
            style={{ flex: "1.2 1 0%" }}
            className={cn(
              INPUT,
              dueBad && "border-danger ring-[3px] ring-danger-subtle",
            )}
          />
          <TimeSelect
            value={time}
            label={t.time}
            disabled={!dueOn}
            onChange={setTime}
          />
        </div>
        {dueBad ? <Hint error>{t.invalid}</Hint> : null}
      </div>

      {slots.repeat}
      {slots.reminder}

      <PanelBtn
        variant="primary"
        className="mt-3.5 w-full"
        disabled={blocked}
        onClick={save}
      >
        {copy.pickers.dates.save}
      </PanelBtn>
      <PanelBtn className="mt-2 w-full" onClick={() => write(null, null)}>
        {t.clear}
      </PanelBtn>
    </>
  );
}
