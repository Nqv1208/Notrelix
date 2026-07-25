import { createContext, useContext, useMemo, type ReactNode } from 'react';
import { useAppRuntime } from '@notrelix/runtime-web';
import { createUseWorkspaceShellData, type WorkspaceSummary, type WorkspaceView } from '@notrelix/features-workspace';

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
  const { api: runtimeClient } = useAppRuntime();

  /**
   * Create the workspace shell hook using the injected API client from AppRuntime.
   * `runtimeClient` is a stable reference (created once at app startup), so this
   * useMemo effectively runs only once — equivalent to module-level factory calls
   * but without relying on a global mutable singleton.
   */
  const useWorkspaceShellData = useMemo(
    () =>
      createUseWorkspaceShellData({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        options: { mockMode: false },
      }),
    [runtimeClient],
  );

  const { workspace, views, isLoading, isError, refetch } = useWorkspaceShellData(workspaceId);

  const value = useMemo<WorkspaceContextValue>(
    () => ({
      workspaceId,
      workspace: workspace ?? null,
      views,
      isLoading,
      isError,
      refetch,
    }),
    [workspaceId, workspace, views, isLoading, isError, refetch],
  );

  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}
