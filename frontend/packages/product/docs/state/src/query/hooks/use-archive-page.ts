import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Moves a page to the trash; it leaves the tree and the lists. */
export function createUseArchivePage(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useArchivePage(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: pageApi.archive,
      onSuccess: async (_result, pageId) => {
        // The page is gone: stop what is in flight for it and do not refetch
        // it (it would answer 404 while the screen is still leaving).
        const ofThisPage = (key: readonly unknown[]) => key.includes(pageId);
        await queryClient.cancelQueries({
          predicate: (query) =>
            query.queryKey.includes("documents") && ofThisPage(query.queryKey),
        });
        queryClient.removeQueries({
          predicate: (query) =>
            query.queryKey.includes("documents") && ofThisPage(query.queryKey),
          type: "inactive",
        });
        const collections = [
          docsQueryKeys.tree,
          docsQueryKeys.list,
          docsQueryKeys.trash,
          docsQueryKeys.favorites,
        ];
        for (const key of collections) {
          void queryClient.invalidateQueries({ queryKey: key(workspaceId) });
        }
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.all(workspaceId),
          predicate: (query) => query.queryKey.includes("search"),
        });
      },
    });
  };
}
