import type { BreadcrumbItem } from '@notrelix/docs-core';
import type { BreadcrumbDtoApi } from '../dto';
import { mapBreadcrumb } from '../model/page.mapper';
import type { DocsApiClient, PageApiEndpoints } from './page.api';

export function createBreadcrumbApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async getBreadcrumb(pageId: string): Promise<BreadcrumbItem[]> {
      const breadcrumb = await api.get<BreadcrumbDtoApi[]>(
        endpoints.pages.breadcrumb(pageId),
      );
      return breadcrumb.map(mapBreadcrumb);
    },
  };
}
