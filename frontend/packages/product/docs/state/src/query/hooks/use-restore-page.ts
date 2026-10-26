import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Brings a trashed page back into the tree. */
export function createUseRestorePage(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useRestorePage(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: pageApi.restore,
      onSuccess: () =>
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.all(workspaceId),
        }),
    });
  };
}
