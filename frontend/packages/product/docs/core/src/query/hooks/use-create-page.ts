import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createPageApi } from '../../api/page.api';
import type { DocsApiClient, PageApiEndpoints } from '../../api/page.api';
import { docsQueryKeys } from '../keys';

export function createUseCreatePage(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const pageApi = createPageApi(api, endpoints);
  return function useCreatePage(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: pageApi.create,
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: docsQueryKeys.list(workspaceId) });
      },
    });
  };
}
