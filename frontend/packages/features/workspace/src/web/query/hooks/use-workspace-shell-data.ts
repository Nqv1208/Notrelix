import { createUseWorkspace } from "./use-workspace";
import { createUseWorkspaceViews } from "./use-workspace-views";
import type {
  WorkspaceApiClient,
  WorkspaceEndpoints,
} from "../../../core/api/workspace.service";

interface UseWorkspaceShellDataDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceEndpoints;
  options?: {
    mockMode?: boolean;
  };
}

export function createUseWorkspaceShellData({
  api,
  endpoints,
  options,
}: UseWorkspaceShellDataDeps) {
  const useWorkspace = createUseWorkspace({
    api,
    endpoints,
    ...(options ? { options } : {}),
  });
  const useWorkspaceViews = createUseWorkspaceViews({
    api,
    ...(options ? { options } : {}),
  });

  return function useWorkspaceShellData(workspaceId: string) {
    const workspace = useWorkspace(workspaceId);
    const views = useWorkspaceViews(workspaceId);

    return {
      workspace: workspace.data,
      views: views.data || [],
      isLoading: workspace.isLoading || views.isLoading,
      isError: workspace.isError || views.isError,
      refetch: async () => {
        await Promise.all([workspace.refetch(), views.refetch()]);
      },
    };
  };
}
