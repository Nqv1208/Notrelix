import { useQuery } from "@tanstack/react-query";
import { createHistoryApi } from "../../api/history.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "@notrelix/docs-core/query/keys";

export function createUsePageHistory(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const historyApi = createHistoryApi(api, endpoints);
  return function usePageHistory(pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.history(pageId),
      queryFn: () => historyApi.getList(pageId),
      enabled: !!pageId,
    });
  };
}
