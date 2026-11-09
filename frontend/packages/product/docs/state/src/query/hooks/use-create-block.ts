import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { Block, CreateBlockPayload } from "@notrelix/docs-core";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

let pendingSequence = 0;

/**
 * The block shown before the producer answers. Its id is temporary: the
 * stored block replaces it when the create succeeds.
 */
function pendingBlock(
  pageId: string,
  payload: CreateBlockPayload,
  blocks: readonly Block[],
): Block {
  const now = new Date().toISOString();
  const last = blocks.reduce((max, block) => Math.max(max, block.position), 0);
  pendingSequence += 1;
  return {
    id: `pending-${pendingSequence}`,
    pageId,
    type: payload.type,
    properties: payload.properties ?? {},
    position: last + 1000,
    parentId: payload.parentId ?? null,
    createdById: "",
    updatedById: "",
    createdAt: now,
    updatedAt: now,
  };
}

/**
 * `Documents.Blocks.CreateBlock`, optimistic on the page's block list: the
 * new block shows at once, the stored block replaces it on success, and the
 * list goes back to its last stored state when the producer refuses the
 * create (validation, a missing anchor, a conflict or the network).
 */
export function createUseCreateBlock(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  return function useCreateBlock(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    const key = docsQueryKeys.blocks(workspaceId, pageId);
    return useMutation({
      mutationFn: (payload: CreateBlockPayload) =>
        blockApi.create(pageId, payload),
      onMutate: async (payload: CreateBlockPayload) => {
        await queryClient.cancelQueries({ queryKey: key });
        const previous = queryClient.getQueryData<Block[]>(key);
        const pending = pendingBlock(pageId, payload, previous ?? []);
        if (previous)
          queryClient.setQueryData<Block[]>(key, [...previous, pending]);
        return { previous, pendingId: pending.id };
      },
      onSuccess: (stored, _payload, context) => {
        const current = queryClient.getQueryData<Block[]>(key);
        if (current && context?.pendingId)
          queryClient.setQueryData<Block[]>(
            key,
            current.map((block) =>
              block.id === context.pendingId ? stored : block,
            ),
          );
        void queryClient.invalidateQueries({ queryKey: key });
      },
      onError: (_error, _payload, context) => {
        if (context?.previous) queryClient.setQueryData(key, context.previous);
      },
    });
  };
}
