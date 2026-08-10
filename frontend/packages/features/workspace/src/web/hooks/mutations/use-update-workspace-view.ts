import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createViewsService } from "../../../core/api/views.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";
import type { UpdateWorkspaceViewInput } from "../../../core/types/workspace";

interface UseUpdateWorkspaceViewDeps {
  api: WorkspaceApiClient;
}

export function createUseUpdateWorkspaceView({
  api,
}: UseUpdateWorkspaceViewDeps) {
  const service = createViewsService(api);

  return function useUpdateWorkspaceView(workspaceId: string, viewId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (input: UpdateWorkspaceViewInput) =>
        service.update(workspaceId, viewId, input),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.views(workspaceId),
        });
      },
    });
  };
}
