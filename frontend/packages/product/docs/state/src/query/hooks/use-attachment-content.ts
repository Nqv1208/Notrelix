import { useQuery } from "@tanstack/react-query";
import { createAttachmentApi } from "../../api/attachment.api";
import type {
  DocsApiClient,
  DocsFileClient,
  PageApiEndpoints,
} from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/**
 * The bytes of a page file, read through the authorised content route with the
 * session. A stored file never changes, so the read stays fresh for good.
 */
export function createUseAttachmentContent(
  api: DocsApiClient & DocsFileClient,
  endpoints: PageApiEndpoints,
) {
  const attachmentApi = createAttachmentApi(api, endpoints);
  return function useAttachmentContent(
    workspaceId: string,
    pageId: string,
    attachmentId: string | null | undefined,
  ) {
    return useQuery({
      queryKey: docsQueryKeys.attachmentContent(
        workspaceId,
        pageId,
        attachmentId ?? "",
      ),
      queryFn: () => attachmentApi.content(pageId, attachmentId ?? ""),
      enabled: !!workspaceId && !!pageId && !!attachmentId,
      staleTime: Infinity,
      gcTime: 5 * 60_000,
      retry: 1,
    });
  };
}
