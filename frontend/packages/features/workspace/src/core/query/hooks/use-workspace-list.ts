import { useQuery } from '@tanstack/react-query';
import { createWorkspaceService, type WorkspaceApiClient, type WorkspaceEndpoints } from '../../api/workspace.service';
import { workspaceQueryKeys } from '../keys';

interface UseWorkspaceListDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceEndpoints;
}

export function createUseWorkspaceList({ api, endpoints }: UseWorkspaceListDeps) {
  const service = createWorkspaceService(api, endpoints);

  return function useWorkspaceList() {
    return useQuery({
      queryKey: workspaceQueryKeys.all,
      queryFn: () => service.getList(),
    });
  };
}
