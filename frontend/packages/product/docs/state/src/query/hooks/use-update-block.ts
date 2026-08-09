import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createBlockApi } from "../../api/block.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "@notrelix/docs-core/query/keys";
import type { UpdateBlockPayload } from "@notrelix/docs-core";

export function createUseUpdateBlock(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const blockApi = createBlockApi(api, endpoints);
  return function useUpdateBlock(pageId: string, blockId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (payload: UpdateBlockPayload) =>
        blockApi.update(blockId, payload),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.blocks(pageId),
        });
      },
    });
  };
}
