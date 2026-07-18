import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createViewsService } from '~/core/api/views.service';
import type { WorkspaceApiClient } from '~/core/api/workspace.service';
import { workspaceQueryKeys } from '~/core/query/keys';

interface UseCreateWorkspaceViewDeps {
  api: WorkspaceApiClient;
}

export function createUseCreateWorkspaceView({ api }: UseCreateWorkspaceViewDeps) {
  const service = createViewsService(api);

  return function useCreateWorkspaceView(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: service.create,
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: workspaceQueryKeys.views(workspaceId) });
      },
    });
  };
}
