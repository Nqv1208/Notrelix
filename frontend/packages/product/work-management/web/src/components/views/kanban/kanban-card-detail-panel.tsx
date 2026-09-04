import { useEffect } from "react";
import { Sheet, SheetContent } from "@notrelix/ui-web";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import { TaskDetailPanelSurface } from "../../card-detail/task-detail-panel-surface";
import { defaultTaskDetailCapabilities } from "../../card-detail/task-detail-types";

interface KanbanCardDetailPanelProps {
  board: Board;
  card: CardDetail | null;
  workspaceId: string;
  onClose: () => void;
}

export function KanbanCardDetailPanel({
  board,
  card,
  workspaceId: _workspaceId,
  onClose,
}: KanbanCardDetailPanelProps) {
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", handleEscape);
    return () => document.removeEventListener("keydown", handleEscape);
  }, [onClose]);

  if (!card) return null;

  return (
    <Sheet open={!!card} onOpenChange={(open) => !open && onClose()}>
      <SheetContent className="w-full sm:max-w-2xl overflow-y-auto">
        <TaskDetailPanelSurface
          status="ready"
          board={board}
          card={card}
          capabilities={defaultTaskDetailCapabilities}
          detailData={{
            updates: card.updates,
            updatesLoading: false,
            files: card.files,
            filesLoading: false,
            activity: card.activity,
            activityLoading: false,
            activityFetching: false,
          }}
          callbacks={{
            onClose,
            onRenameTitle: () => undefined,
            onToggleWatch: () => undefined,
            onDuplicate: () => undefined,
            onArchive: () => undefined,
            onUpdateFieldValue: () => undefined,
            onRefreshActivity: () => undefined,
            onCreateUpdate: (_input, options) => options?.onSuccess?.(),
            onUpdateUpdate: () => undefined,
            onDeleteUpdate: () => undefined,
            onSelectTab: () => undefined,
          }}
        />
      </SheetContent>
    </Sheet>
  );
}
