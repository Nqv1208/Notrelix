import { useQuery } from "@tanstack/react-query";
import {
  createPageApi,
  type DocsApiClient,
  type PageApiEndpoints,
} from "../../api/page.api";
import { docsQueryKeys } from "@notrelix/docs-core/query/keys";

export function createUsePage(api: DocsApiClient, endpoints: PageApiEndpoints) {
  const pageApi = createPageApi(api, endpoints);
  return function usePage(pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.detail(pageId),
      queryFn: () => pageApi.getDetail(pageId),
      enabled: !!pageId,
    });
  };
}
