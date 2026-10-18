import { useQuery } from "@tanstack/react-query";
import {
  createActivityService,
  type ActivityEndpoints,
} from "../../../core/api/activity.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseWorkspaceActivityDeps {
  api: WorkspaceApiClient;
  endpoints: ActivityEndpoints;
}

/** The first page of workspace activity (`Workspaces.Activity.GetWorkspaceActivity`). */
export function createUseWorkspaceActivity({
  api,
  endpoints,
}: UseWorkspaceActivityDeps) {
  const service = createActivityService(api, endpoints);

  return function useWorkspaceActivity(workspaceId: string, pageSize = 10) {
    return useQuery({
      queryKey: [...workspaceQueryKeys.activity(workspaceId), pageSize],
      queryFn: () => service.getPage(workspaceId, 1, pageSize),
      enabled: !!workspaceId,
      staleTime: 30_000,
    });
  };
}
