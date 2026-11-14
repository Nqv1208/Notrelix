import { cn } from "@notrelix/ui-web";
import {
  kanbanColumnTabletWidthClass,
  kanbanColumnWidthClass,
} from "./kanban-layout";

/** Lane tints of the four skeleton lanes, as the status categories draw them. */
const LANE_TINTS = [
  "bg-work-status-todo-lane",
  "bg-work-status-doing-lane",
  "bg-work-status-review-lane",
  "bg-work-status-done-lane",
] as const;

/** A grey bar of a skeleton: the border colour, as the artboard draws it. */
function Bar({ className }: { className: string }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "block rounded-sm bg-border motion-safe:animate-pulse",
        className,
      )}
    />
  );
}

/**
 * The lanes of a Kanban that is still loading: four lanes of a header bar and two card shapes, under the board
 * header, tabs and toolbar that stay on screen (BC-Kanban frame `kb-L`).
 */
export function KanbanSkeleton() {
  return (
    <div
      className="flex min-h-0 flex-1 gap-3 overflow-hidden px-4 pt-1 pb-6 sm:px-6"
      aria-busy="true"
      aria-label="Loading Kanban board"
      role="status"
      data-kanban-skeleton=""
    >
      {LANE_TINTS.map((tint) => (
        <div
          key={tint}
          data-skeleton-lane=""
          className={cn(
            "flex h-fit shrink-0 flex-col gap-2 rounded-xl p-2",
            kanbanColumnWidthClass,
            kanbanColumnTabletWidthClass,
            tint,
          )}
        >
          <div className="flex h-8 items-center pl-1.5">
            <Bar className="h-3 w-[90px]" />
          </div>
          {[0, 1].map((card) => (
            <div
              key={card}
              data-skeleton-card=""
              className="flex flex-col gap-2 rounded-xl bg-card px-3 pt-[11px] pb-2.5 shadow-xs"
            >
              <Bar className="h-2.5 w-[50px]" />
              <Bar className="h-3 w-[85%]" />
              <Bar className="h-2.5 w-[40%]" />
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}
