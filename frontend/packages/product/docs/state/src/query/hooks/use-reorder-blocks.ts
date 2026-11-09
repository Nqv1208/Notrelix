import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { Block, ReorderBlocksInput } from "@notrelix/docs-core";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/**
 * The list with the moves applied: each moved block takes its new parent and
 * a place next to the block it is named after. The producer computes the
 * stored key, so the list refetches once the request settles.
 */
function applyMoves(
  blocks: readonly Block[],
  moves: ReorderBlocksInput["moves"],
): Block[] {
  let next = [...blocks];
  for (const move of moves) {
    const moving = next.find((block) => block.id === move.blockId);
    if (!moving) continue;
    const parentId =
      move.parentId !== undefined ? move.parentId : moving.parentId;
    const after = move.afterBlockId
      ? next.find((block) => block.id === move.afterBlockId)
      : undefined;
    const before = move.beforeBlockId
      ? next.find((block) => block.id === move.beforeBlockId)
      : undefined;
    const lastSibling = next
      .filter(
        (block) => block.parentId === parentId && block.id !== move.blockId,
      )
      .reduce((max, block) => Math.max(max, block.position), 0);
    const position = after
      ? after.position + 500
      : before
        ? before.position - 500
        : lastSibling + 1000;
    next = next.map((block) =>
      block.id === move.blockId ? { ...block, parentId, position } : block,
    );
  }
  return next;
}

/**
 * `Documents.Blocks.Reorder`, optimistic on the page's block list: the moves
 * show at once and the last stored list comes back when the producer refuses
 * them (a stale version, a key outside its siblings or a block of another page).
 */
export function createUseReorderBlocks(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  return function useReorderBlocks(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    const key = docsQueryKeys.blocks(workspaceId, pageId);
    return useMutation({
      mutationFn: (payload: ReorderBlocksInput) => blockApi.reorder(payload),
      onMutate: async (payload: ReorderBlocksInput) => {
        await queryClient.cancelQueries({ queryKey: key });
        const previous = queryClient.getQueryData<Block[]>(key);
        if (previous)
          queryClient.setQueryData<Block[]>(
            key,
            applyMoves(previous, payload.moves),
          );
        return { previous };
      },
      onError: (_error, _payload, context) => {
        if (context?.previous) queryClient.setQueryData(key, context.previous);
      },
      onSuccess: () => {
        void queryClient.invalidateQueries({ queryKey: key });
      },
    });
  };
}
