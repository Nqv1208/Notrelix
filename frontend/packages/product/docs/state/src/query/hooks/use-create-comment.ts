import { useMemo } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createCommentApi } from "../../api/comment.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";
import type { CreateCommentPayload } from "@notrelix/docs-core";
import { createCommentActionKeys } from "./comment-action-keys";

export function createUseCreateComment(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const commentApi = createCommentApi(api, endpoints);
  return function useCreateComment(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    // A double click or a retry of one comment posts it once (UC-COL-H01).
    const keys = useMemo(() => createCommentActionKeys(), [pageId]);
    return useMutation({
      mutationFn: (payload: CreateCommentPayload) =>
        commentApi.create(pageId, payload, {
          idempotencyKey: keys.keyFor(payload),
        }),
      onSuccess: (_created, payload) => {
        keys.succeeded(payload);
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.comments(workspaceId, pageId),
        });
      },
    });
  };
}
