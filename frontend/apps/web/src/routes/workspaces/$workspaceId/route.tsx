import { Outlet, useParams, useLocation } from '@tanstack/react-router';
import { AuthGuard } from '@/shell/guards/auth-guard';
import { WorkspaceGuard } from '@/shell/guards/workspace-guard';
import { WorkspaceProvider } from '@/providers/workspace-provider';
import { WorkspaceSidebar } from '@/shell/sidebar/sidebar';
import { WorkspaceTopbar } from '@/shell/topbar/topbar';
import { WorkspaceTabbedRouteFrame } from '@/shell/workspace-tabbed-frame';

const CONTENT_ROUTES = ['/', '/dashboard', '/boards', '/docs'];

function isContentRoute(pathname: string, workspaceId: string): boolean {
  const path = pathname.replace(`/workspaces/${workspaceId}`, '') || '/';
  return CONTENT_ROUTES.some((route) => path === route || path.startsWith(route + '/'));
}

export function WorkspaceLayout() {
  const { workspaceId } = useParams({ from: '/workspaces/$workspaceId' });
  const location = useLocation();
  const useTabbedFrame = isContentRoute(location.pathname, workspaceId);

  return (
    <AuthGuard>
      <WorkspaceProvider workspaceId={workspaceId}>
        <WorkspaceGuard workspaceId={workspaceId}>
          <div className="flex h-screen w-screen overflow-hidden bg-background">
            <WorkspaceSidebar />
            <div className="flex-1 flex flex-col min-w-0">
              {!useTabbedFrame && <WorkspaceTopbar />}
              <main className="flex-1 overflow-y-auto min-h-0">
                {useTabbedFrame ? (
                  <WorkspaceTabbedRouteFrame>
                    <Outlet />
                  </WorkspaceTabbedRouteFrame>
                ) : (
                  <Outlet />
                )}
              </main>
            </div>
          </div>
        </WorkspaceGuard>
      </WorkspaceProvider>
    </AuthGuard>
  );
}
