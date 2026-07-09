import { createContext, useContext, ReactNode } from 'react';
import { createUseWorkspaceShellData, type WorkspaceSummary, type WorkspaceView } from '@notrelix/features-workspace';
import { api, endpoints } from '@notrelix/contracts';

const useWorkspaceShellData = createUseWorkspaceShellData({ api, endpoints });

interface WorkspaceContextType {
  workspaceId: string;
  workspace: WorkspaceSummary | null;
  views: WorkspaceView[];
  isLoading: boolean;
  isError: boolean;
  refetch: () => Promise<void>;
}

const WorkspaceContext = createContext<WorkspaceContextType | undefined>(undefined);

export function useWorkspaceContext() {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error('useWorkspaceContext must be used within a WorkspaceProvider');
  }
  return context;
}

interface WorkspaceProviderProps {
  workspaceId: string;
  children: ReactNode;
}

export function WorkspaceProvider({ workspaceId, children }: WorkspaceProviderProps) {
  const { workspace, views, isLoading, isError, refetch } = useWorkspaceShellData(workspaceId);

  return (
    <WorkspaceContext.Provider
      value={{
        workspaceId,
        workspace: workspace || null,
        views,
        isLoading,
        isError,
        refetch,
      }}
    >
      {children}
    </WorkspaceContext.Provider>
  );
}
