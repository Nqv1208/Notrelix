import { useCallback, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type { Block } from "@notrelix/docs-core";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";
import { loadBlockWindows } from "./load-block-windows";

export function createUsePageBlocks(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  return function usePageBlocks(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    const queryKey = docsQueryKeys.blocks(workspaceId, pageId);
    // Where the background load stopped after a window failed three times.
    const [stalledAt, setStalledAt] = useState<string | null>(null);

    // Appends the windows after `from` to the cache, keeping what already shows.
    const loadRest = useCallback(
      async (from: string) => {
        const stalled = await loadBlockWindows(
          (cursor) =>
            blockApi.getWindow(
              pageId,
              cursor,
              queryClient.getQueryData<Block[]>(queryKey)?.length ?? 0,
            ),
          from,
          (more) =>
            queryClient.setQueryData<Block[]>(queryKey, (old) =>
              old
                ? [
                    ...old,
                    ...more.filter((b) => !old.some((o) => o.id === b.id)),
                  ]
                : old,
            ),
        );
        if (stalled) setStalledAt(stalled);
      },
      [pageId, workspaceId, queryClient],
    );

    const query = useQuery({
      queryKey,
      // The first window of 1 000 blocks resolves the query so the page renders
      // at once; the later windows are appended to the cache as they arrive.
      queryFn: async () => {
        const first = await blockApi.getWindow(pageId);
        setStalledAt(null);
        if (first.nextCursor) void loadRest(first.nextCursor);
        return first.blocks;
      },
      enabled: !!workspaceId && !!pageId,
    });
    const retryRest = stalledAt
      ? () => {
          setStalledAt(null);
          void loadRest(stalledAt);
        }
      : undefined;
    return { ...query, retryRest };
  };
}
