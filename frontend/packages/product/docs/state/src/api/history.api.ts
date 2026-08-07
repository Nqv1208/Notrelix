import type { PageActivity } from '@notrelix/docs-core';
import type { HistoryDtoApi } from '../dto';
import { mapHistory } from '../model/page.mapper';
import type { DocsApiClient, PageApiEndpoints } from './page.api';

export function createHistoryApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async getList(pageId: string): Promise<PageActivity[]> {
      const history = await api.get<HistoryDtoApi[]>(
        endpoints.pages.history(pageId),
      );
      return history.map((dto) => mapHistory(dto, pageId));
    },
  };
}
