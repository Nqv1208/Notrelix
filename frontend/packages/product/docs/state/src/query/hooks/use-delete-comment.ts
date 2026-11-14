import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createCommentApi } from "../../api/comment.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export function createUseDeleteComment(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const commentApi = createCommentApi(api, endpoints);
  return function useDeleteComment(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: ({
        commentId,
        expectedVersion,
      }: {
        commentId: string;
        expectedVersion: number;
      }) => commentApi.delete(commentId, expectedVersion),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.comments(workspaceId, pageId),
        });
      },
    });
  };
}
