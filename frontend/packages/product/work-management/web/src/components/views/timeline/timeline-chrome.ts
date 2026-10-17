import { cn } from "@notrelix/ui-web";
import { kanbanOutlineButtonClass } from "../kanban/kanban-chrome";
import type { TimelineTone } from "./timeline-model";

/**
 * The Timeline's 32 px outline button (Hôm nay). On the dark panel the
 * mockup keeps the button on the panel tier with the strong line, where the
 * shared outline variant would tint it.
 */
export const timelineOutlineButtonClass = cn(
  kanbanOutlineButtonClass,
  "dark:border-line-strong dark:bg-card dark:hover:bg-state-hover-strong",
);

/** Previous and next: 32 px squares on the panel tier. */
export const timelineIconButtonClass =
  "size-8 rounded-[7px] border-line-strong bg-popover p-0 text-muted-foreground shadow-none hover:bg-state-hover-strong dark:border-line-strong dark:bg-card dark:hover:bg-state-hover-strong";

/** The panel tier the chart and list sit on: white in light, the panel in dark. */
export const timelinePanelClass = "bg-popover dark:bg-card";

/** Bar fill by Status bucket (the Cần làm bar also carries a border). */
export const BAR_TONE_CLASS: Readonly<Record<TimelineTone, string>> = {
  todo: "border border-timeline-bar-todo-border bg-timeline-bar-todo",
  doing: "bg-timeline-bar-doing",
  review: "bg-timeline-bar-review",
  done: "bg-timeline-bar-done text-muted-foreground",
};

/** The 3 px progress strip along the foot of a bar. */
export const PROGRESS_TONE_CLASS: Readonly<Record<TimelineTone, string>> = {
  todo: "",
  doing: "bg-timeline-progress-doing",
  review: "bg-timeline-progress-review",
  done: "bg-timeline-progress-done",
};
