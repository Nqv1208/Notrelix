import { useQuery } from "@tanstack/react-query";
import {
  createWorkspaceService,
  type WorkspaceApiClient,
  type WorkspaceEndpoints,
} from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseWorkspaceFiguresDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceEndpoints;
}

/**
 * The workspace figures (`Workspaces.Workspaces.GetWorkspaceDashboard`). Only Owner and Admin may read
 * them, so the host turns the query on for them alone; a refusal is not retried.
 */
export function createUseWorkspaceFigures({
  api,
  endpoints,
}: UseWorkspaceFiguresDeps) {
  const service = createWorkspaceService(api, endpoints);

  return function useWorkspaceFigures(workspaceId: string, enabled: boolean) {
    return useQuery({
      queryKey: workspaceQueryKeys.figures(workspaceId),
      queryFn: () => service.getFigures(workspaceId),
      enabled: !!workspaceId && enabled,
      retry: false,
    });
  };
}
