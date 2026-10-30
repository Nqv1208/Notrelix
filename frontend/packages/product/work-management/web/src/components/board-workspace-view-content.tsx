import { useFullBoard } from "@notrelix/work-management-state";
import { BoardViewerContext } from "./shared/board-viewer";
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
  /** Opens an item's own page from the Kanban sheet. */
  onOpenItemPage?: (itemId: string) => void;
  /** Switches the board to another view by type (the Timeline's undated link). */
  onViewChange?: (view: string) => void;
  /**
   * The signed-in member, read by the host from its session: the views offer
   * "Giao cho tôi" with it. Absent: the shortcut is not offered.
   */
  currentUserId?: string | null;
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

export function BoardWorkspaceViewContent(props: BoardScreenProps) {
  return (
    <BoardViewerContext.Provider value={props.currentUserId ?? null}>
      <BoardViewSwitch {...props} />
    </BoardViewerContext.Provider>
  );
}

function BoardViewSwitch({
  workspaceId,
  boardId,
  view,
  onOpenItemPage,
  onViewChange,
  currentUserId,
  ...kanbanParams
}: BoardScreenProps) {
  if (view.type === "table")
    return (
      <MainTableView
        boardId={boardId}
        workspaceId={workspaceId}
        currentUserId={currentUserId}
        {...kanbanParams}
      />
    );
  if (view.type === "kanban")
    return (
      <BoardFullDataView
        workspaceId={workspaceId}
        boardId={boardId}
        mode="kanban"
        kanbanParams={kanbanParams}
        currentUserId={currentUserId}
        onOpenItemPage={onOpenItemPage}
      />
    );
  if (view.type === "calendar")
    return (
      <BoardFullDataView
        workspaceId={workspaceId}
        boardId={boardId}
        mode="calendar"
        kanbanParams={kanbanParams}
        currentUserId={currentUserId}
      />
    );
  if (view.type === "timeline")
    return (
      <BoardFullDataView
        workspaceId={workspaceId}
        boardId={boardId}
        mode="timeline"
        kanbanParams={kanbanParams}
        currentUserId={currentUserId}
        onOpenCalendar={
          onViewChange ? () => onViewChange("calendar") : undefined
        }
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
  currentUserId,
  onOpenItemPage,
  onOpenCalendar,
}: {
  workspaceId: string;
  boardId: string;
  mode: "kanban" | "calendar" | "timeline";
  kanbanParams?: KanbanParams;
  currentUserId?: string | null;
  onOpenItemPage?: (itemId: string) => void;
  onOpenCalendar?: () => void;
}) {
  const { board, isLoading, error } = useFullBoard(boardId, workspaceId);

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
      flush
      viewContent={
        mode === "kanban" ? (
          <KanbanView
            boardId={board.id}
            workspaceId={workspaceId}
            {...kanbanParams}
            currentUserId={currentUserId}
            onOpenItemPage={onOpenItemPage}
          />
        ) : mode === "calendar" ? (
          <BoardCalendarView
            boardId={board.id}
            workspaceId={workspaceId}
            selectedItemId={kanbanParams?.selectedItemId}
            onSelectedItemChange={kanbanParams?.onSelectedItemChange}
            filter={kanbanParams?.filter}
            onFilterChange={kanbanParams?.onFilterChange}
            currentUserId={currentUserId}
          />
        ) : (
          <BoardTimelineView
            boardId={board.id}
            workspaceId={workspaceId}
            selectedItemId={kanbanParams?.selectedItemId}
            onSelectedItemChange={kanbanParams?.onSelectedItemChange}
            filter={kanbanParams?.filter}
            onFilterChange={kanbanParams?.onFilterChange}
            currentUserId={currentUserId}
            onOpenCalendar={onOpenCalendar}
          />
        )
      }
    />
  );
}

export type { BoardWorkspaceSurfaceStatus };
