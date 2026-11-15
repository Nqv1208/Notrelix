import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** "Make a copy": the page only, copied by the server in one step. */
export function createUseDuplicatePage(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useDuplicatePage(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: { pageId: string; title?: string }) =>
        pageApi.duplicate(workspaceId, input.pageId, input.title),
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.list(workspaceId),
        });
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.tree(workspaceId),
        });
      },
    });
  };
}
