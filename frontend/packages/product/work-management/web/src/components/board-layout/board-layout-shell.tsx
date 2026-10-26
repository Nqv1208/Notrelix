import { BoardToolbar } from "./board-toolbar";
import type { ViewTabSavedView } from "./view-tabs";

interface BoardLayoutShellProps {
  workspaceId: string;
  boardId: string;
  boardTitle: string;
  activeView: string;
  onViewChange: (viewKey: string) => void;
  savedViews?: readonly ViewTabSavedView[];
  onAddView?: (viewType: string) => void;
  /** Opens the active view's settings; absent: Settings stays disabled. */
  onOpenSettings?: () => void;
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
  onOpenSettings,
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
        onOpenSettings={onOpenSettings}
      />
      <div className="flex-1 min-h-0 overflow-hidden">{children}</div>
    </div>
  );
}
