import { useQuery } from '@tanstack/react-query';
import { createViewsService } from '../../api/views.service';
import type { WorkspaceApiClient } from '../../api/workspace.service';
import { workspaceQueryKeys } from '../keys';

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
