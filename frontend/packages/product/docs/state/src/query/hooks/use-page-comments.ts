import { useQuery } from "@tanstack/react-query";
import { createCommentApi } from "../../api/comment.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

export function createUsePageComments(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const commentApi = createCommentApi(api, endpoints);
  return function usePageComments(workspaceId: string, pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.comments(workspaceId, pageId),
      queryFn: () => commentApi.getList(pageId),
      enabled: !!workspaceId && !!pageId,
    });
  };
}
