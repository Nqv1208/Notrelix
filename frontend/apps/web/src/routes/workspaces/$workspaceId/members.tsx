import { useParams } from '@tanstack/react-router';

export function MembersPage() {
  const { workspaceId } = useParams({ from: '/workspaces/$workspaceId' });

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-4">Members</h1>
      <p className="text-muted-foreground">
        Workspace: {workspaceId}
      </p>
    </div>
  );
}
