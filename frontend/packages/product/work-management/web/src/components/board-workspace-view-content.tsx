import { useFullBoard } from "@notrelix/work-management-state";
import { MainTableView } from "./views/table/main-table-view";
import { BoardCalendarView } from "./views/calendar/board-calendar-view";
import { KanbanView } from "./views/kanban/kanban-view";
import { BoardTimelineView } from "./views/timeline/board-timeline-view";
import {
  BoardWorkspaceSurface,
  type BoardWorkspaceSurfaceStatus,
} from "./board-workspace-surface";

type WorkspaceView = { type: string; name?: string };

export interface BoardScreenProps {
  workspaceId: string;
  boardId: string;
  view: WorkspaceView;
  /** Card id from the `item` search parameter (Kanban opens its detail). */
  selectedItemId?: string;
  onSelectedItemChange?: (
    id: string | undefined,
    options?: { replace?: boolean },
  ) => void;
  /** The route `filter` parameter (Kanban). */
  filter?: string;
  onFilterChange?: (
    value: string | undefined,
    options?: { replace?: boolean },
  ) => void;
  /** The route `groupBy` parameter: a field id (Kanban). */
  groupBy?: string;
  onGroupByChange?: (fieldId: string) => void;
}

type KanbanParams = Pick<
  BoardScreenProps,
  | "selectedItemId"
  | "onSelectedItemChange"
  | "filter"
  | "onFilterChange"
  | "groupBy"
  | "onGroupByChange"
>;

export function BoardScreen(props: BoardScreenProps) {
  return <BoardWorkspaceViewContent {...props} />;
}

export function BoardWorkspaceViewContent({
  workspaceId,
  boardId,
  view,
  ...kanbanParams
}: BoardScreenProps) {
  if (view.type === "table")
    return <MainTableView boardId={boardId} workspaceId={workspaceId} />;
  if (view.type === "kanban")
    return (
      <BoardFullDataView
        workspaceId={workspaceId}
        boardId={boardId}
        mode="kanban"
        kanbanParams={kanbanParams}
      />
    );
  if (view.type === "calendar")
    return (
      <BoardFullDataView
        workspaceId={workspaceId}
        boardId={boardId}
        mode="calendar"
      />
    );
  if (view.type === "timeline")
    return (
      <BoardFullDataView
        workspaceId={workspaceId}
        boardId={boardId}
        mode="timeline"
      />
    );
  return (
    <BoardWorkspaceSurface
      status="unsupported"
      unsupportedViewName={view.name ?? view.type}
    />
  );
}

function BoardFullDataView({
  workspaceId,
  boardId,
  mode,
  kanbanParams,
}: {
  workspaceId: string;
  boardId: string;
  mode: "kanban" | "calendar" | "timeline";
  kanbanParams?: KanbanParams;
}) {
  const { board, groups, isLoading, error } = useFullBoard(
    boardId,
    workspaceId,
  );

  if (isLoading)
    return (
      <BoardWorkspaceSurface
        status="loading"
        skeletonRows={mode === "kanban" ? 4 : 6}
      />
    );
  if (error || !board)
    return <BoardWorkspaceSurface status="error" error={error} />;

  return (
    <BoardWorkspaceSurface
      status="ready"
      flush={mode === "kanban"}
      viewContent={
        mode === "kanban" ? (
          <KanbanView
            boardId={board.id}
            workspaceId={workspaceId}
            {...kanbanParams}
          />
        ) : mode === "calendar" ? (
          <BoardCalendarView groups={groups} />
        ) : (
          <BoardTimelineView groups={groups} />
        )
      }
    />
  );
}

export type { BoardWorkspaceSurfaceStatus };
