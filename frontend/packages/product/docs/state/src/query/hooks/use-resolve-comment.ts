import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createCommentApi } from "../../api/comment.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export function createUseResolveComment(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const commentApi = createCommentApi(api, endpoints);
  return function useResolveComment(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: commentApi.resolve,
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.comments(workspaceId, pageId),
        });
      },
    });
  };
}
