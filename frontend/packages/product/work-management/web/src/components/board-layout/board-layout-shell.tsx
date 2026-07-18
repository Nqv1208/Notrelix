import { BoardToolbar } from './board-toolbar';

interface BoardLayoutShellProps {
  workspaceId: string;
  boardId: string;
  boardTitle: string;
  activeView: string;
  onViewChange: (viewType: string) => void;
  children: React.ReactNode;
}

export function BoardLayoutShell({
  workspaceId,
  boardId,
  boardTitle,
  activeView,
  onViewChange,
  children,
}: BoardLayoutShellProps) {
  return (
    <div className="h-full flex flex-col bg-background">
      <BoardToolbar
        boardTitle={boardTitle}
        activeView={activeView}
        onViewChange={onViewChange}
      />
      <div className="flex-1 min-h-0 overflow-hidden">
        {children}
      </div>
    </div>
  );
}
