import { useInfiniteQuery } from "@tanstack/react-query";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/**
 * The workspace's trashed pages the caller may restore or delete for good, one
 * cursor page at a time (`fetchNextPage` reads the next).
 */
export function createUseTrashedPages(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useTrashedPages(workspaceId: string, enabled = true) {
    return useInfiniteQuery({
      queryKey: docsQueryKeys.trash(workspaceId),
      queryFn: ({ pageParam }) => pageApi.getTrash(workspaceId, pageParam),
      initialPageParam: undefined as string | undefined,
      getNextPageParam: (last) => last.nextCursor ?? undefined,
      enabled: Boolean(workspaceId) && enabled,
    });
  };
}
