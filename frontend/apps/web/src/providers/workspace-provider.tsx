import { createContext, useContext, useMemo, type ReactNode } from "react";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  createUseWorkspace,
  type WorkspaceSummary,
} from "@notrelix/features-workspace";

type WorkspaceContextValue = {
  workspaceId: string;
  workspace: WorkspaceSummary | null;
  isLoading: boolean;
  isError: boolean;
  /** The failed load, when `isError` (an AppError from the API client). */
  error: unknown;
  refetch: () => Promise<unknown>;
};

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

export function useWorkspaceContext() {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error(
      "useWorkspaceContext must be used within a WorkspaceProvider",
    );
  }
  return context;
}

type WorkspaceProviderProps = {
  workspaceId: string;
  children: ReactNode;
};

/**
 * Workspace scope for the shell. Holds the workspace record only: the
 * mock-only workspace views left the shell (WX-DEC-010).
 */
export function WorkspaceProvider({
  workspaceId,
  children,
}: WorkspaceProviderProps) {
  const { api: runtimeClient } = useAppRuntime();

  // `runtimeClient` is created once at startup, so this runs once.
  const useWorkspace = useMemo(
    () =>
      createUseWorkspace({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );

  const { data, isLoading, isError, error, refetch } =
    useWorkspace(workspaceId);

  const value = useMemo<WorkspaceContextValue>(
    () => ({
      workspaceId,
      workspace: data ?? null,
      isLoading,
      isError,
      error,
      refetch,
    }),
    [workspaceId, data, isLoading, isError, error, refetch],
  );

  return (
    <WorkspaceContext.Provider value={value}>
      {children}
    </WorkspaceContext.Provider>
  );
}
