import {
  useEffect,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import {
  AlertCircle,
  Check,
  ChevronLeft,
  ChevronRight,
  Lock,
  Search,
  X,
} from "lucide-react";
import {
  cn,
  Popover,
  PopoverAnchor,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import {
  specOf,
  type DraftError,
  type FieldDefinition,
} from "@notrelix/work-management-core";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";
import { identityMarkClass } from "../../shared/item-display";
import { dayOf, EDIT_TRIGGER_CLASS } from "./table-cell-editors";

/**
 * The in-cell editors of the custom field types, one way to edit per type:
 * a line typed in place (Text, Number, Link), a popover (Long text, Select,
 * Multi select, Person, Date) and a one-tap box (Checkbox). Each hands the
 * finished value to `onCommit`; what happens next (saving, error, conflict)
 * belongs to the cell around it.
 */

type FieldCopy = MainTableCopy["fields"];

const NOTE_CLASS =
  "z-modal flex w-80 flex-col gap-1.5 rounded-[10px] border bg-popover p-2.5 text-compact text-popover-foreground shadow-pop ring-0 dark:bg-card";

/** A message under the cell (an error, a conflict); it never takes the focus unless asked. */
export function AnchoredNote({
  note,
  tone,
  focusRef,
  onEscape,
  className,
  noteClassName,
  plain = false,
  children,
}: {
  note: ReactNode;
  tone: "danger" | "warning";
  /** A bare message under the cell, with no box (`WM-Field-Editors`: the invalid draft). */
  plain?: boolean;
  focusRef?: React.RefObject<HTMLElement | null>;
  onEscape?: () => void;
  className?: string;
  /** Sizes the box when a drawn frame sets its own (`WM-StaleSchema`'s 330 px note). */
  noteClassName?: string;
  children: ReactNode;
}) {
  return (
    <Popover open={Boolean(note)}>
      <PopoverAnchor asChild>
        <span
          className={cn(
            "flex min-w-0 flex-1 items-center gap-[7px] rounded-md",
            className,
          )}
        >
          {children}
        </span>
      </PopoverAnchor>
      <PopoverContent
        // The note's own alert carries the message; a nameless dialog would fail axe.
        role="group"
        align="start"
        side="bottom"
        // The cell is 40 px and its input 28: a plain message sits 6 px under the cell, a box 2 px under its anchor.
        sideOffset={plain ? 12.5 : 2}
        collisionPadding={8}
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          focusRef?.current?.focus();
        }}
        onCloseAutoFocus={(event) => event.preventDefault()}
        onInteractOutside={(event) => event.preventDefault()}
        onEscapeKeyDown={onEscape}
        className={cn(
          plain
            ? "z-modal flex w-auto flex-row items-center gap-1 rounded-none border-0 bg-transparent p-0 text-caption shadow-none ring-0"
            : NOTE_CLASS,
          !plain &&
            (tone === "danger" ? "border-danger-text" : "border-warning-text"),
          noteClassName,
        )}
      >
        {note}
      </PopoverContent>
    </Popover>
  );
}

function sameValue(field: FieldDefinition, next: unknown, current: unknown) {
  const spec = specOf(field);
  return next === current || (spec.isEmpty(next) && spec.isEmpty(current));
}

const DRAFT_MESSAGE: Record<DraftError, keyof FieldCopy> = {
  invalid_number: "invalidNumber",
  invalid_link: "invalidLink",
  invalid_date: "invalidDate",
  read_only: "lockType",
};

/** One option as the cell and the lists draw it: a tinted 22 px chip, 6 px radius, 12 px label. */
export function OptionChip({ option }: { option: EditorOption }) {
  return (
    <span className="inline-flex h-5.5 min-w-0 items-center gap-1.5 rounded-sm bg-state-hover px-2 text-caption font-semibold text-foreground">
      {option.color ? (
        <span
          aria-hidden="true"
          className="size-2 shrink-0 rounded-full"
          style={{ backgroundColor: option.color }}
        />
      ) : null}
      <span className="truncate">{option.label}</span>
    </span>
  );
}

/** A person's 22 px mark. */
export function PersonMark({ option }: { option: EditorOption }) {
  return (
    <span
      aria-hidden="true"
      title={option.label}
      className={cn(
        "flex size-5.5 shrink-0 items-center justify-center rounded-full text-mark-xs font-bold select-none",
        identityMarkClass(option.id),
      )}
    >
      {option.initials}
    </span>
  );
}

const POP_BUTTON =
  "h-6.5 cursor-pointer rounded-[7px] border px-2 text-caption focus-visible:outline-2 focus-visible:outline-ring";
const POP_PRIMARY = `${POP_BUTTON} border-primary bg-primary font-semibold text-primary-foreground`;
const POP_TEXT = `${POP_BUTTON} border-transparent bg-transparent font-medium text-muted-foreground hover:bg-state-hover-strong`;
const POP_PLAIN = `${POP_BUTTON} border-line-strong bg-transparent font-medium text-foreground hover:bg-state-hover-strong`;
/** A popover as the editors draw it: 8 px inside a 1 px border, 6 px between its parts. */
const POP_BOX =
  "gap-1.5 rounded-[10px] border border-line-strong bg-popover p-2 text-compact shadow-pop ring-0 dark:bg-card";
/** The line between a list and its foot, reaching the popover's border. */
const POP_RULE = "-mx-2 my-0.5 h-px bg-border";
/** The editors open under the cell, 2 px down and level with its edge (the trigger sits 4 px in, 24 px tall in a 40 px cell). */
const POP_PLACE = {
  align: "start",
  side: "bottom",
  alignOffset: -4,
  sideOffset: 10.5,
} as const;

/** A line of text, a number or a link typed in the cell: Enter saves, Escape puts the old value back, leaving the cell saves. */
export function InlineInputEditor({
  field,
  value,
  text,
  label,
  copy,
  alignEnd = false,
  unit,
  trailing,
  onCommit,
  children,
}: {
  field: FieldDefinition;
  value: unknown;
  /** The value as the draft starts. */
  text: string;
  label: string;
  copy: FieldCopy;
  alignEnd?: boolean;
  /** Drawn after the input while it is open ("giờ"). */
  unit?: string | null;
  /** Shown beside the value while the cell is not being typed in (the link's open icon). */
  trailing?: ReactNode;
  /** `stale`: someone else changed the cell while this one was open. */
  onCommit: (value: unknown, stale?: boolean) => void;
  children: ReactNode;
}) {
  const [editing, setEditing] = useState(false);
  const started = useRef<unknown>(undefined);
  const [draft, setDraft] = useState("");
  const [error, setError] = useState<DraftError | null>(null);
  const settled = useRef(false);
  const refocus = useRef(false);
  const trigger = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    if (!editing && refocus.current) {
      refocus.current = false;
      trigger.current?.focus();
    }
  }, [editing]);

  const end = (save: boolean, restoreFocus: boolean) => {
    if (settled.current) return;
    if (save) {
      const parsed = specOf(field).parseDraft(draft);
      // A draft that does not parse stays open with its reason and is never sent.
      if (!parsed.ok) return setError(parsed.error);
      settled.current = true;
      if (!sameValue(field, parsed.value, value))
        onCommit(parsed.value, !sameValue(field, started.current, value));
    } else {
      settled.current = true;
    }
    refocus.current = restoreFocus;
    setError(null);
    setEditing(false);
  };

  if (!editing)
    return (
      <>
        <button
          ref={trigger}
          type="button"
          aria-label={text ? `${label}: ${text}` : label}
          title={text || undefined}
          onClick={() => {
            settled.current = false;
            started.current = value;
            setDraft(text);
            setEditing(true);
          }}
          className={cn(EDIT_TRIGGER_CLASS, "max-w-full flex-1")}
        >
          {children}
        </button>
        {trailing}
      </>
    );
  return (
    <AnchoredNote
      tone="danger"
      plain
      note={
        error ? (
          <p
            role="alert"
            className="m-0 flex items-center gap-1 leading-[1.45] font-semibold text-danger-text"
          >
            <AlertCircle className="size-4" aria-hidden="true" />
            {copy[DRAFT_MESSAGE[error]] as string}
          </p>
        ) : null
      }
    >
      <input
        autoFocus
        type="text"
        value={draft}
        aria-label={label}
        aria-invalid={error ? true : undefined}
        onChange={(event) => {
          setError(null);
          setDraft(event.target.value);
        }}
        onFocus={(event) => event.currentTarget.select()}
        onBlur={() => end(true, false)}
        onKeyDown={(event) => {
          if (event.key === "Enter") {
            event.preventDefault();
            end(true, true);
          } else if (event.key === "Escape") {
            event.preventDefault();
            event.stopPropagation();
            end(false, true);
          }
        }}
        className={cn(
          "h-7 min-w-0 flex-1 rounded-[6px] border border-line-strong bg-popover px-2 text-compact text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card",
          alignEnd && "text-right tabular-nums",
          error && "border-danger-text ring-1 ring-danger-text",
        )}
      />
      {unit ? (
        <span className="flex-none text-crumb text-meta-foreground">
          {unit}
        </span>
      ) : null}
    </AnchoredNote>
  );
}

/** A long text typed into a popover: Enter saves, Shift+Enter breaks the line, Escape cancels, leaving saves. */
export function LongTextEditor({
  text,
  label,
  copy,
  onCommit,
  children,
}: {
  text: string;
  label: string;
  copy: FieldCopy;
  onCommit: (value: string | null, stale?: boolean) => void;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState(text);
  const cancelled = useRef(false);
  const started = useRef(text);
  const finish = (save: boolean) => {
    cancelled.current = !save;
    if (save && draft !== text)
      onCommit(draft.trim() === "" ? null : draft, text !== started.current);
    setOpen(false);
  };
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (next) {
          cancelled.current = false;
          started.current = text;
          setDraft(text);
          setOpen(true);
        } else finish(!cancelled.current);
      }}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={text ? `${label}: ${text}` : label}
          title={text || undefined}
          className={cn(EDIT_TRIGGER_CLASS, "max-w-full flex-1")}
        >
          {children}
        </button>
      </PopoverTrigger>
      <PopoverContent
        {...POP_PLACE}
        aria-label={label}
        onEscapeKeyDown={() => {
          cancelled.current = true;
        }}
        className={cn(POP_BOX, "w-85")}
      >
        <textarea
          autoFocus
          value={draft}
          aria-label={label}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter" && !event.shiftKey) {
              event.preventDefault();
              finish(true);
            }
          }}
          className="h-27.5 w-full resize-none rounded-[7px] border border-line-strong bg-background px-3 py-2.5 text-compact leading-[1.55] text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
        />
        <div className="flex justify-end gap-1.5">
          <button
            type="button"
            onClick={() => finish(false)}
            className={POP_TEXT}
          >
            {copy.cancel}
          </button>
          <button
            type="button"
            onClick={() => finish(true)}
            className={POP_PRIMARY}
          >
            {copy.save}
          </button>
        </div>
      </PopoverContent>
    </Popover>
  );
}

/** The 16 px box of a checkbox cell (`WM-Field-Editors`): one tap flips it and saves at once. */
const CHECK_BOX =
  "grid size-4 shrink-0 place-items-center rounded-[4px] border-[1.5px] p-0";

/** The one-tap box: Space or a click flips it and saves at once; the text beside it says which way it is set. */
export function CheckboxEditor({
  checked,
  label,
  editable,
  caption,
  saving = false,
  onCommit,
}: {
  checked: boolean;
  label: string;
  editable: boolean;
  /** What the box means: "Chưa" or "Đã …". */
  caption: string;
  saving?: boolean;
  onCommit: (value: boolean) => void;
}) {
  const box = cn(
    CHECK_BOX,
    checked
      ? "border-primary bg-primary text-primary-foreground"
      : "border-line-strong bg-popover dark:bg-card",
  );
  const mark = checked ? <Check className="size-2.75" strokeWidth={3} /> : null;
  const text = (
    <span
      className={cn(
        "min-w-0 truncate text-nav",
        checked && !saving ? "text-foreground" : "text-meta-foreground",
      )}
    >
      {caption}
    </span>
  );
  if (!editable)
    return (
      <>
        <span role="img" aria-label={label} className={box}>
          {mark}
        </span>
        {text}
        <Lock
          className="ml-auto size-4 flex-none text-meta-foreground"
          aria-hidden="true"
        />
      </>
    );
  return (
    // A real button toggles on Enter and click; Space is taken on key release so one press is one toggle in every browser.
    <span
      role="presentation"
      className="flex min-w-0 items-center gap-[7px]"
      onKeyUp={(event) => {
        if (event.key !== " ") return;
        event.preventDefault();
        onCommit(!checked);
      }}
    >
      <button
        type="button"
        role="checkbox"
        aria-checked={checked}
        aria-label={label}
        disabled={saving}
        onClick={() => onCommit(!checked)}
        className={cn(
          box,
          "cursor-pointer focus-visible:outline-2 focus-visible:outline-ring",
          // On the tablet the 16 px box takes a 44 px touch area around it; the drawing does not move.
          "tablet:relative tablet:after:absolute tablet:after:-inset-4 tablet:after:content-['']",
        )}
      >
        {mark}
      </button>
      {text}
    </span>
  );
}

/** A choice an option editor lists. */
export interface EditorOption {
  readonly id: string;
  readonly label: string;
  readonly color?: string;
  readonly initials?: string;
}

/** Popover widths of the three option editors, as drawn. */
const OPTION_POP_WIDTH = { select: "w-62.5", multi: "w-65", person: "w-67.5" };

/**
 * Options in a popover (Select, Multi select, Person). A single choice saves
 * and closes; a multiple one saves each tick and closes on Enter or "Xong".
 * Arrow keys move through the list, Escape or leaving closes without a write.
 * Select and Person start with a search box; Multi select only past seven.
 */
export function OptionEditor({
  field,
  options,
  selected,
  multi,
  label,
  copy,
  onCommit,
  children,
}: {
  field: FieldDefinition;
  options: readonly EditorOption[];
  selected: readonly string[];
  multi: boolean;
  label: string;
  copy: FieldCopy;
  onCommit: (value: unknown) => void;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const root = useRef<HTMLDivElement>(null);
  const isPerson = field.fieldType === "person";
  const searchable = !multi || isPerson || options.length > 7;
  const shown = options.filter((option) =>
    option.label.toLowerCase().includes(query.trim().toLowerCase()),
  );
  const current = selected.join(", ");
  const pick = (id: string) => {
    if (!multi) {
      setOpen(false);
      if (selected[0] !== id) onCommit(id);
      return;
    }
    onCommit(
      selected.includes(id)
        ? selected.filter((entry) => entry !== id)
        : [...selected, id],
    );
  };
  const nav = (event: KeyboardEvent<HTMLDivElement>) => {
    const items = Array.from(
      root.current?.querySelectorAll<HTMLElement>("[data-nav]") ?? [],
    );
    const index = items.indexOf(document.activeElement as HTMLElement);
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      const step = event.key === "ArrowDown" ? 1 : -1;
      items[Math.min(items.length - 1, Math.max(0, index + step))]?.focus();
    } else if (event.key === "Enter" && multi) {
      event.preventDefault();
      setOpen(false);
    } else if (
      event.key === "Enter" &&
      (event.target as HTMLElement).tagName === "INPUT"
    ) {
      event.preventDefault();
      if (shown[0]) pick(shown[0].id);
    }
  };
  const row =
    "flex min-h-8 w-full cursor-pointer items-center gap-2 rounded-[7px] border-0 bg-transparent px-2 text-left text-compact text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring";
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setQuery("");
        setOpen(next);
      }}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={current ? `${label}: ${current}` : label}
          aria-haspopup="listbox"
          aria-expanded={open}
          className={cn(EDIT_TRIGGER_CLASS, "max-w-full flex-1")}
        >
          {children}
        </button>
      </PopoverTrigger>
      <PopoverContent
        {...POP_PLACE}
        aria-label={field.name}
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          const target =
            root.current?.querySelector<HTMLElement>(
              "[aria-selected=true], [data-nav]",
            ) ?? null;
          target?.focus();
        }}
        className={cn(
          POP_BOX,
          OPTION_POP_WIDTH[isPerson ? "person" : multi ? "multi" : "select"],
        )}
      >
        <div ref={root} onKeyDown={nav} className="flex flex-col gap-1.5">
          {searchable ? (
            <label className="flex h-7.5 items-center gap-1.5 rounded-[6px] border border-line-strong bg-background px-2 text-compact focus-within:ring-2 focus-within:ring-focus-ring">
              <Search
                className="size-4 flex-none text-meta-foreground"
                aria-hidden="true"
              />
              <input
                data-nav=""
                type="search"
                value={query}
                placeholder={copy.search}
                aria-label={fillTableCopy(copy.searchLabel, {
                  field: field.name,
                })}
                onChange={(event) => setQuery(event.target.value)}
                className="min-w-0 flex-1 border-0 bg-transparent p-0 text-compact text-foreground outline-none placeholder:text-meta-foreground"
              />
            </label>
          ) : null}
          <div
            role="listbox"
            aria-label={field.name}
            aria-multiselectable={multi || undefined}
            className="flex max-h-64 flex-col gap-1.5 overflow-auto"
          >
            {shown.map((option) => {
              const on = selected.includes(option.id);
              return (
                <button
                  key={option.id}
                  data-nav=""
                  type="button"
                  role="option"
                  aria-selected={on}
                  onClick={() => pick(option.id)}
                  className={cn(row, !multi && on && "bg-state-selected")}
                >
                  {multi ? (
                    <span
                      aria-hidden="true"
                      className={cn(
                        CHECK_BOX,
                        on
                          ? "border-primary bg-primary text-primary-foreground"
                          : "border-line-strong",
                      )}
                    >
                      {on ? (
                        <Check className="size-2.75" strokeWidth={3} />
                      ) : null}
                    </span>
                  ) : null}
                  {option.initials ? (
                    <>
                      <PersonMark option={option} />
                      <span className="min-w-0 flex-1 truncate">
                        {option.label}
                      </span>
                    </>
                  ) : (
                    <span className="flex min-w-0 flex-1">
                      <OptionChip option={option} />
                    </span>
                  )}
                  {!multi && on ? (
                    <Check
                      className="size-4 flex-none text-state-selected-foreground"
                      aria-hidden="true"
                    />
                  ) : null}
                </button>
              );
            })}
            {shown.length === 0 ? (
              <p className="m-0 px-2 py-1.5 text-meta-foreground">
                {copy.noOptions}
              </p>
            ) : null}
          </div>
          <div role="separator" className={POP_RULE} />
          {multi && !isPerson ? (
            <div className="flex justify-end">
              <button
                data-nav=""
                type="button"
                onClick={() => setOpen(false)}
                className={POP_PRIMARY}
              >
                {copy.done}
              </button>
            </div>
          ) : (
            <button
              data-nav=""
              type="button"
              disabled={selected.length === 0}
              onClick={() => {
                if (!multi) setOpen(false);
                onCommit(multi ? [] : null);
              }}
              className={cn(
                row,
                "text-meta-foreground disabled:cursor-default disabled:opacity-50",
              )}
            >
              <X className="size-4 flex-none" aria-hidden="true" />
              {isPerson ? copy.clearPeople : copy.clearValue}
            </button>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}

const DAY_MS = 86_400_000;

/** A month of Monday-first weeks, 30 px rows of 32 px days (`WM-Field-Editors`); the arrow keys move the day, Page Up and Page Down the month. */
function MonthGrid({
  copy,
  picked,
  onPick,
}: {
  copy: MainTableCopy;
  picked: string;
  onPick: (day: string) => void;
}) {
  const t = copy.duePicker;
  const months = Object.values(t.months);
  const today = dayOf(new Date());
  const start = picked || today;
  const [shown, setShown] = useState({
    year: Number(start.slice(0, 4)),
    month: Number(start.slice(5, 7)),
  });
  const first = new Date(shown.year, shown.month - 1, 1);
  const lead = (first.getDay() + 6) % 7;
  const cells = Array.from({ length: 42 }, (_, index) => {
    const date = new Date(shown.year, shown.month - 1, 1 - lead + index);
    return { date, key: dayOf(date) };
  });
  // Six weeks only when the month needs them (the artboard draws five).
  const weeks = cells[35]?.date.getMonth() === shown.month - 1 ? 6 : 5;
  const go = (by: number) =>
    setShown(({ year, month }) => {
      const next = new Date(year, month - 1 + by, 1);
      return { year: next.getFullYear(), month: next.getMonth() + 1 };
    });
  const move = (event: KeyboardEvent<HTMLElement>, key: string) => {
    const at = new Date(`${key}T00:00:00`);
    const by: Record<string, number> = {
      ArrowLeft: -1,
      ArrowRight: 1,
      ArrowUp: -7,
      ArrowDown: 7,
    };
    if (event.key === "PageUp" || event.key === "PageDown") {
      event.preventDefault();
      go(event.key === "PageUp" ? -1 : 1);
    } else if (by[event.key] !== undefined) {
      event.preventDefault();
      const next = new Date(at.getTime() + by[event.key]! * DAY_MS);
      setShown({ year: next.getFullYear(), month: next.getMonth() + 1 });
      requestAnimationFrame(() =>
        document
          .querySelector<HTMLElement>(`[data-day="${dayOf(next)}"]`)
          ?.focus(),
      );
    }
  };
  const nav =
    "grid h-6.5 w-8.5 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent p-0 text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring";
  return (
    <>
      <div className="flex items-center justify-between">
        <button
          type="button"
          aria-label={t.prevMonth}
          onClick={() => go(-1)}
          className={nav}
        >
          <ChevronLeft className="size-4" aria-hidden="true" />
        </button>
        <b aria-live="polite" className="text-compact font-bold">
          {fillTableCopy(t.month, {
            month: months[shown.month - 1] ?? "",
            year: shown.year,
          })}
        </b>
        <button
          type="button"
          aria-label={t.nextMonth}
          onClick={() => go(1)}
          className={nav}
        >
          <ChevronRight className="size-4" aria-hidden="true" />
        </button>
      </div>
      <div
        role="grid"
        aria-label={fillTableCopy(t.month, {
          month: months[shown.month - 1] ?? "",
          year: shown.year,
        })}
        className="grid grid-cols-[repeat(7,2rem)] justify-center gap-0.5"
      >
        {Object.values(t.weekdays).map((name) => (
          <span
            key={name}
            role="columnheader"
            className="grid h-7.5 place-items-center text-micro font-semibold text-meta-foreground"
          >
            {name}
          </span>
        ))}
        {cells.slice(0, weeks * 7).map(({ date, key }) => {
          const inMonth = date.getMonth() === shown.month - 1;
          const on = key === picked;
          return (
            <button
              key={key}
              type="button"
              role="gridcell"
              data-day={key}
              aria-selected={on}
              aria-current={key === today ? "date" : undefined}
              aria-label={`${date.getDate()} ${months[date.getMonth()] ?? ""} ${date.getFullYear()}`}
              tabIndex={on || (!picked && key === today) ? 0 : -1}
              onKeyDown={(event) => move(event, key)}
              onClick={() => onPick(key)}
              className={cn(
                "grid h-7.5 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent p-0 text-crumb tabular-nums max-xl:h-11 hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
                !inMonth && "text-meta-foreground opacity-60",
                key === today && "shadow-[inset_0_0_0_1.5px_var(--primary)]",
                on &&
                  "bg-primary font-semibold text-primary-foreground hover:bg-primary",
              )}
            >
              {date.getDate()}
            </button>
          );
        })}
      </div>
    </>
  );
}

/** A day picked from a calendar popover; picking saves and closes. */
export function DateEditor({
  value,
  text,
  label,
  copy,
  tableCopy,
  onCommit,
  children,
}: {
  /** "YYYY-MM-DD" or empty. */
  value: string;
  text: string;
  label: string;
  copy: FieldCopy;
  tableCopy: MainTableCopy;
  onCommit: (value: string | null) => void;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const picked = /^\d{4}-\d{2}-\d{2}/.test(value) ? value.slice(0, 10) : "";
  const pick = (day: string | null) => {
    setOpen(false);
    if ((day ?? "") !== picked) onCommit(day);
  };
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={text ? `${label}: ${text}` : label}
          aria-haspopup="dialog"
          className={cn(EDIT_TRIGGER_CLASS, "max-w-full flex-1")}
        >
          {children}
        </button>
      </PopoverTrigger>
      <PopoverContent
        {...POP_PLACE}
        aria-label={label}
        className={cn(POP_BOX, "w-67.5")}
      >
        <MonthGrid copy={tableCopy} picked={picked} onPick={pick} />
        <div role="separator" className={POP_RULE} />
        <div className="flex justify-between">
          <button
            type="button"
            disabled={!picked}
            onClick={() => pick(null)}
            className={cn(
              POP_TEXT,
              "disabled:cursor-default disabled:opacity-50",
            )}
          >
            {copy.clearDate}
          </button>
          <button
            type="button"
            onClick={() => pick(dayOf(new Date()))}
            className={POP_PLAIN}
          >
            {copy.today}
          </button>
        </div>
      </PopoverContent>
    </Popover>
  );
}
