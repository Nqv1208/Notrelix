import { useQuery } from "@tanstack/react-query";
import {
  createPageApi,
  type DocsApiClient,
  type PageApiEndpoints,
} from "../../api/page.api";
import { docsQueryKeys } from "@notrelix/docs-core/query/keys";

export function createUsePageList(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function usePageList(workspaceId: string) {
    return useQuery({
      queryKey: docsQueryKeys.list(workspaceId),
      queryFn: () => pageApi.getList(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
