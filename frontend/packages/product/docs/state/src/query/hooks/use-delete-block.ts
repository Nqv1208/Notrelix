import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { Block } from "@notrelix/docs-core";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Ids of `blockId` and every block below it in the list. */
function subtreeIds(blocks: readonly Block[], blockId: string): Set<string> {
  const ids = new Set([blockId]);
  let grew = true;
  while (grew) {
    grew = false;
    for (const block of blocks) {
      if (block.parentId && ids.has(block.parentId) && !ids.has(block.id)) {
        ids.add(block.id);
        grew = true;
      }
    }
  }
  return ids;
}

export function createUseDeleteBlock(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  return function useDeleteBlock(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    const key = docsQueryKeys.blocks(workspaceId, pageId);
    return useMutation({
      // The block goes with the blocks under it, against the version this page last read.
      mutationFn: (block: Pick<Block, "id" | "version">) =>
        blockApi.delete(pageId, block.id, block.version ?? 0),
      onMutate: async (block: Pick<Block, "id" | "version">) => {
        await queryClient.cancelQueries({ queryKey: key });
        const previous = queryClient.getQueryData<Block[]>(key);
        if (previous) {
          const removed = subtreeIds(previous, block.id);
          queryClient.setQueryData<Block[]>(
            key,
            previous.filter((item) => !removed.has(item.id)),
          );
        }
        return { previous };
      },
      onError: (_error, _block, context) => {
        if (context?.previous) queryClient.setQueryData(key, context.previous);
      },
      onSettled: () => {
        void queryClient.invalidateQueries({ queryKey: key });
      },
    });
  };
}
