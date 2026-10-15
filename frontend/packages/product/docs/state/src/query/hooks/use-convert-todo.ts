import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createPageExtrasApi } from "../../api/page-extras.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Turns a checklist line into a work item (gap `CTR-GAP-DOCS-PAGE-EXT`). */
export function createUseConvertTodo(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const extras = createPageExtrasApi(api, endpoints);
  return function useConvertTodo(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (blockId: string) => extras.convertTodo(pageId, blockId),
      onSettled: () => {
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.blocks(workspaceId, pageId),
        });
        void queryClient.invalidateQueries({
          queryKey: docsQueryKeys.backlinks(workspaceId, pageId),
        });
      },
    });
  };
}
