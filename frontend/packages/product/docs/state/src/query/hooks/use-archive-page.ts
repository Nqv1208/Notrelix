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
      onSuccess: () =>
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.all(workspaceId),
        }),
    });
  };
}
