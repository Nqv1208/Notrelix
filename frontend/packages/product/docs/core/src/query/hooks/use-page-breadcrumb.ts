import { useQuery } from '@tanstack/react-query';
import { createBreadcrumbApi } from '~/api/breadcrumb.api';
import type { DocsApiClient, PageApiEndpoints } from '~/api/page.api';
import { docsQueryKeys } from '../keys';

export function createUsePageBreadcrumb(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const breadcrumbApi = createBreadcrumbApi(api, endpoints);
  return function usePageBreadcrumb(pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.breadcrumb(pageId),
      queryFn: () => breadcrumbApi.getBreadcrumb(pageId),
      enabled: !!pageId,
    });
  };
}
