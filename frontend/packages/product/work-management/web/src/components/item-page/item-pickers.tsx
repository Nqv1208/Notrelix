import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { Check, ChevronLeft, ChevronRight, Plus, Search } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger, cn } from "@notrelix/ui-web";
import { personInitials } from "@notrelix/work-management-core";
import { calendarParts, identityMarkClass } from "../shared/item-display";
import { fillSheetCopy, pluralSheetCopy } from "../card-detail/item-sheet.copy";
import type { ItemPageCopy } from "./item-page.copy";
import { DisabledControl } from "./item-page-parts";

/**
 * The property pickers of the item page (the States mockup): Status,
 * Priority, Assignee and the day picker. Each opens next to its property,
 * filters as you type, moves with the arrow keys, chooses with Enter or a
 * number, closes with Escape and saves the moment a choice is made. Pure:
 * the options, the people and the callbacks are injected.
 */

export type ItemPickerCopy = ItemPageCopy["pickers"];

const PANEL_CLASS =
  "w-75 gap-0 overflow-hidden rounded-[10px] border border-line-strong bg-popover p-0 shadow-lg ring-0";
const KEY_CLASS =
  "rounded-[4px] border border-line-strong bg-keycap px-1.25 font-mono text-key text-muted-foreground";
const SEARCH_ROW_CLASS =
  "flex h-10 items-center gap-2 border-b border-line px-3 text-meta-foreground";
const SEARCH_INPUT_CLASS =
  "min-w-0 flex-1 border-0 bg-transparent p-0 text-item-ui text-foreground outline-none placeholder:text-meta-foreground";
const ROW_CLASS =
  "flex min-h-8.5 cursor-pointer items-center gap-2.5 rounded-[7px] px-2.5 text-item-ui text-foreground";
const ROW_ACTIVE_CLASS = "bg-state-hover-strong";

/** No case, no diacritics: how a search compares names. */
export function foldForSearch(text: string): string {
  return text
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[đĐ]/g, "d")
    .toLowerCase()
    .trim();
}

/** The part of `text` the query matched, in the link ink. */
function Highlight({ text, query }: { text: string; query: string }) {
  const needle = foldForSearch(query);
  if (!needle) return <>{text}</>;
  // Fold char by char so the offsets stay those of the shown text.
  let folded = "";
  const starts: number[] = [];
  for (let index = 0; index < text.length; index += 1) {
    const piece = foldForSearch(text[index] ?? "");
    for (let k = 0; k < Math.max(piece.length, 1); k += 1) starts.push(index);
    folded += piece || " ";
  }
  const at = folded.indexOf(needle);
  if (at < 0) return <>{text}</>;
  const from = starts[at] ?? 0;
  const to = (starts[at + needle.length - 1] ?? text.length - 1) + 1;
  return (
    <>
      {text.slice(0, from)}
      <b className="font-semibold text-brand-subtle-foreground">
        {text.slice(from, to)}
      </b>
      {text.slice(to)}
    </>
  );
}

/** A popover that opens under its trigger; the panel is the picker. */
export function PickerPopover({
  trigger,
  open,
  onOpenChange,
  children,
  className,
}: {
  trigger: ReactNode;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  children: (close: () => void) => ReactNode;
  className?: string;
}) {
  const [innerOpen, setInnerOpen] = useState(false);
  const controlled = open !== undefined;
  const isOpen = controlled ? open : innerOpen;
  const setOpen = (next: boolean) => {
    if (!controlled) setInnerOpen(next);
    onOpenChange?.(next);
  };
  return (
    <Popover open={isOpen} onOpenChange={setOpen}>
      <PopoverTrigger asChild>{trigger}</PopoverTrigger>
      <PopoverContent
        align="start"
        className={cn(PANEL_CLASS, className)}
        onOpenAutoFocus={(event) => {
          // The search field takes focus, not the panel.
          event.preventDefault();
          const field = (
            event.currentTarget as HTMLElement
          ).querySelector<HTMLElement>("[data-picker-search]");
          (field ?? (event.currentTarget as HTMLElement)).focus();
        }}
      >
        {children(() => setOpen(false))}
      </PopoverContent>
    </Popover>
  );
}

export interface PickerOption {
  readonly id: string;
  readonly label: string;
  /** Classes of the marker before the label. */
  readonly markerClass?: string;
  /** The marker is a ring, not a disc (an option without weight, such as Thấp). */
  readonly hollow?: boolean;
}

/**
 * A searchable list of options: Status and Priority. The current one is
 * ticked; the others show the number that chooses them while the field is
 * empty. `none` adds a last row that clears the value (key 0).
 */
export function ItemListPicker({
  options,
  currentId,
  searchPlaceholder,
  searchLabel,
  noMatch,
  shortcut,
  none,
  footer,
  onSelect,
  onClear,
  idPrefix,
}: {
  options: readonly PickerOption[];
  currentId: string | null;
  searchPlaceholder: string;
  searchLabel: string;
  noMatch: string;
  /** The key that opens the picker, shown in the search field. */
  shortcut?: string;
  none?: string;
  footer?: ReactNode;
  onSelect: (optionId: string) => void;
  onClear?: () => void;
  idPrefix: string;
}) {
  const [query, setQuery] = useState("");
  const [highlightId, setHighlightId] = useState<string | null>(currentId);
  const needle = foldForSearch(query);
  const matches = useMemo(
    () =>
      needle
        ? options.filter((option) =>
            foldForSearch(option.label).includes(needle),
          )
        : options,
    [needle, options],
  );
  const highlighted =
    matches.find((option) => option.id === highlightId) ?? matches[0] ?? null;
  const move = (step: 1 | -1) => {
    if (matches.length === 0) return;
    const index = highlighted
      ? matches.findIndex((option) => option.id === highlighted.id)
      : -1;
    const next = matches[(index + step + matches.length) % matches.length];
    if (next) setHighlightId(next.id);
  };
  return (
    <>
      <div className={SEARCH_ROW_CLASS}>
        <Search aria-hidden="true" className="size-3.5 shrink-0" />
        <input
          data-picker-search
          type="text"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder={searchPlaceholder}
          aria-label={searchLabel}
          role="combobox"
          aria-expanded="true"
          aria-controls={`${idPrefix}-listbox`}
          aria-activedescendant={
            highlighted ? `${idPrefix}-option-${highlighted.id}` : undefined
          }
          onKeyDown={(event) => {
            if (event.key === "ArrowDown") {
              event.preventDefault();
              move(1);
            } else if (event.key === "ArrowUp") {
              event.preventDefault();
              move(-1);
            } else if (event.key === "Enter") {
              event.preventDefault();
              if (highlighted) onSelect(highlighted.id);
            } else if (query === "" && /^[1-9]$/.test(event.key)) {
              const option = options[Number(event.key) - 1];
              if (option) {
                event.preventDefault();
                onSelect(option.id);
              }
            } else if (query === "" && event.key === "0" && none && onClear) {
              event.preventDefault();
              onClear();
            }
          }}
          className={SEARCH_INPUT_CLASS}
        />
        {shortcut ? (
          <kbd aria-hidden="true" className={KEY_CLASS}>
            {shortcut}
          </kbd>
        ) : null}
      </div>
      <div
        role="listbox"
        id={`${idPrefix}-listbox`}
        aria-label={searchLabel}
        className="flex flex-col gap-0.5 p-1.5"
      >
        {matches.length === 0 ? (
          <p className="m-0 px-2.5 py-2 text-item-ui text-meta-foreground">
            {noMatch}
          </p>
        ) : (
          matches.map((option) => {
            const position = options.findIndex((o) => o.id === option.id) + 1;
            const current = option.id === currentId;
            return (
              <div
                key={option.id}
                id={`${idPrefix}-option-${option.id}`}
                role="option"
                aria-selected={current}
                onMouseEnter={() => setHighlightId(option.id)}
                onClick={() => onSelect(option.id)}
                className={cn(
                  ROW_CLASS,
                  option.id === highlighted?.id && ROW_ACTIVE_CLASS,
                )}
              >
                <span
                  aria-hidden="true"
                  className={cn(
                    "size-2.25 shrink-0 rounded-full",
                    option.hollow
                      ? "border-[1.5px] border-meta-foreground"
                      : option.markerClass,
                  )}
                />
                <span className="min-w-0 flex-1 truncate">{option.label}</span>
                {current ? (
                  <Check
                    aria-hidden="true"
                    className="size-3.5 text-brand-subtle-foreground"
                  />
                ) : position <= 9 ? (
                  <kbd aria-hidden="true" className={KEY_CLASS}>
                    {position}
                  </kbd>
                ) : null}
              </div>
            );
          })
        )}
        {none && onClear && !needle ? (
          <div
            role="option"
            aria-selected={currentId === null}
            onClick={onClear}
            className={cn(
              ROW_CLASS,
              "mt-1 border-t border-line rounded-t-none pt-1 text-muted-foreground",
            )}
          >
            <span className="min-w-0 flex-1 truncate">{none}</span>
            <kbd aria-hidden="true" className={KEY_CLASS}>
              0
            </kbd>
          </div>
        ) : null}
      </div>
      {footer ? (
        <div className="border-t border-line px-3.5 py-2.5 text-item-small">
          {footer}
        </div>
      ) : null}
    </>
  );
}

// ─── People ──────────────────────────────────────────────────────────────────

export interface PickerPerson {
  readonly userId: string;
  readonly name: string;
  /** Department; "Công nghệ". */
  readonly department: string | null;
  /** Open items the person holds on the board. */
  readonly openCount: number | null;
  /** The person sees the board, so an item given to them reaches them. */
  readonly onBoard: boolean;
}

/**
 * Who an item can be given to: the people of the board first, then the ones
 * who cannot see it (a manager can invite them, anyone else is told whom to
 * ask). A choice toggles the person and sends the whole set.
 */
export function ItemAssigneePicker({
  copy,
  people,
  loading,
  selectedUserIds,
  viewerUserId,
  canInvite,
  onChange,
  onInvite,
  onClose,
}: {
  copy: ItemPickerCopy;
  people: readonly PickerPerson[];
  loading?: boolean;
  selectedUserIds: readonly string[];
  viewerUserId?: string;
  canInvite: boolean;
  onChange: (userIds: string[]) => void;
  onInvite?: (userId: string) => void;
  onClose: () => void;
}) {
  const t = copy.assignee;
  const [query, setQuery] = useState("");
  const needle = foldForSearch(query);
  const matches = people.filter(
    (person) => !needle || foldForSearch(person.name).includes(needle),
  );
  const onBoard = matches.filter((person) => person.onBoard);
  const outside = matches.filter((person) => !person.onBoard);
  const rows = [...onBoard, ...outside.slice(0, 0)];
  const [highlightId, setHighlightId] = useState<string | null>(null);
  const highlighted =
    rows.find((person) => person.userId === highlightId) ?? rows[0] ?? null;
  const selected = new Set(selectedUserIds);
  const toggle = (userId: string) => {
    const next = new Set(selected);
    if (next.has(userId)) next.delete(userId);
    else next.add(userId);
    onChange(Array.from(next));
  };
  const move = (step: 1 | -1) => {
    if (rows.length === 0) return;
    const index = highlighted
      ? rows.findIndex((person) => person.userId === highlighted.userId)
      : -1;
    const next = rows[(index + step + rows.length) % rows.length];
    if (next) setHighlightId(next.userId);
  };
  const assignMe = () => {
    if (!viewerUserId) return;
    onChange([viewerUserId]);
    onClose();
  };

  const personRow = (person: PickerPerson) => {
    const active = person.userId === highlighted?.userId;
    const note = person.onBoard
      ? person.department && person.openCount !== null
        ? pluralSheetCopy(t.workload_one, t.workload_other, person.openCount, {
            department: person.department,
          })
        : person.department
          ? fillSheetCopy(t.workloadNone, { department: person.department })
          : null
      : person.department
        ? fillSheetCopy(t.outsideNote, { department: person.department })
        : t.outsideNoteNone;
    return (
      <div
        key={person.userId}
        role={person.onBoard ? "option" : undefined}
        aria-selected={person.onBoard ? selected.has(person.userId) : undefined}
        onMouseEnter={() => person.onBoard && setHighlightId(person.userId)}
        onClick={() => person.onBoard && toggle(person.userId)}
        className={cn(
          ROW_CLASS,
          "items-start py-1.5",
          person.onBoard ? null : "cursor-default",
          active && person.onBoard && ROW_ACTIVE_CLASS,
        )}
      >
        <span
          aria-hidden="true"
          className={cn(
            "mt-0.5 grid size-6 shrink-0 place-items-center rounded-full text-avatar-xs",
            identityMarkClass(person.userId),
            !person.onBoard && "opacity-60",
          )}
        >
          {personInitials(person.name)}
        </span>
        <span className="min-w-0 flex-1">
          <span
            className={cn(
              "block leading-tight",
              !person.onBoard && "text-muted-foreground",
            )}
          >
            <Highlight text={person.name} query={query} />
          </span>
          {note ? (
            <span className="block text-item-note leading-snug text-meta-foreground">
              {note}
            </span>
          ) : null}
        </span>
        {person.onBoard ? (
          selected.has(person.userId) ? (
            <Check
              aria-hidden="true"
              className="mt-1 size-3.5 text-brand-subtle-foreground"
            />
          ) : null
        ) : canInvite && onInvite ? (
          <button
            type="button"
            onClick={() => onInvite(person.userId)}
            className="shrink-0 rounded-[7px] border border-line-strong bg-popover px-2.5 py-1 text-item-small font-medium text-foreground hover:bg-state-hover-strong"
          >
            {t.invite}
          </button>
        ) : (
          <span className="max-w-[11ch] shrink-0 text-right text-item-note leading-snug text-meta-foreground">
            {t.askManager}
          </span>
        )}
      </div>
    );
  };

  return (
    <>
      <div className={SEARCH_ROW_CLASS}>
        <Search aria-hidden="true" className="size-3.5 shrink-0" />
        <input
          data-picker-search
          type="text"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder={t.search}
          aria-label={t.label}
          role="combobox"
          aria-expanded="true"
          onKeyDown={(event) => {
            if (event.key === "ArrowDown") {
              event.preventDefault();
              move(1);
            } else if (event.key === "ArrowUp") {
              event.preventDefault();
              move(-1);
            } else if (event.key === "Enter") {
              event.preventDefault();
              if (highlighted) toggle(highlighted.userId);
            } else if (query === "" && event.key === "Backspace") {
              event.preventDefault();
              onChange([]);
            } else if (
              query === "" &&
              event.key.toLowerCase() === "i" &&
              !event.metaKey &&
              !event.ctrlKey &&
              !event.altKey
            ) {
              event.preventDefault();
              assignMe();
            }
          }}
          className={SEARCH_INPUT_CLASS}
        />
      </div>
      <div className="max-h-80 overflow-y-auto p-1.5">
        {loading && people.length === 0 ? (
          <p className="m-0 px-2.5 py-2 text-item-ui text-meta-foreground">
            {t.loading}
          </p>
        ) : matches.length === 0 ? (
          <p className="m-0 px-2.5 py-2 text-item-ui text-meta-foreground">
            {t.empty}
          </p>
        ) : null}
        {onBoard.length > 0 ? (
          <section aria-label={t.members} role="listbox">
            <h3 className="m-0 px-2.5 pt-1.5 pb-1 text-item-note font-semibold text-muted-foreground">
              {t.members}
            </h3>
            {onBoard.map(personRow)}
          </section>
        ) : null}
        {outside.length > 0 ? (
          <section aria-label={t.outside}>
            <h3 className="m-0 px-2.5 pt-2 pb-1 text-item-note font-semibold text-muted-foreground">
              {t.outside}
            </h3>
            {outside.slice(0, 5).map(personRow)}
          </section>
        ) : null}
      </div>
      <div className="flex items-center justify-between border-t border-line px-3.5 py-2 text-item-note text-meta-foreground">
        <span className="inline-flex items-center gap-1.5">
          {t.clear}:
          <kbd aria-hidden="true" className={KEY_CLASS}>
            ⌫
          </kbd>
        </span>
        <button
          type="button"
          disabled={!viewerUserId}
          onClick={assignMe}
          className="inline-flex items-center gap-1.5 border-0 bg-transparent p-0 text-item-note text-meta-foreground disabled:opacity-50"
        >
          {t.assignMe}
          <kbd aria-hidden="true" className={KEY_CLASS}>
            I
          </kbd>
        </button>
      </div>
    </>
  );
}

// ─── Day picker ──────────────────────────────────────────────────────────────

interface Day {
  readonly year: number;
  readonly month: number;
  readonly day: number;
}

const WEEKDAY_KEYS = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"] as const;
const MONTH_KEYS = [
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

const dayKey = (day: Day) =>
  `${day.year}-${String(day.month).padStart(2, "0")}-${String(day.day).padStart(2, "0")}`;
const dayNumber = (day: Day) => Date.UTC(day.year, day.month - 1, day.day);
const addDays = (day: Day, count: number): Day => {
  const next = new Date(dayNumber(day) + count * 86_400_000);
  return {
    year: next.getUTCFullYear(),
    month: next.getUTCMonth() + 1,
    day: next.getUTCDate(),
  };
};
/** 0 = Monday … 6 = Sunday. */
const weekdayOf = (day: Day) => (new Date(dayNumber(day)).getUTCDay() + 6) % 7;

function dayOf(value: string | Date | null | undefined, timeZone?: string) {
  if (!value) return null;
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  const parts = calendarParts(date, timeZone);
  return { year: parts.year, month: parts.month, day: parts.day } as Day;
}

/** The instant a wall-clock moment of a time zone is. */
export function zonedInstant(
  day: Day,
  hour: number,
  minute: number,
  timeZone?: string,
): Date {
  const wall = Date.UTC(day.year, day.month - 1, day.day, hour, minute);
  let guess = wall;
  // Two passes settle the offset, also across a daylight-saving change.
  for (let pass = 0; pass < 2; pass += 1) {
    const parts = calendarParts(new Date(guess), timeZone);
    const shown = Date.UTC(
      parts.year,
      parts.month - 1,
      parts.day,
      parts.hour,
      parts.minute,
    );
    guess += wall - shown;
  }
  return new Date(guess);
}

/**
 * A month of days with quick picks above it. The day of `value` is filled,
 * the span from `rangeFrom` to it is tinted, today wears a ring. A choice
 * keeps the time of day the value had; "Thêm giờ" sets one. Days are those of
 * `timeZone`.
 */
export function ItemDatePicker({
  copy,
  value,
  rangeFrom,
  now,
  timeZone,
  onChange,
  onClose,
}: {
  copy: ItemPickerCopy;
  value: string | null;
  /** The start of the span that ends at `value`, shown tinted. */
  rangeFrom?: string | null;
  now: Date;
  timeZone?: string;
  onChange: (value: string | null) => void;
  onClose: () => void;
}) {
  const t = copy.date;
  const today = dayOf(now, timeZone) as Day;
  const selected = dayOf(value, timeZone);
  const from = dayOf(rangeFrom, timeZone);
  const valueParts = value ? calendarParts(new Date(value), timeZone) : null;
  const hasTime =
    valueParts !== null && (valueParts.hour !== 0 || valueParts.minute !== 0);
  const [time, setTime] = useState<string | null>(
    hasTime && valueParts
      ? `${String(valueParts.hour).padStart(2, "0")}:${String(valueParts.minute).padStart(2, "0")}`
      : null,
  );
  const [timeOpen, setTimeOpen] = useState(hasTime);
  const [shown, setShown] = useState<{ year: number; month: number }>(
    selected ?? today,
  );
  const [focused, setFocused] = useState<Day>(selected ?? today);
  const gridRef = useRef<HTMLDivElement>(null);
  const moveFocus = useRef(false);
  useEffect(() => {
    if (!moveFocus.current) return;
    moveFocus.current = false;
    gridRef.current
      ?.querySelector<HTMLButtonElement>(`[data-day="${dayKey(focused)}"]`)
      ?.focus();
  });

  const pick = (day: Day, clock: string | null = time) => {
    const [hour, minute] = (clock ?? "00:00").split(":").map(Number);
    onChange(zonedInstant(day, hour ?? 0, minute ?? 0, timeZone).toISOString());
    onClose();
  };

  const friday = addDays(today, (4 - weekdayOf(today) + 7) % 7 || 7);
  const nextWeek = addDays(today, 7 - weekdayOf(today));
  const chips: readonly { label: string; day: Day }[] = [
    { label: t.today, day: today },
    { label: t.tomorrow, day: addDays(today, 1) },
    { label: t.friday, day: friday },
    { label: t.nextWeek, day: nextWeek },
  ];

  const firstOfMonth: Day = { year: shown.year, month: shown.month, day: 1 };
  const lead = weekdayOf(firstOfMonth);
  const daysInMonth = new Date(
    Date.UTC(shown.year, shown.month, 0),
  ).getUTCDate();
  const cells = Math.ceil((lead + daysInMonth) / 7) * 7;
  const days = Array.from({ length: cells }, (_, index) =>
    addDays(firstOfMonth, index - lead),
  );
  const stepMonth = (step: number) => {
    const next = new Date(Date.UTC(shown.year, shown.month - 1 + step, 1));
    setShown({
      year: next.getUTCFullYear(),
      month: next.getUTCMonth() + 1,
    });
  };

  return (
    <div>
      <div className="flex flex-wrap gap-1.5 px-3 pt-3 pb-2.5">
        {chips.map((chip) => (
          <button
            key={chip.label}
            type="button"
            onClick={() => pick(chip.day)}
            className="h-6.5 rounded-full border border-line-strong bg-popover px-2.5 text-item-small font-medium text-foreground hover:bg-state-hover-strong"
          >
            {chip.label}
          </button>
        ))}
      </div>
      <div className="border-t border-line px-3 pt-3 pb-2">
        <div className="mb-2 flex items-center justify-between">
          <b className="text-item-ui font-semibold">
            {fillSheetCopy(t.month, {
              month: t.months[MONTH_KEYS[shown.month - 1] ?? "jan"],
              year: shown.year,
            })}
          </b>
          <span className="flex gap-1">
            <button
              type="button"
              aria-label={t.previous}
              onClick={() => stepMonth(-1)}
              className="grid size-6 place-items-center rounded-md border-0 bg-transparent text-muted-foreground hover:bg-state-hover-strong"
            >
              <ChevronLeft aria-hidden="true" className="size-4" />
            </button>
            <button
              type="button"
              aria-label={t.next}
              onClick={() => stepMonth(1)}
              className="grid size-6 place-items-center rounded-md border-0 bg-transparent text-muted-foreground hover:bg-state-hover-strong"
            >
              <ChevronRight aria-hidden="true" className="size-4" />
            </button>
          </span>
        </div>
        <div
          ref={gridRef}
          role="grid"
          aria-label={t.label}
          className="grid grid-cols-7"
          onKeyDown={(event) => {
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
            if (!step) return;
            event.preventDefault();
            const next = addDays(focused, step);
            setFocused(next);
            setShown({ year: next.year, month: next.month });
            moveFocus.current = true;
          }}
        >
          {WEEKDAY_KEYS.map((key) => t.weekdays[key]).map((name) => (
            <span
              key={name}
              role="columnheader"
              className="grid h-7 place-items-center text-item-note font-semibold text-muted-foreground"
            >
              {name}
            </span>
          ))}
          {days.map((day) => {
            const key = dayKey(day);
            const outside = day.month !== shown.month;
            const isSelected = selected !== null && key === dayKey(selected);
            const inRange =
              from !== null &&
              selected !== null &&
              dayNumber(day) >= dayNumber(from) &&
              dayNumber(day) <= dayNumber(selected);
            const isToday = key === dayKey(today);
            const weekend = weekdayOf(day) >= 5;
            return (
              <button
                key={key}
                type="button"
                role="gridcell"
                data-day={key}
                aria-selected={isSelected}
                aria-current={isToday ? "date" : undefined}
                tabIndex={key === dayKey(focused) ? 0 : -1}
                onFocus={() => setFocused(day)}
                onClick={() => pick(day)}
                className={cn(
                  "grid h-8.5 place-items-center border-0 bg-transparent p-0 text-item-ui text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                  outside && "text-meta-foreground opacity-60",
                  !outside && weekend && "text-muted-foreground",
                  inRange && !isSelected && "bg-brand-subtle",
                  isSelected &&
                    "rounded-md bg-primary font-semibold text-primary-foreground",
                  isToday &&
                    !isSelected &&
                    "rounded-md shadow-[inset_0_0_0_1.5px_var(--primary)]",
                )}
              >
                {day.day}
              </button>
            );
          })}
        </div>
      </div>
      <div className="flex items-center justify-between border-t border-line px-3.5 py-2.5 text-item-small">
        {timeOpen ? (
          <label className="inline-flex items-center gap-2 text-muted-foreground">
            {t.time}
            <input
              type="time"
              value={time ?? ""}
              aria-label={t.time}
              onChange={(event) => {
                setTime(event.target.value || null);
                if (selected && event.target.value)
                  onChange(
                    zonedInstant(
                      selected,
                      Number(event.target.value.slice(0, 2)),
                      Number(event.target.value.slice(3, 5)),
                      timeZone,
                    ).toISOString(),
                  );
              }}
              className="h-6.5 rounded-md border border-line-strong bg-background px-1.5 text-item-small text-foreground"
            />
          </label>
        ) : (
          <button
            type="button"
            onClick={() => setTimeOpen(true)}
            className="inline-flex items-center gap-1 border-0 bg-transparent p-0 text-item-small text-muted-foreground"
          >
            <Plus aria-hidden="true" className="size-3" />
            {t.addTime}
          </button>
        )}
        {value ? (
          <button
            type="button"
            onClick={() => {
              onChange(null);
              onClose();
            }}
            className="border-0 bg-transparent p-0 text-item-small font-medium text-foreground hover:underline"
          >
            {t.clear}
          </button>
        ) : null}
      </div>
    </div>
  );
}

export { DisabledControl as PickerDisabled };
