import { useQuery } from "@tanstack/react-query";
import {
  createPageApi,
  type DocsApiClient,
  type PageApiEndpoints,
} from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export function createUsePage(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const pageApi = createPageApi(api, endpoints);
  return function usePage(workspaceId: string, pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.detail(workspaceId, pageId),
      queryFn: () => pageApi.getDetail(pageId),
      enabled: !!workspaceId && !!pageId,
    });
  };
}
