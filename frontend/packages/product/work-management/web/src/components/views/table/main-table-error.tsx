import { RefreshCw, TriangleAlert } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import { kanbanPrimaryButtonClass } from "../kanban/kanban-chrome";
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
  return (
    <div data-slot="main-table-unavailable">
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
