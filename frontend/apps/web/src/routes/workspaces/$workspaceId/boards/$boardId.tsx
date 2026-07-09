import { useParams } from '@tanstack/react-router';

export function BoardPage() {
  const { workspaceId, boardId } = useParams({
    from: '/workspaces/$workspaceId/boards/$boardId',
  });

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-4">Board</h1>
      <p className="text-muted-foreground">
        Workspace: {workspaceId}, Board: {boardId}
      </p>
    </div>
  );
}
