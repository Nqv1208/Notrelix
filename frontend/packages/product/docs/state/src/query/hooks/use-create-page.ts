import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { Page } from "@notrelix/docs-core";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Prefix of the id a child row has in the tree until the server answers. */
export const PENDING_PAGE_ID_PREFIX = "pending:";

/**
 * `Documents.Pages.CreatePage`. A page created under a parent shows in the
 * tree at once as a copy of its parent's placement; a refusal puts the list
 * back exactly as it was, and every outcome refreshes it.
 */
export function createUseCreatePage(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useCreatePage(workspaceId: string) {
    const queryClient = useQueryClient();
    const listKey = docsQueryKeys.list(workspaceId);
    return useMutation({
      retry: false,
      mutationFn: pageApi.create,
      onMutate: async (payload) => {
        const previous = queryClient.getQueryData<Page[]>(listKey);
        const parent = previous?.find((page) => page.id === payload.parentId);
        if (!previous || !parent) return { previous: undefined };
        await queryClient.cancelQueries({ queryKey: listKey });
        const now = new Date().toISOString();
        queryClient.setQueryData<Page[]>(listKey, [
          ...previous,
          {
            ...parent,
            id: `${PENDING_PAGE_ID_PREFIX}${payload.idempotencyKey ?? now}`,
            title: payload.title,
            navTitle: undefined,
            parentId: parent.id,
            position: 0,
            version: null,
            createdAt: now,
            updatedAt: now,
            lastEditedAt: now,
          },
        ]);
        return { previous };
      },
      onError: (_error, _payload, context) => {
        if (context?.previous)
          queryClient.setQueryData(listKey, context.previous);
      },
      onSettled: () => queryClient.invalidateQueries({ queryKey: listKey }),
    });
  };
}
