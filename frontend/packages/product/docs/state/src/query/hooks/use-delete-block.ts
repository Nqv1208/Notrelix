import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export function createUseDeleteBlock(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  return function useDeleteBlock(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: blockApi.delete,
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.blocks(workspaceId, pageId),
        });
      },
    });
  };
}
