import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createBlockApi } from '~/api/block.api';
import type { DocsApiClient, PageApiEndpoints } from '~/api/page.api';
import { docsQueryKeys } from '../keys';
import type { CreateBlockPayload } from '~/types/block';

export function createUseCreateBlock(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const blockApi = createBlockApi(api, endpoints);
  return function useCreateBlock(pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (payload: CreateBlockPayload) => blockApi.create(pageId, payload),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: docsQueryKeys.blocks(pageId) });
      },
    });
  };
}
