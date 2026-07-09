import { Outlet, useParams } from '@tanstack/react-router';

export function WorkspaceLayout() {
  const { workspaceId } = useParams({ from: '/workspaces/$workspaceId' });

  return (
    <div className="min-h-screen flex">
      <aside className="w-64 border-r bg-muted/30 p-4">
        <h2 className="font-semibold mb-4">Workspace</h2>
        <p className="text-sm text-muted-foreground">ID: {workspaceId}</p>
      </aside>
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  );
}
