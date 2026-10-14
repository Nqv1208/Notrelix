import { Check, Flag } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { initialsFor, type MarkToneId } from "../../core/model/navigation";
import type { MyWorkDue } from "../../core/model/my-work";
import { MARK_TONE_CLASS } from "../shell/shell-models";
import {
  pluralForm,
  type WorkPriority,
  type WorkStatusBucket,
} from "./home-models";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

/**
 * Parts the rows of Home and My work share: the department mark, the Status
 * pill, the Priority flag, the tick, and the words of a due moment. They draw
 * what they are given and never invent a Status or a Priority.
 */

/** The department (Space) mark: 20 px, or 16 px small (Final-Home `.dm`). */
export function DepartmentMark({
  name,
  markTone,
  small = false,
  className,
}: {
  name: string;
  markTone: MarkToneId;
  small?: boolean;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid shrink-0 place-items-center font-bold",
        small
          ? "size-4 rounded-[4px] text-avatar-xs"
          : "size-5 rounded-md text-mark-xs",
        MARK_TONE_CLASS[markTone],
        className,
      )}
    >
      {initialsFor(name)}
    </span>
  );
}

const DOT_CLASS: Record<WorkStatusBucket, string> = {
  todo: "shadow-[inset_0_0_0_2px_var(--color-work-status-todo)]",
  doing: "bg-work-status-doing",
  review: "bg-work-status-review",
  done: "bg-work-status-done",
};

const PILL_CLASS: Record<WorkStatusBucket, string> = {
  todo: "bg-status-chip-todo",
  doing: "bg-status-chip-doing",
  review: "bg-status-chip-review",
  done: "bg-status-chip-done",
};

/** The 8 px dot of a Status bucket (Cần làm is a ring). */
export function StatusDot({
  bucket,
  className,
}: {
  bucket: WorkStatusBucket;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "inline-block size-2 shrink-0 rounded-full",
        DOT_CLASS[bucket],
        className,
      )}
    />
  );
}

export const STATUS_PILL_CLASS = PILL_CLASS;

const PRIORITY_CLASS: Record<WorkPriority, string> = {
  urgent: "text-danger-text",
  high: "text-priority-high-text",
  medium: "text-meta-foreground",
  low: "text-meta-foreground",
};

/** The 14 px flag of Home: urgent in danger, high in orange, the rest quiet. */
export function PriorityFlag({
  priority,
  label,
}: {
  priority: WorkPriority;
  label: string;
}) {
  return (
    <span
      role="img"
      aria-label={label}
      title={label}
      className={cn(
        "grid w-3.5 shrink-0 place-items-center",
        PRIORITY_CLASS[priority],
      )}
    >
      <Flag aria-hidden="true" className="size-3.5" strokeWidth={1.8} />
    </span>
  );
}

export const PRIORITY_TEXT_CLASS = PRIORITY_CLASS;

/** The round tick of a row: completes the Item, or says why it cannot. */
export function TickButton({
  label,
  title,
  busy = false,
  disabled = false,
  onClick,
}: {
  label: string;
  title?: string;
  busy?: boolean;
  /** Looks as in the design but has no handler (`aria-disabled`). */
  disabled?: boolean;
  onClick?: () => void;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      title={title ?? label}
      aria-disabled={disabled || busy ? true : undefined}
      aria-busy={busy ? true : undefined}
      onClick={disabled || busy ? undefined : onClick}
      className={cn(
        "group/tick grid size-5 shrink-0 place-items-center rounded-full border-[1.5px] border-line-strong bg-transparent p-0 text-transparent outline-none",
        "focus-visible:ring-2 focus-visible:ring-focus-ring",
        disabled
          ? "cursor-not-allowed border-dashed"
          : "cursor-pointer hover:border-work-status-done hover:text-work-status-done",
        busy && "opacity-60",
      )}
    >
      <Check aria-hidden="true" className="size-3" strokeWidth={3} />
    </button>
  );
}

const WEEKDAY_KEYS = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"] as const;

/** Words for a due moment: "Quá hạn 2 ngày", "Hôm nay", "17:00", "Thứ Tư 14/10". */
export function dueWords(
  copy: WorkspaceHomeCopy,
  due: MyWorkDue,
  options: { timeOnToday: boolean },
): { text: string; tone: "over" | "today" | "normal" } {
  switch (due.kind) {
    case "none":
      return { text: copy.work.due.none, tone: "normal" };
    case "overdue":
      return {
        text: pluralForm(
          {
            one: copy.work.due.overdue_one,
            other: copy.work.due.overdue_other,
          },
          due.daysOverdue,
        ),
        tone: "over",
      };
    case "today":
      return {
        text: options.timeOnToday && due.time ? due.time : copy.work.due.today,
        tone: "today",
      };
    case "upcoming":
      return {
        text: `${copy.work.weekdays[WEEKDAY_KEYS[due.weekday] ?? "sun"]} ${due.shortDate}`,
        tone: "normal",
      };
  }
}
