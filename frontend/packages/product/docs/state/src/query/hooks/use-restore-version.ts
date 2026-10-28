import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { Block } from "@notrelix/docs-core";
import { createVersionApi } from "../../api/version.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** The restore asked about blocks whose versions are not known (nothing was read yet). */
export class RestoreNeedsBlocks extends Error {
  constructor() {
    super("The page's blocks are not loaded yet.");
  }
}

/**
 * Restores the page to a history entry. The request names every live block
 * with the version the person last read (from the cache the page shows), like
 * a block save does: if anything changed since, the producer answers 412 and
 * restores nothing; the blocks are then read again so the person looks at the
 * current page. On success the page's blocks become the restored ones.
 */
export function createUseRestoreVersion(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const versionApi = createVersionApi(api, endpoints);
  return function useRestoreVersion(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    const blocksKey = docsQueryKeys.blocks(workspaceId, pageId);
    return useMutation({
      mutationFn: async (versionId: string) => {
        const seen = queryClient.getQueryData<Block[]>(blocksKey);
        if (!seen) throw new RestoreNeedsBlocks();
        return versionApi.restore(
          pageId,
          versionId,
          seen.map((block) => ({
            blockId: block.id,
            version: block.version ?? 0,
          })),
        );
      },
      onSuccess: (restored) => {
        queryClient.setQueryData<Block[]>(blocksKey, restored.blocks);
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.versions(workspaceId, pageId),
        });
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.history(workspaceId, pageId),
        });
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.detail(workspaceId, pageId),
        });
      },
      onError: () => queryClient.invalidateQueries({ queryKey: blocksKey }),
    });
  };
}
