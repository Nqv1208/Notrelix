import type {
  Board,
  CardDetail,
  CardDetailTab,
} from "@notrelix/work-management-core";
import { AlertCircle } from "lucide-react";
import { Button, Skeleton } from "@notrelix/ui-web";
import { TaskDetailHeader } from "./task-detail-header";
import { TaskDetailTabs } from "./task-detail-tabs";
import type {
  TaskDetailCallbacks,
  TaskDetailCapabilities,
  TaskDetailData,
} from "./task-detail-types";

export type TaskDetailPanelStatus = "loading" | "error" | "ready";

export function TaskDetailPanelSurface({
  status,
  board,
  card,
  capabilities,
  detailData,
  callbacks,
  activeTab,
}: {
  status: TaskDetailPanelStatus;
  board: Board;
  card: CardDetail | null;
  capabilities: TaskDetailCapabilities;
  detailData?: TaskDetailData;
  callbacks: TaskDetailCallbacks;
  activeTab?: CardDetailTab;
}) {
  return (
    <div
      className="flex h-full min-h-0 w-full min-w-0 flex-col overflow-hidden bg-popover"
      aria-label="Task detail panel"
      tabIndex={-1}
      onKeyDown={(event) => {
        if (event.key === "Escape") callbacks.onClose();
      }}
    >
      {status === "loading" ? <TaskDetailPanelSkeleton /> : null}
      {status === "error" ? (
        <TaskDetailPanelError onClose={callbacks.onClose} />
      ) : null}
      {status === "ready" && card && detailData ? (
        <>
          <TaskDetailHeader
            key={card.id}
            board={board}
            card={card}
            capabilities={capabilities}
            onClose={callbacks.onClose}
            onRenameTitle={callbacks.onRenameTitle}
            onToggleWatch={callbacks.onToggleWatch}
            onDuplicate={callbacks.onDuplicate}
            onArchive={callbacks.onArchive}
            onUpdateFieldValue={callbacks.onUpdateFieldValue}
          />
          <TaskDetailTabs
            card={card}
            data={detailData}
            capabilities={capabilities}
            callbacks={callbacks}
            activeTab={activeTab}
          />
        </>
      ) : null}
    </div>
  );
}

export function TaskDetailPanelSkeleton() {
  return (
    <div
      className="flex h-full w-full flex-col gap-4 bg-popover p-4"
      aria-label="Loading task details"
      aria-busy="true"
      role="status"
    >
      <Skeleton className="h-10 rounded-lg" />
      <Skeleton className="h-20 rounded-lg" />
      <Skeleton className="h-9 rounded-lg" />
      <Skeleton className="h-40 rounded-lg" />
    </div>
  );
}

export function TaskDetailPanelError({ onClose }: { onClose: () => void }) {
  return (
    <div className="flex h-full w-full flex-col items-center justify-center bg-popover p-6 text-center">
      <AlertCircle className="mb-3 size-8 text-destructive" />
      <h2 className="text-sm font-semibold text-foreground">
        Task unavailable
      </h2>
      <p className="mt-2 max-w-xs text-sm text-muted-foreground">
        This task could not be loaded or no longer exists.
      </p>
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="mt-4 bg-card"
        onClick={onClose}
      >
        Close panel
      </Button>
    </div>
  );
}
