import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createPageApi } from "../../api/page.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "@notrelix/docs-core/query/keys";
import type { UpdatePagePayload } from "@notrelix/docs-core";

export function createUseUpdatePage(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const pageApi = createPageApi(api, endpoints);
  return function useUpdatePage(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (payload: UpdatePagePayload) =>
        pageApi.update(pageId, payload),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.detail(pageId),
        });
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.list(workspaceId),
        });
      },
    });
  };
}
