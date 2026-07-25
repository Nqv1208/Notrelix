import type { Page } from '../types/page';
import type { PageDtoApi } from '../dto';
import { mapPage } from '../model/page.mapper';
import type { DocsApiClient, PageApiEndpoints } from './page.api';

export function createFavoriteApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async getFavorites(workspaceId: string): Promise<Page[]> {
      const pages = await api.get<PageDtoApi[]>(
        endpoints.pages.list(workspaceId),
      );
      return pages.map(mapPage).filter((page) => page.isFavorited);
    },

    // PENDING BACKEND: toggle page favorite is pending endpoint
    // See: docs/client/audits/backend-contract-gaps.md
    async toggleFavorite(_pageId: string): Promise<void> {
      console.warn('POST /pages/{pageId}/favorite is pending backend validation.');
    },
  };
}
