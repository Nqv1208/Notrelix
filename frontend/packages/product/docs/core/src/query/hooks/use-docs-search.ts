import { useQuery } from '@tanstack/react-query';
import { createSearchApi } from '../../api/search.api';
import type { DocsApiClient, PageApiEndpoints } from '../../api/page.api';
import { docsQueryKeys } from '../keys';

export function createUseDocsSearch(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const searchApi = createSearchApi(api, endpoints);
  return function useDocsSearch(workspaceId: string, query: string) {
    return useQuery({
      queryKey: docsQueryKeys.search(workspaceId, query),
      queryFn: () => searchApi.search(workspaceId, query),
      enabled: !!workspaceId && query.length > 0,
    });
  };
}
