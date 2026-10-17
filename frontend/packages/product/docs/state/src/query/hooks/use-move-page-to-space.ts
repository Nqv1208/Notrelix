import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { MovePageToSpacePayload, Page } from "@notrelix/docs-core";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Ids of `pageId` and every page below it in the list. */
function subtreeIds(pages: readonly Page[], pageId: string): Set<string> {
  const ids = new Set([pageId]);
  let grew = true;
  while (grew) {
    grew = false;
    for (const page of pages) {
      if (page.parentId && ids.has(page.parentId) && !ids.has(page.id)) {
        ids.add(page.id);
        grew = true;
      }
    }
  }
  return ids;
}

/**
 * `Documents.Pages.MoveToSpace` (GOV-028, docs/product/workspaces.md §79):
 * a root page moves to a Space, or back to the workspace level, with its
 * whole subtree. Optimistic on the page list (rolled back when the producer
 * rejects the move: stale `expectedVersion`, no right on the target, nested
 * page); every outcome refreshes the list, the tree and the page, so the
 * next write carries the current version.
 */
export function createUseMovePageToSpace(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useMovePageToSpace(workspaceId: string) {
    const queryClient = useQueryClient();
    const listKey = docsQueryKeys.list(workspaceId);
    return useMutation({
      retry: false,
      mutationFn: (payload: MovePageToSpacePayload) =>
        pageApi.moveToSpace(payload),
      onMutate: async (payload: MovePageToSpacePayload) => {
        await queryClient.cancelQueries({ queryKey: listKey });
        const previous = queryClient.getQueryData<Page[]>(listKey);
        if (previous) {
          const moving = subtreeIds(previous, payload.pageId);
          queryClient.setQueryData<Page[]>(
            listKey,
            previous.map((page) =>
              moving.has(page.id)
                ? { ...page, spaceId: payload.spaceId }
                : page,
            ),
          );
        }
        return { previous };
      },
      onError: (_error, _payload, context) => {
        if (context?.previous)
          queryClient.setQueryData(listKey, context.previous);
      },
      onSettled: async (_result, _error, payload) => {
        await queryClient.invalidateQueries({ queryKey: listKey });
        await queryClient.invalidateQueries({
          queryKey: docsQueryKeys.tree(workspaceId),
        });
        await queryClient.invalidateQueries({
          queryKey: docsQueryKeys.detail(workspaceId, payload.pageId),
        });
      },
    });
  };
}
