import { Outlet, useParams } from "@tanstack/react-router";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { WorkspaceProvider } from "@/providers/workspace-provider";
import { WorkspaceScopeBoundary } from "@/shell/workspace/workspace-scope-boundary";
import { WorkspaceShell } from "@/shell/workspace/workspace-shell";

/**
 * Workspace layout: one shell (sidebar, topbar, palette) and one scroll
 * container for every workspace route (WX-SHELL-001…005). `key` resets the
 * shell's local state when the workspace changes; server state is already
 * scoped by workspace and cleared on switch by the route's `beforeLoad`.
 */
export function WorkspaceLayout() {
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId" });

  return (
    <AuthGuard>
      <WorkspaceProvider key={workspaceId} workspaceId={workspaceId}>
        <WorkspaceScopeBoundary>
          <WorkspaceShell>
            <Outlet />
          </WorkspaceShell>
        </WorkspaceScopeBoundary>
      </WorkspaceProvider>
    </AuthGuard>
  );
}
