import { createContext, useContext, ReactNode, useMemo } from 'react';
import { createUseWorkspaceShellData, type WorkspaceSummary, type WorkspaceView } from '@notrelix/features-workspace';
import { env } from '@/config/env';
import { api, endpoints } from '@notrelix/contracts';

const useWorkspaceShellData = createUseWorkspaceShellData({
  api,
  endpoints,
  options: { mockMode: env.mockApi },
});

type WorkspaceContextValue = {
  workspaceId: string;
  workspace: WorkspaceSummary | null;
  views: WorkspaceView[];
  isLoading: boolean;
  isError: boolean;
  refetch: () => Promise<unknown>;
};

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

export function useWorkspaceContext() {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error('useWorkspaceContext must be used within a WorkspaceProvider');
  }
  return context;
}

type WorkspaceProviderProps = {
  workspaceId: string;
  children: ReactNode;
};

export function WorkspaceProvider({ workspaceId, children }: WorkspaceProviderProps) {
  const { workspace, views, isLoading, isError, refetch } = useWorkspaceShellData(workspaceId);

  const value = useMemo<WorkspaceContextValue>(
    () => ({
      workspaceId,
      workspace: workspace || null,
      views,
      isLoading,
      isError,
      refetch,
    }),
    [workspaceId, workspace, views, isLoading, isError, refetch]
  );

  return (
    <WorkspaceContext.Provider value={value}>
      {children}
    </WorkspaceContext.Provider>
  );
}
