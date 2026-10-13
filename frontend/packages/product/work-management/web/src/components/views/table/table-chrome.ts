import type { WorkStatusCategory } from "@notrelix/work-management-core";

/**
 * Class strings the Board table repeats. Literal strings so Tailwind
 * generates them. The table sits on the white panel tier: pure white in
 * light, the panel tier in dark.
 */
export const tablePanelClass = "bg-popover dark:bg-card";

/** Keyboard key chip (footer, status menu). */
export const tableKeyClass =
  "rounded-[4px] border border-line-strong bg-keycap px-[5px] font-mono text-key text-muted-foreground";

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

export const NEUTRAL_DOT_CLASS = "bg-muted-foreground";
