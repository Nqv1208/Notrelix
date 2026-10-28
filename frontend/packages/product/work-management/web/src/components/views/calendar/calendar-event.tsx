import type { DragEvent, KeyboardEvent, MouseEvent } from "react";
import { Ban } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import {
  PRIORITY_TONE_CLASS,
  PriorityGlyph,
} from "../../shared/priority-glyph";
import type { CalendarCopy } from "./calendar.copy";
import type { CalendarEntry, CalendarTone } from "./calendar-model";

/** Literal class strings so Tailwind generates every one of them. */
const TONE_CLASS: Readonly<Record<CalendarTone, string>> = {
  todo: "border-line-strong bg-calendar-event-todo",
  doing: "bg-calendar-event-doing",
  review: "bg-calendar-event-review",
  done: "bg-calendar-event-done text-meta-foreground",
  overdue: "bg-calendar-event-overdue font-semibold text-danger-text",
};

const DOT_CLASS: Readonly<Record<CalendarTone, string>> = {
  todo: "shadow-[inset_0_0_0_2px_var(--work-status-todo)]",
  doing: "bg-work-status-doing",
  review: "bg-calendar-dot-review",
  done: "bg-work-status-done",
  overdue: "bg-danger-text",
};

/** The colour dot (or the milestone diamond) at the start of an entry. */
export function CalendarDot({
  tone,
  milestone = false,
  className,
}: {
  tone: CalendarTone;
  milestone?: boolean;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "size-1.75 flex-none rounded-full",
        milestone ? "rotate-45 rounded-[1px] bg-mark-crimson" : DOT_CLASS[tone],
        className,
      )}
    />
  );
}

/** The "Giai đoạn" / "Mốc" tag of a phase or milestone. */
export function CalendarKindTag({ children }: { children: string }) {
  return (
    <span className="flex-none rounded-[4px] border border-line-strong bg-popover px-1 text-rail-label leading-4 font-semibold text-muted-foreground dark:bg-card">
      {children}
    </span>
  );
}

/** The assignee's initials in an 18px mark. */
export function CalendarMark({
  assignee,
}: {
  assignee: NonNullable<CalendarEntry["assignee"]>;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid size-4.5 flex-none place-items-center rounded-full text-avatar-xs font-bold",
        assignee.markClass,
      )}
    >
      {assignee.initials}
    </span>
  );
}

export function entryText(entry: CalendarEntry): string {
  return entry.itemKey ? `${entry.itemKey} ${entry.title}` : entry.title;
}

export function entryLabel(entry: CalendarEntry): string {
  return entry.note ? `${entryText(entry)}, ${entry.note}` : entryText(entry);
}

export interface CalendarEventProps {
  entry: CalendarEntry;
  copy: CalendarCopy;
  /** Grid position in the week (1-based column, span, row under the numbers). */
  col: number;
  span: number;
  row: number;
  /** Week view: the title wraps and the chip grows with it. */
  wrap?: boolean;
  /** Being dragged: the chip stays in place, dimmed. */
  lifting?: boolean;
  canDrag: boolean;
  onOpen: (id: string) => void;
  onDragStart: (entry: CalendarEntry, event: DragEvent<HTMLElement>) => void;
  onDragEnd: () => void;
  /** Alt and arrow keys: due date moved by `days`. */
  onShift: (entry: CalendarEntry, days: number) => void;
}

const ARROW_DAYS: Readonly<Record<string, number>> = {
  ArrowLeft: -1,
  ArrowRight: 1,
  ArrowUp: -7,
  ArrowDown: 7,
};

/**
 * One entry in the week grid. A board item is a button that opens it, can be
 * dragged to another day and moves with Alt and the arrow keys. A phase or a
 * milestone is a plain marker.
 */
export function CalendarEvent({
  entry,
  copy,
  col,
  span,
  row,
  wrap = false,
  lifting = false,
  canDrag,
  onOpen,
  onDragStart,
  onDragEnd,
  onShift,
}: CalendarEventProps) {
  const text = entryText(entry);
  const style = { gridColumn: `${col} / span ${span}`, gridRow: row + 2 };
  const base = cn(
    "mx-1 flex min-w-0 items-center gap-1.5 overflow-hidden rounded-[6px] border border-transparent px-1.5 text-xs leading-[1.45] font-medium whitespace-nowrap text-foreground",
    wrap ? "h-auto min-h-6.5 items-start py-1 whitespace-normal" : "h-6",
    TONE_CLASS[entry.tone],
    (entry.kind !== "item" || entry.tone === "overdue") && "font-semibold",
    lifting && "opacity-40",
  );
  const dot = (
    <CalendarDot
      tone={entry.tone}
      milestone={entry.kind === "milestone"}
      className={wrap ? "mt-[5px]" : undefined}
    />
  );
  const label = (
    <span
      className={cn(
        "min-w-0 flex-1",
        wrap ? "leading-[1.35] whitespace-normal" : "truncate",
        entry.tone === "done" && "line-through",
      )}
    >
      {text}
    </span>
  );

  if (entry.kind !== "item") {
    const milestone = entry.kind === "milestone";
    return (
      <div
        data-calendar-marker={entry.kind}
        className={cn(
          base,
          milestone && "border-dashed border-line-strong bg-transparent",
        )}
        style={style}
        title={text}
      >
        {milestone ? <CalendarDot tone={entry.tone} milestone /> : dot}
        <CalendarKindTag>
          {milestone ? copy.kind.milestone : copy.kind.phase}
        </CalendarKindTag>
        {label}
      </div>
    );
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    const days = ARROW_DAYS[event.key];
    if (!event.altKey || days === undefined || !entry.movable || !canDrag) {
      return;
    }
    event.preventDefault();
    onShift(entry, days);
  };

  return (
    <button
      type="button"
      data-calendar-event={entry.id}
      draggable={entry.movable && canDrag}
      className={cn(
        base,
        "cursor-pointer text-left hover:shadow-[inset_0_0_0_1px_var(--line-strong)] focus-visible:outline-2 focus-visible:outline-ring",
      )}
      style={style}
      title={text}
      aria-label={[
        entryLabel(entry),
        entry.priority ? copy.priority[entry.priority] : null,
        entry.openBlockerCount
          ? copy.blockedBy.replace("{{count}}", String(entry.openBlockerCount))
          : null,
      ]
        .filter(Boolean)
        .join(", ")}
      onClick={(event: MouseEvent) => {
        event.stopPropagation();
        onOpen(entry.id);
      }}
      onKeyDown={handleKeyDown}
      onDragStart={(event) => onDragStart(entry, event)}
      onDragEnd={onDragEnd}
    >
      {dot}
      {label}
      {entry.openBlockerCount ? (
        <Ban
          data-blocked={entry.openBlockerCount}
          aria-hidden="true"
          className="size-3.25 flex-none text-danger-text"
        />
      ) : null}
      {entry.priority ? (
        <span
          data-priority={entry.priority}
          aria-hidden="true"
          className={cn("flex-none", PRIORITY_TONE_CLASS[entry.priority])}
        >
          <PriorityGlyph level={entry.priority} />
        </span>
      ) : null}
      {entry.assignee ? <CalendarMark assignee={entry.assignee} /> : null}
    </button>
  );
}

/** The chip that shows where a dragged entry would land. */
export function CalendarGhost({
  text,
  col,
  row,
  assignee,
}: {
  text: string;
  col: number;
  row: number;
  assignee?: CalendarEntry["assignee"];
}) {
  return (
    <div
      data-calendar-ghost=""
      aria-hidden="true"
      className="mx-1 flex h-6 min-w-0 items-center gap-1.5 overflow-hidden rounded-[6px] border-[1.5px] border-dashed border-primary bg-popover px-1.5 text-xs leading-[1.45] font-medium whitespace-nowrap text-brand-ink shadow-popover dark:bg-card"
      style={{ gridColumn: `${col} / span 1`, gridRow: row + 2 }}
    >
      <CalendarDot tone="doing" />
      <span className="min-w-0 flex-1 truncate">{text}</span>
      {assignee ? <CalendarMark assignee={assignee} /> : null}
    </div>
  );
}
