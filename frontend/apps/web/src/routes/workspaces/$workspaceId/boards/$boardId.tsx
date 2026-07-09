import { useParams, useSearch } from '@tanstack/react-router';
import { BoardWorkspaceViewContent } from '@notrelix/work-management-web';

export function BoardPage() {
  const { workspaceId, boardId } = useParams({
    from: '/workspaces/$workspaceId/boards/$boardId',
  });

  const search = useSearch({
    strict: false,
  }) as { view?: string };

  const viewType = search.view || 'kanban';
  const view = { type: viewType, name: viewType.toUpperCase() };

  return (
    <div className="h-full flex flex-col bg-background">
      <BoardWorkspaceViewContent
        workspaceId={workspaceId}
        boardId={boardId}
        view={view}
      />
    </div>
  );
}
