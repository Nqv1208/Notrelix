import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createWorkspaceService, type WorkspaceApiClient, type WorkspaceEndpoints } from '../../../core/api/workspace.service';
import { workspaceQueryKeys } from '../../../core/query/keys';
import type { UpdateWorkspaceInput } from '../../../core/types/workspace';

interface UseUpdateWorkspaceDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceEndpoints;
}

export function createUseUpdateWorkspace({ api, endpoints }: UseUpdateWorkspaceDeps) {
  const service = createWorkspaceService(api, endpoints);

  return function useUpdateWorkspace(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (input: UpdateWorkspaceInput) => service.update(workspaceId, input),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: workspaceQueryKeys.detail(workspaceId) });
        queryClient.invalidateQueries({ queryKey: workspaceQueryKeys.all });
      },
    });
  };
}
