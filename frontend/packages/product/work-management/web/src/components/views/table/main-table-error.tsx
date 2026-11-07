import { Plus, RefreshCw, Search, TriangleAlert } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import {
  kanbanPillClass,
  kanbanPrimaryButtonClass,
} from "../kanban/kanban-chrome";
import { BoardStateBlock } from "../../board-layout/board-state-block";
import { mainTableCopy, type MainTableCopy } from "./main-table.copy";

/** The board could not be loaded (BC-Shell Er): what is wrong, that nothing is lost, and "Thử lại". */
export function MainTableError({
  copy = mainTableCopy("vi"),
  message,
  onRetry,
}: {
  copy?: MainTableCopy;
  /** Replaces the default description. */
  message?: string;
  /** Loads the board again; absent = no button. */
  onRetry?: () => void;
}) {
  const t = copy.toolbar;
  return (
    <div data-slot="main-table-unavailable">
      {/* The toolbar stays where it is drawn, switched off: nothing on it can act on a board that did not load. */}
      <div
        data-table-toolbar=""
        aria-hidden="true"
        className="flex items-center gap-1.5 px-4 py-2.5 max-sm:hidden sm:px-6"
      >
        <span className="flex h-8 w-50 items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-compact text-meta-foreground dark:bg-card">
          <Search className="size-4 shrink-0" />
          {t.search.placeholder}
        </span>
        {[t.filter.trigger, t.sort.label, t.groupBy.label].map((label) => (
          <Button
            key={label}
            variant="ghost"
            size="sm"
            disabled
            tabIndex={-1}
            className={kanbanPillClass}
          >
            {label}
          </Button>
        ))}
        <span className="flex-1" />
        <Button
          size="sm"
          disabled
          tabIndex={-1}
          className={kanbanPrimaryButtonClass}
        >
          <Plus className="size-4" />
          {t.addItem}
        </Button>
      </div>
      <BoardStateBlock
        role="alert"
        className="py-9"
        icon={<TriangleAlert />}
        title={copy.unavailable.title}
        description={message ?? copy.unavailable.description}
        action={
          onRetry ? (
            <Button
              size="sm"
              className={cn(
                kanbanPrimaryButtonClass,
                "max-sm:h-11 max-sm:px-3.5 max-sm:text-[14px]",
              )}
              onClick={onRetry}
            >
              <RefreshCw className="size-4" aria-hidden="true" />
              {copy.unavailable.retry}
            </Button>
          ) : undefined
        }
      />
    </div>
  );
}
