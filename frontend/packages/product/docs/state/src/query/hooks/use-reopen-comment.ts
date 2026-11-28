import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createCommentApi } from "../../api/comment.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export function createUseReopenComment(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const commentApi = createCommentApi(api, endpoints);
  return function useReopenComment(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: { commentId: string; expectedVersion: number }) =>
        commentApi.reopen(input.commentId, input.expectedVersion),
      // Also after a refusal: a stale thread is read again as the server holds it.
      onSettled: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.comments(workspaceId, pageId),
        });
      },
    });
  };
}
