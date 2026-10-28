import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Deletes a trashed page for good (only a page in the trash can be deleted). */
export function createUseDeletePage(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useDeletePage(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: pageApi.delete,
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.trash(workspaceId),
        });
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.list(workspaceId),
        });
      },
    });
  };
}
