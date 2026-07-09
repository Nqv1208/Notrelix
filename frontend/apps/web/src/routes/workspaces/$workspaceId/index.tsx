import { useParams } from '@tanstack/react-router';

export function WorkspaceHomePage() {
  const { workspaceId } = useParams({ from: '/workspaces/$workspaceId' });

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-4">Workspace Home</h1>
      <p className="text-muted-foreground">
        Welcome to workspace {workspaceId}
      </p>
    </div>
  );
}
