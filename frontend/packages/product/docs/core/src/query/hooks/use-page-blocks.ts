import { useQuery } from '@tanstack/react-query';
import { createBlockApi } from '~/api/block.api';
import type { DocsApiClient, PageApiEndpoints } from '~/api/page.api';
import { docsQueryKeys } from '../keys';

export function createUsePageBlocks(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const blockApi = createBlockApi(api, endpoints);
  return function usePageBlocks(pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.blocks(pageId),
      queryFn: () => blockApi.getList(pageId),
      enabled: !!pageId,
    });
  };
}
