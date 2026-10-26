import { useQuery } from "@tanstack/react-query";
import { createAttachmentApi } from "../../api/attachment.api";
import type {
  DocsApiClient,
  DocsFileClient,
  PageApiEndpoints,
} from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** The files of a page (scoped by workspace and page; readable by page view). */
export function createUsePageAttachments(
  api: DocsApiClient & DocsFileClient,
  endpoints: PageApiEndpoints,
) {
  const attachmentApi = createAttachmentApi(api, endpoints);
  return function usePageAttachments(workspaceId: string, pageId: string) {
    return useQuery({
      queryKey: docsQueryKeys.attachments(workspaceId, pageId),
      queryFn: () => attachmentApi.list(pageId),
      enabled: !!workspaceId && !!pageId,
    });
  };
}
