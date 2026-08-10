import { useQuery } from "@tanstack/react-query";
import { createHistoryApi } from "../../api/history.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export function createUsePageHistory(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const historyApi = createHistoryApi(api, endpoints);
  return function usePageHistory(workspaceId: string, pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.history(workspaceId, pageId),
      queryFn: () => historyApi.getList(pageId),
      enabled: !!workspaceId && !!pageId,
    });
  };
}
