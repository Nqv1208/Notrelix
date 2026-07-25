import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createViewsService } from '../../../core/api/views.service';
import type { WorkspaceApiClient } from '../../../core/api/workspace.service';
import { workspaceQueryKeys } from '../../../core/query/keys';

interface UseReorderWorkspaceViewsDeps {
  api: WorkspaceApiClient;
}

export function createUseReorderWorkspaceViews({ api }: UseReorderWorkspaceViewsDeps) {
  const service = createViewsService(api);

  return function useReorderWorkspaceViews(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (viewIds: string[]) => service.reorder(workspaceId, viewIds),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: workspaceQueryKeys.views(workspaceId) });
      },
    });
  };
}
