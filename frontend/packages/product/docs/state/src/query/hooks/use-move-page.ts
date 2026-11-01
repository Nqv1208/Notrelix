import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/**
 * `Documents.Pages.Move`: a page moves with its subtree under another page, or
 * to the top of its Space. Not optimistic: the Space of the whole subtree can
 * change, so every outcome refreshes the lists, the tree, the page and its
 * breadcrumb, and the next write carries the current version.
 */
export function createUseMovePage(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useMovePage(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      retry: false,
      mutationFn: pageApi.move,
      onSettled: async (_result, _error, payload) => {
        await Promise.all([
          queryClient.invalidateQueries({
            queryKey: docsQueryKeys.list(workspaceId),
          }),
          queryClient.invalidateQueries({
            queryKey: docsQueryKeys.tree(workspaceId),
          }),
          queryClient.invalidateQueries({
            queryKey: docsQueryKeys.detail(workspaceId, payload.pageId),
          }),
          queryClient.invalidateQueries({
            queryKey: docsQueryKeys.breadcrumb(workspaceId, payload.pageId),
          }),
        ]);
      },
    });
  };
}
