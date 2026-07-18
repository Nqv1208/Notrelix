import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createPageApi } from '~/api/page.api';
import type { DocsApiClient, PageApiEndpoints } from '~/api/page.api';
import { docsQueryKeys } from '../keys';

export function createUseDeletePage(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const pageApi = createPageApi(api, endpoints);
  return function useDeletePage(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: pageApi.delete,
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: docsQueryKeys.list(workspaceId) });
      },
    });
  };
}
