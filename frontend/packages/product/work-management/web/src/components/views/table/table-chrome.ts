import type { WorkStatusCategory } from "@notrelix/work-management-core";

/**
 * Class strings the Board table repeats. Literal strings so Tailwind
 * generates them. The table sits on the white panel tier: pure white in
 * light, the panel tier in dark.
 */
export const tablePanelClass = "bg-popover dark:bg-card";

/** Status pill and group header tint by Status category. */
export const STATUS_TINT_CLASS: Readonly<Record<WorkStatusCategory, string>> = {
  todo: "bg-status-chip-todo",
  in_progress: "bg-status-chip-doing",
  review: "bg-status-chip-review",
  done: "bg-status-chip-done",
};

/** The 8 px status dot: an outline for "to do", filled for the others. */
export const STATUS_DOT_CLASS: Readonly<Record<WorkStatusCategory, string>> = {
  todo: "border-2 border-work-status-todo",
  in_progress: "bg-work-status-doing",
  review: "bg-status-dot-review",
  done: "bg-work-status-done",
};

/**
 * Accent of a group (the left edge of its rows): the colour the person gave
 * the option, else the colour of its Status category, else the neutral rule.
 */
export function groupAccent(group: {
  readonly color: string | null;
  readonly category: WorkStatusCategory | null;
}): string {
  const own = group.color?.trim();
  if (own) return own;
  switch (group.category) {
    case "todo":
      return "var(--work-status-todo)";
    case "in_progress":
      return "var(--work-status-doing)";
    case "review":
      return "var(--work-status-review)";
    case "done":
      return "var(--work-status-done)";
    default:
      return "var(--line-strong)";
  }
}

export const NEUTRAL_DOT_CLASS = "bg-muted-foreground";

/**
 * The buttons of a table read in pages ("Hiện thêm", "Thử lại", "Lên đầu"): 44 px tall, as the Large Board
 * artboard draws them, on the panel tier with the strong rule.
 */
export const pageButtonClass =
  "h-11 gap-1.5 rounded-[7px] border-line-strong bg-popover px-3.5 text-[13px] font-medium text-foreground dark:bg-card hover:bg-state-hover-strong";
