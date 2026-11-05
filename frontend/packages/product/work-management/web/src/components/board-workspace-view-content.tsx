import { useFullBoard } from "@notrelix/work-management-state";
import { BoardViewerContext } from "./shared/board-viewer";
import { MainTableView } from "./views/table/main-table-view";
import { BoardCalendarView } from "./views/calendar/board-calendar-view";
import { KanbanView } from "./views/kanban/kanban-view";
import { MainTableError } from "./views/table/main-table-error";
import { useMainTableCopy } from "./views/table/use-main-table-copy";
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
  /** Opens an item of any board (the toast after a move, the item that blocks a move); absent: only this board's items open, through `onSelectedItemChange`. */
  onOpenItem?: (boardId: string, itemId: string) => void;
  /** The route `filter` parameter (Kanban). */
  filter?: string;
  onFilterChange?: (
    value: string | undefined,
    options?: { replace?: boolean },
  ) => void;
  /** The route `groupBy` parameter: a field id (Kanban). */
  groupBy?: string;
  onGroupByChange?: (fieldId: string) => void;
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
  | "onOpenItem"
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
  onOpenCalendar,
}: {
  workspaceId: string;
  boardId: string;
  mode: "kanban" | "calendar" | "timeline";
  kanbanParams?: KanbanParams;
  currentUserId?: string | null;
  onOpenCalendar?: () => void;
}) {
  const { board, isLoading, error, refetch } = useFullBoard(
    boardId,
    workspaceId,
  );
  const tableCopy = useMainTableCopy();

  // The Kanban keeps its toolbar while it loads and draws its own lane skeletons.
  if (isLoading && mode !== "kanban")
    return <BoardWorkspaceSurface status="loading" skeletonRows={6} />;
  // A board that did not load (not a refused or vanished one, whose answer stays the generic NotFound) is drawn
  // as the table draws it: the switched-off toolbar, what is wrong, and "Thử lại".
  const refused = (error as { kind?: string } | null)?.kind;
  if (
    mode === "kanban" &&
    error &&
    refused !== "not_found" &&
    refused !== "forbidden"
  )
    return <MainTableError copy={tableCopy} onRetry={() => void refetch()} />;
  if (!isLoading && (error || !board))
    return <BoardWorkspaceSurface status="error" error={error} />;

  return (
    <BoardWorkspaceSurface
      status="ready"
      flush
      viewContent={
        mode === "kanban" ? (
          <KanbanView
            boardId={boardId}
            workspaceId={workspaceId}
            {...kanbanParams}
            currentUserId={currentUserId}
          />
        ) : mode === "calendar" ? (
          <BoardCalendarView
            boardId={boardId}
            workspaceId={workspaceId}
            selectedItemId={kanbanParams?.selectedItemId}
            onSelectedItemChange={kanbanParams?.onSelectedItemChange}
            onOpenItem={kanbanParams?.onOpenItem}
            filter={kanbanParams?.filter}
            onFilterChange={kanbanParams?.onFilterChange}
            currentUserId={currentUserId}
          />
        ) : (
          <BoardTimelineView
            boardId={boardId}
            workspaceId={workspaceId}
            selectedItemId={kanbanParams?.selectedItemId}
            onSelectedItemChange={kanbanParams?.onSelectedItemChange}
            onOpenItem={kanbanParams?.onOpenItem}
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
