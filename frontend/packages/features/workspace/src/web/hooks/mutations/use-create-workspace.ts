import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createWorkspaceService, type WorkspaceApiClient, type WorkspaceEndpoints } from '../../../core/api/workspace.service';
import { workspaceQueryKeys } from '../../../core/query/keys';

interface UseCreateWorkspaceDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceEndpoints;
}

export function createUseCreateWorkspace({ api, endpoints }: UseCreateWorkspaceDeps) {
  const service = createWorkspaceService(api, endpoints);

  return function useCreateWorkspace() {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: service.create,
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: workspaceQueryKeys.all });
      },
    });
  };
}
