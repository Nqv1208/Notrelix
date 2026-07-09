import { Outlet, useParams } from '@tanstack/react-router';
import { AuthGuard } from '@/shell/guards/auth-guard';
import { WorkspaceGuard } from '@/shell/guards/workspace-guard';
import { WorkspaceProvider } from '@/providers/workspace-provider';
import { WorkspaceSidebar } from '@/shell/sidebar/sidebar';
import { WorkspaceTopbar } from '@/shell/topbar/topbar';

export function WorkspaceLayout() {
  const { workspaceId } = useParams({ from: '/workspaces/$workspaceId' });

  return (
    <AuthGuard>
      <WorkspaceProvider workspaceId={workspaceId}>
        <WorkspaceGuard>
          <div className="flex h-screen w-screen overflow-hidden bg-background">
            <WorkspaceSidebar />
            <div className="flex-1 flex flex-col min-w-0">
              <WorkspaceTopbar />
              <main className="flex-1 overflow-y-auto min-h-0">
                <Outlet />
              </main>
            </div>
          </div>
        </WorkspaceGuard>
      </WorkspaceProvider>
    </AuthGuard>
  );
}
