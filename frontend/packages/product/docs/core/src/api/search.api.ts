import type { SearchResult } from '../types/tree';
import type { PageDtoApi } from '../dto';
import type { DocsApiClient, PageApiEndpoints } from './page.api';

export function createSearchApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async search(
      workspaceId: string,
      query: string,
    ): Promise<SearchResult[]> {
      const pages = await api.get<PageDtoApi[]>(
        `${endpoints.pages.search(workspaceId)}?query=${encodeURIComponent(query)}`,
      );
      return pages.map((page) => ({
        id: page.id,
        type: 'page' as const,
        title: page.title,
        excerpt: page.title,
        icon: page.iconValue ?? null,
        pageId: page.id,
        score: 1,
        group: 'Pages' as const,
      }));
    },
  };
}
