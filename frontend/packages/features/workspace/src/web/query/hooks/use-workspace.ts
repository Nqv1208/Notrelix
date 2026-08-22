import { useQuery } from "@tanstack/react-query";
import {
  createWorkspaceService,
  type WorkspaceApiClient,
  type WorkspaceEndpoints,
} from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseWorkspaceDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceEndpoints;
}

export function createUseWorkspace({ api, endpoints }: UseWorkspaceDeps) {
  const service = createWorkspaceService(api, endpoints);

  return function useWorkspace(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.detail(workspaceId),
      queryFn: () => service.getDetail(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
