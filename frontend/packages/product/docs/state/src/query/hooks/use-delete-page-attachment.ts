import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createAttachmentApi } from "../../api/attachment.api";
import type {
  DocsApiClient,
  DocsFileClient,
  PageApiEndpoints,
} from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/**
 * Removes a stored file from a page (`Collaboration.PageAttachments.Delete`, editors only: the server refuses
 * anyone without edit right, and the control is shown to editors alone). Success or failure re-reads the page's files.
 */
export function createUseDeletePageAttachment(
  api: DocsApiClient & DocsFileClient,
  endpoints: PageApiEndpoints,
) {
  const attachmentApi = createAttachmentApi(api, endpoints);
  return function useDeletePageAttachment(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (attachmentId: string) =>
        attachmentApi.remove(pageId, attachmentId),
      // A failed delete may still have removed it (a lost answer): read the list again either way.
      onSettled: () => {
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.attachments(workspaceId, pageId),
        });
      },
    });
  };
}
