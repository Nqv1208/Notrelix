import { useQuery } from '@tanstack/react-query';
import { createWorkspaceService, type WorkspaceApiClient, type WorkspaceEndpoints } from '../../../core/api/workspace.service';
import { workspaceQueryKeys } from '../../../core/query/keys';

interface UseWorkspaceListDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUseWorkspaceList({ api, endpoints, options }: UseWorkspaceListDeps) {
  const service = createWorkspaceService(api, endpoints);

  return function useWorkspaceList() {
    return useQuery({
      queryKey: workspaceQueryKeys.all,
      queryFn: () => service.getList(),
    });
  };
}
