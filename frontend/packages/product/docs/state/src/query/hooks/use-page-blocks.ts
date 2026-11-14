import { useQuery, useQueryClient } from "@tanstack/react-query";
import type { Block } from "@notrelix/docs-core";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

const WINDOW_ATTEMPTS = 3;

export function createUsePageBlocks(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  return function usePageBlocks(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    const queryKey = docsQueryKeys.blocks(workspaceId, pageId);
    return useQuery({
      queryKey,
      // The first window of 1 000 blocks resolves the query so the page renders
      // at once; the later windows are appended to the cache as they arrive.
      queryFn: async () => {
        const first = await blockApi.getWindow(pageId);
        if (first.nextCursor) {
          void (async () => {
            let cursor: string | null = first.nextCursor;
            while (cursor) {
              let window: Awaited<
                ReturnType<typeof blockApi.getWindow>
              > | null = null;
              for (
                let attempt = 0;
                attempt < WINDOW_ATTEMPTS && !window;
                attempt++
              ) {
                try {
                  window = await blockApi.getWindow(
                    pageId,
                    cursor,
                    queryClient.getQueryData<Block[]>(queryKey)?.length ?? 0,
                  );
                } catch {
                  // shortcut: a window that fails three times stops the load and the page keeps what it has, until the next refetch.
                }
              }
              if (!window) return;
              const more = window.blocks;
              queryClient.setQueryData<Block[]>(queryKey, (old) =>
                old
                  ? [
                      ...old,
                      ...more.filter((b) => !old.some((o) => o.id === b.id)),
                    ]
                  : old,
              );
              cursor = window.nextCursor;
            }
          })();
        }
        return first.blocks;
      },
      enabled: !!workspaceId && !!pageId,
    });
  };
}
