import {
  useEffect,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { AlertCircle, Check } from "lucide-react";
import {
  Calendar,
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
import { dayOf, EDIT_TRIGGER_CLASS } from "./table-cell-editors";
import { TableCheckbox } from "./table-parts";

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
  children,
}: {
  note: ReactNode;
  tone: "danger" | "warning";
  focusRef?: React.RefObject<HTMLElement | null>;
  onEscape?: () => void;
  className?: string;
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
        align="start"
        side="bottom"
        sideOffset={2}
        collisionPadding={8}
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          focusRef?.current?.focus();
        }}
        onCloseAutoFocus={(event) => event.preventDefault()}
        onInteractOutside={(event) => event.preventDefault()}
        onEscapeKeyDown={onEscape}
        className={cn(
          NOTE_CLASS,
          tone === "danger" ? "border-danger-text" : "border-warning-text",
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

/** A line of text, a number or a link typed in the cell: Enter saves, Escape puts the old value back, leaving the cell saves. */
export function InlineInputEditor({
  field,
  value,
  text,
  label,
  copy,
  alignEnd = false,
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
  onCommit: (value: unknown) => void;
  children: ReactNode;
}) {
  const [editing, setEditing] = useState(false);
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
      if (!sameValue(field, parsed.value, value)) onCommit(parsed.value);
    } else {
      settled.current = true;
    }
    refocus.current = restoreFocus;
    setError(null);
    setEditing(false);
  };

  if (!editing)
    return (
      <button
        ref={trigger}
        type="button"
        aria-label={text ? `${label}: ${text}` : label}
        title={text || undefined}
        onClick={() => {
          settled.current = false;
          setDraft(text);
          setEditing(true);
        }}
        className={cn(EDIT_TRIGGER_CLASS, "max-w-full flex-1")}
      >
        {children}
      </button>
    );
  return (
    <AnchoredNote
      tone="danger"
      note={
        error ? (
          <p
            role="alert"
            className="m-0 flex items-center gap-1 text-crumb font-semibold text-danger-text"
          >
            <AlertCircle className="size-3.5" aria-hidden="true" />
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
          "h-7 min-w-0 flex-1 rounded-[6px] border border-line-strong bg-popover px-2 text-nav text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card",
          alignEnd && "text-right tabular-nums",
          error && "border-danger-text",
        )}
      />
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
  onCommit: (value: string | null) => void;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState(text);
  const cancelled = useRef(false);
  const finish = (save: boolean) => {
    cancelled.current = !save;
    if (save && draft !== text) onCommit(draft.trim() === "" ? null : draft);
    setOpen(false);
  };
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (next) {
          cancelled.current = false;
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
        align="start"
        side="bottom"
        sideOffset={2}
        aria-label={label}
        onEscapeKeyDown={() => {
          cancelled.current = true;
        }}
        className="w-80 gap-2 rounded-[10px] border border-line-strong bg-popover p-2.5 shadow-pop ring-0 dark:bg-card"
      >
        <textarea
          autoFocus
          rows={5}
          value={draft}
          aria-label={label}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter" && !event.shiftKey) {
              event.preventDefault();
              finish(true);
            }
          }}
          className="w-full resize-none rounded-[7px] border border-line-strong bg-background px-2 py-1.5 text-nav text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
        />
        <div className="flex justify-end gap-1.5">
          <button
            type="button"
            onClick={() => finish(false)}
            className="h-7 cursor-pointer rounded-md border-0 bg-transparent px-2.5 text-compact text-muted-foreground hover:bg-state-hover"
          >
            {copy.cancel}
          </button>
          <button
            type="button"
            onClick={() => finish(true)}
            className="h-7 cursor-pointer rounded-md border-0 bg-primary px-2.5 text-compact font-semibold text-primary-foreground"
          >
            {copy.save}
          </button>
        </div>
      </PopoverContent>
    </Popover>
  );
}

/** The one-tap box: Space or a click flips it and saves at once. */
export function CheckboxEditor({
  checked,
  label,
  editable,
  onCommit,
}: {
  checked: boolean;
  label: string;
  editable: boolean;
  onCommit: (value: boolean) => void;
}) {
  if (!editable)
    return (
      <span
        role="img"
        aria-label={label}
        className={cn(
          "grid size-3.75 shrink-0 place-items-center rounded-[4px] border-[1.5px]",
          checked
            ? "border-primary bg-primary text-primary-foreground"
            : "border-line-strong bg-popover dark:bg-card",
        )}
      >
        {checked ? <Check className="size-2.75" strokeWidth={3} /> : null}
      </span>
    );
  return (
    // A real button toggles on Enter and click; Space is taken on key release so one press is one toggle in every browser.
    <span
      role="presentation"
      onKeyUp={(event) => {
        if (event.key !== " ") return;
        event.preventDefault();
        onCommit(!checked);
      }}
    >
      <TableCheckbox checked={checked} label={label} onChange={onCommit} />
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

/**
 * Options in a popover (Select, Multi select, Person). A single choice saves
 * and closes; a multiple one saves each tick and closes on Enter or "Xong".
 * Arrow keys move through the list, Escape or leaving closes without a write.
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
  const searchable = options.length > 7;
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
        align="start"
        side="bottom"
        sideOffset={2}
        aria-label={field.name}
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          const target =
            root.current?.querySelector<HTMLElement>(
              "[aria-selected=true], [data-nav]",
            ) ?? null;
          target?.focus();
        }}
        className="w-64 gap-1 rounded-[10px] border border-line-strong bg-popover p-1.5 text-compact shadow-pop ring-0 dark:bg-card"
      >
        <div ref={root} onKeyDown={nav} className="flex flex-col gap-1">
          {searchable ? (
            <input
              data-nav=""
              type="search"
              value={query}
              placeholder={copy.search}
              aria-label={fillTableCopy(copy.searchLabel, {
                field: field.name,
              })}
              onChange={(event) => setQuery(event.target.value)}
              className="h-7 w-full rounded-md border border-line-strong bg-background px-2 text-compact text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
            />
          ) : null}
          <div
            role="listbox"
            aria-label={field.name}
            aria-multiselectable={multi || undefined}
            className="flex max-h-64 flex-col gap-0.5 overflow-auto"
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
                  className={cn(
                    "flex h-8 w-full cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent px-2 text-left text-compact text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
                    on && "bg-state-hover font-semibold",
                  )}
                >
                  {multi ? (
                    <span
                      aria-hidden="true"
                      className={cn(
                        "grid size-3.75 shrink-0 place-items-center rounded-[4px] border-[1.5px] text-micro leading-none",
                        on
                          ? "border-primary bg-primary text-primary-foreground"
                          : "border-line-strong",
                      )}
                    >
                      {on ? "✓" : ""}
                    </span>
                  ) : null}
                  {option.initials ? (
                    <span
                      aria-hidden="true"
                      className="flex size-6 shrink-0 items-center justify-center rounded-full bg-foreground text-avatar-md font-bold text-background"
                    >
                      {option.initials}
                    </span>
                  ) : option.color ? (
                    <span
                      aria-hidden="true"
                      className="size-2 shrink-0 rounded-full"
                      style={{ backgroundColor: option.color }}
                    />
                  ) : null}
                  <span className="min-w-0 flex-1 truncate">
                    {option.label}
                  </span>
                  {!multi && on ? <span aria-hidden="true">✓</span> : null}
                </button>
              );
            })}
            {shown.length === 0 ? (
              <p className="m-0 px-2 py-1.5 text-meta-foreground">
                {copy.noOptions}
              </p>
            ) : null}
          </div>
          <div className="flex items-center justify-between gap-2 border-t border-border pt-1">
            <button
              data-nav=""
              type="button"
              disabled={selected.length === 0}
              onClick={() => {
                if (!multi) setOpen(false);
                onCommit(multi ? [] : null);
              }}
              className="h-7 cursor-pointer rounded-md border-0 bg-transparent px-2 text-compact text-muted-foreground hover:bg-state-hover-strong disabled:cursor-default disabled:opacity-50"
            >
              {field.fieldType === "person"
                ? copy.clearPeople
                : copy.clearValue}
            </button>
            {multi ? (
              <button
                data-nav=""
                type="button"
                onClick={() => setOpen(false)}
                className="h-7 cursor-pointer rounded-md border-0 bg-primary px-2.5 text-compact font-semibold text-primary-foreground"
              >
                {copy.done}
              </button>
            ) : null}
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}

/** A day picked from a calendar popover; picking saves and closes. */
export function DateEditor({
  value,
  text,
  label,
  copy,
  onCommit,
  children,
}: {
  /** "YYYY-MM-DD" or empty. */
  value: string;
  text: string;
  label: string;
  copy: FieldCopy;
  onCommit: (value: string | null) => void;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const picked = /^\d{4}-\d{2}-\d{2}/.test(value)
    ? new Date(`${value.slice(0, 10)}T00:00:00`)
    : undefined;
  const pick = (day: string | null) => {
    setOpen(false);
    if ((day ?? "") !== value.slice(0, 10)) onCommit(day);
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
        align="start"
        side="bottom"
        sideOffset={2}
        aria-label={label}
        className="w-auto gap-0 p-0"
      >
        <Calendar
          mode="single"
          selected={picked}
          defaultMonth={picked}
          onSelect={(date: Date | undefined) => pick(date ? dayOf(date) : null)}
        />
        <div className="flex justify-between border-t border-border p-2">
          <button
            type="button"
            disabled={!picked}
            onClick={() => pick(null)}
            className="h-8 cursor-pointer rounded-[7px] border-0 bg-transparent px-3 text-compact font-medium text-danger-text hover:bg-state-hover-strong disabled:cursor-default disabled:opacity-50"
          >
            {copy.clearDate}
          </button>
          <button
            type="button"
            onClick={() => pick(dayOf(new Date()))}
            className="h-8 cursor-pointer rounded-[7px] border border-line-strong bg-transparent px-3 text-compact font-medium hover:bg-state-hover-strong"
          >
            {copy.today}
          </button>
        </div>
      </PopoverContent>
    </Popover>
  );
}
