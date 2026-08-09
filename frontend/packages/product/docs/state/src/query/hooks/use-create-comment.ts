import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createCommentApi } from "../../api/comment.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "@notrelix/docs-core/query/keys";
import type { CreateCommentPayload } from "@notrelix/docs-core";

export function createUseCreateComment(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const commentApi = createCommentApi(api, endpoints);
  return function useCreateComment(pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (payload: CreateCommentPayload) =>
        commentApi.create(pageId, payload),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.comments(pageId),
        });
      },
    });
  };
}
