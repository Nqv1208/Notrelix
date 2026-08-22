import { useQuery } from "@tanstack/react-query";
import { createViewsService } from "../../../core/api/views.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseWorkspaceViewsDeps {
  api: WorkspaceApiClient;
}

export function createUseWorkspaceViews({ api }: UseWorkspaceViewsDeps) {
  const service = createViewsService(api);

  return function useWorkspaceViews(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.views(workspaceId),
      queryFn: () => service.getList(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
