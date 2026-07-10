import { type ReactNode } from 'react';
import { Navigate, useParams } from '@tanstack/react-router';
import { createUseWorkspaceList } from '@notrelix/features-workspace';
import { env } from '@/config/env';
import { api, endpoints } from '@notrelix/contracts';
import { LoadingState } from '@notrelix/ui-web';

const useWorkspaceList = createUseWorkspaceList({
  api,
  endpoints,
  options: { mockMode: env.mockApi },
});

interface WorkspaceGuardProps {
  workspaceId?: string;
  children: ReactNode;
}

export function WorkspaceGuard({ workspaceId: propWorkspaceId, children }: WorkspaceGuardProps) {
  const { workspaceId: paramWorkspaceId } = useParams({ strict: false });
  const workspaceId = propWorkspaceId ?? paramWorkspaceId;
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
