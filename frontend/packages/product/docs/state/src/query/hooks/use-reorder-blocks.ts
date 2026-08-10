import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";
import type { ReorderBlocksInput } from "@notrelix/docs-core";

export function createUseReorderBlocks(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  return function useReorderBlocks(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (payload: ReorderBlocksInput) => blockApi.reorder(payload),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.blocks(workspaceId, pageId),
        });
      },
    });
  };
}
