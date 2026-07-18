import { useQuery } from '@tanstack/react-query';
import { createViewsService } from '~/core/api/views.service';
import type { WorkspaceApiClient } from '~/core/api/workspace.service';
import { workspaceQueryKeys } from '../keys';

interface UseWorkspaceViewsDeps {
  api: WorkspaceApiClient;
  options?: {
    mockMode?: boolean;
  };
}

export function createUseWorkspaceViews({ api, options }: UseWorkspaceViewsDeps) {
  const service = createViewsService(api, options);

  return function useWorkspaceViews(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.views(workspaceId),
      queryFn: () => service.getList(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
