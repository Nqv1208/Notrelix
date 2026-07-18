import { useParams, useSearch, useNavigate } from '@tanstack/react-router';
import { BoardWorkspaceViewContent } from '@notrelix/work-management-web';
import { BoardLayoutShell } from '@notrelix/work-management-web';
import { useFullBoard } from '@notrelix/work-management-state';

export function BoardPage() {
  const { workspaceId, boardId } = useParams({
    from: '/workspaces/$workspaceId/boards/$boardId',
  });
  const navigate = useNavigate();

  const search = useSearch({
    strict: false,
  }) as { view?: string };

  const viewType = search.view || 'kanban';
  const view = { type: viewType, name: viewType.toUpperCase() };

  const { board } = useFullBoard(boardId, workspaceId);

  const handleViewChange = (newView: string) => {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    navigate({ search: { view: newView } } as any);
  };

  return (
    <BoardLayoutShell
      workspaceId={workspaceId}
      boardId={boardId}
      boardTitle={board?.title ?? 'Board'}
      activeView={viewType}
      onViewChange={handleViewChange}
    >
      <BoardWorkspaceViewContent
        workspaceId={workspaceId}
        boardId={boardId}
        view={view}
      />
    </BoardLayoutShell>
  );
}
