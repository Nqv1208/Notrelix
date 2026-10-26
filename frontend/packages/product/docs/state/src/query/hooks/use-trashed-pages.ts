import { useQuery } from "@tanstack/react-query";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** The workspace's trashed pages the caller may restore or delete for good. */
export function createUseTrashedPages(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useTrashedPages(workspaceId: string, enabled = true) {
    return useQuery({
      queryKey: docsQueryKeys.trash(workspaceId),
      queryFn: () => pageApi.getTrash(workspaceId),
      enabled: Boolean(workspaceId) && enabled,
    });
  };
}
