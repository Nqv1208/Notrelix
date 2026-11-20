import { useInfiniteQuery } from "@tanstack/react-query";
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

/**
 * The workspace feed, a page at a time (`Workspaces.Activity.GetWorkspaceActivity`). A Guest is refused the read,
 * so the host turns the query on for members alone; a refusal is not retried.
 */
export function createUseWorkspaceActivity({
  api,
  endpoints,
}: UseWorkspaceActivityDeps) {
  const service = createActivityService(api, endpoints);

  return function useWorkspaceActivity(
    workspaceId: string,
    enabled: boolean,
    pageSize = 10,
  ) {
    return useInfiniteQuery({
      queryKey: [...workspaceQueryKeys.activity(workspaceId), pageSize],
      queryFn: ({ pageParam }) =>
        service.getPage(workspaceId, pageParam, pageSize),
      initialPageParam: null as string | null,
      getNextPageParam: (last) => last.nextCursor,
      enabled: !!workspaceId && enabled,
      staleTime: 30_000,
      retry: false,
    });
  };
}
