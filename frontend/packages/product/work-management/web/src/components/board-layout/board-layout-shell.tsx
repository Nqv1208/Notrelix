import { BoardToolbar } from "./board-toolbar";
import type { ViewTabSavedView } from "./view-tabs";
import type { ViewTabActions } from "./view-tab-menu";

interface BoardLayoutShellProps {
  workspaceId: string;
  boardId: string;
  boardTitle: string;
  activeView: string;
  onViewChange: (viewKey: string) => void;
  savedViews?: readonly ViewTabSavedView[];
  onAddView?: (viewType: string) => void;
  viewActions?: ViewTabActions;
  /** Opens the active view's settings; absent: Settings stays disabled. */
  onOpenSettings?: () => void;
  /** Opens the board's deleted items; absent: no entry in the menu. */
  onOpenTrash?: () => void;
  /** Opens the board's archived items; absent: no entry in the menu. */
  onOpenArchive?: () => void;
  /** A notice between the title row and the view tabs. */
  notice?: React.ReactNode;
  /** A band between the view tabs and the view (the opened view is gone). */
  banner?: React.ReactNode;
  children: React.ReactNode;
}

export function BoardLayoutShell({
  workspaceId: _workspaceId,
  boardId: _boardId,
  boardTitle,
  activeView,
  onViewChange,
  savedViews,
  onAddView,
  viewActions,
  onOpenSettings,
  onOpenTrash,
  onOpenArchive,
  notice,
  banner,
  children,
}: BoardLayoutShellProps) {
  return (
    <div className="h-full min-w-0 flex flex-col bg-background">
      <BoardToolbar
        boardTitle={boardTitle}
        activeView={activeView}
        onViewChange={onViewChange}
        savedViews={savedViews}
        onAddView={onAddView}
        viewActions={viewActions}
        onOpenSettings={onOpenSettings}
        onOpenTrash={onOpenTrash}
        onOpenArchive={onOpenArchive}
        notice={notice}
      />
      {banner ? (
        <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
          {banner}
          <div className="min-h-0 flex-1 overflow-hidden">{children}</div>
        </div>
      ) : (
        <div className="flex-1 min-h-0 overflow-hidden">{children}</div>
      )}
    </div>
  );
}
