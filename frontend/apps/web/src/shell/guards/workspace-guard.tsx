import { type ReactNode, useMemo } from 'react';
import { Navigate, useParams } from '@tanstack/react-router';
import { useAppRuntime } from '@notrelix/runtime-web';
import { createUseWorkspaceList } from '@notrelix/features-workspace';
import { LoadingState } from '@notrelix/ui-web';

interface WorkspaceGuardProps {
  workspaceId?: string;
  children: ReactNode;
}

export function WorkspaceGuard({ workspaceId: propWorkspaceId, children }: WorkspaceGuardProps) {
  const { workspaceId: paramWorkspaceId } = useParams({ strict: false });
  const workspaceId = propWorkspaceId ?? paramWorkspaceId;

  const { api: runtimeClient } = useAppRuntime();

  /**
   * Create the workspace list hook using the injected API client.
   * `runtimeClient` is stable for the app lifetime, so this only runs once.
   */
  const useWorkspaceList = useMemo(
    () =>
      createUseWorkspaceList({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        options: { mockMode: false },
      }),
    [runtimeClient],
  );

  const { data: workspaces = [], isLoading } = useWorkspaceList();

  if (isLoading) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-background">
        <LoadingState title="Loading" description="Verifying workspace access..." />
      </div>
    );
  }

  const hasAccess = workspaces.some((ws) => ws.id === workspaceId);

  if (!hasAccess) {
    return <Navigate to="/home" replace />;
  }

  return <>{children}</>;
}
