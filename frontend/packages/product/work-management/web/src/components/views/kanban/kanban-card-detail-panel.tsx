import { Sheet, SheetContent, SheetTitle } from "@notrelix/ui-web";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import { TaskDetailPanelSurface } from "../../card-detail/task-detail-panel-surface";
import {
  defaultTaskDetailCapabilities,
  type TaskDetailCallbacks,
  type TaskDetailData,
} from "../../card-detail/task-detail-types";

interface KanbanCardDetailPanelProps {
  board: Board;
  /** The selected card; the sheet stays closed while this is null. */
  card: CardDetail | null;
  detailData: TaskDetailData;
  callbacks: TaskDetailCallbacks;
}

/**
 * Sheet around the shared task detail surface. Pure: the container resolves
 * the card, its detail data and the mutations, and decides when to pass a
 * card at all (an unknown item id passes null and nothing renders).
 */
export function KanbanCardDetailPanel({
  board,
  card,
  detailData,
  callbacks,
}: KanbanCardDetailPanelProps) {
  if (!card) return null;

  return (
    <Sheet
      open
      onOpenChange={(open) => {
        if (!open) callbacks.onClose();
      }}
    >
      <SheetContent className="w-full gap-0 p-0 sm:max-w-2xl overflow-y-auto">
        <SheetTitle className="sr-only">{card.title}</SheetTitle>
        <TaskDetailPanelSurface
          status="ready"
          board={board}
          card={card}
          capabilities={defaultTaskDetailCapabilities}
          detailData={detailData}
          callbacks={callbacks}
        />
      </SheetContent>
    </Sheet>
  );
}
