import { useQuery } from "@tanstack/react-query";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export function createUsePageBlocks(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  return function usePageBlocks(workspaceId: string, pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.blocks(workspaceId, pageId),
      queryFn: () => blockApi.getList(pageId),
      enabled: !!workspaceId && !!pageId,
    });
  };
}
