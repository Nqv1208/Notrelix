import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { Block } from "@notrelix/docs-core";
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
      // The block goes with the blocks under it, against the version this page last read.
      mutationFn: (block: Pick<Block, "id" | "version">) =>
        blockApi.delete(pageId, block.id, block.version ?? 0),
      onSettled: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.blocks(workspaceId, pageId),
        });
      },
    });
  };
}
