import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRef } from "react";
import { createAttachmentApi } from "../../api/attachment.api";
import type {
  DocsApiClient,
  DocsFileClient,
  PageApiEndpoints,
} from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/**
 * Registers a link on a page (`Collaboration.PageAttachments.Create`, editors
 * only). One idempotency key belongs to one user action: sending the same
 * address and name again after a failure reuses it, so a retry never adds a
 * second link; a different link, or a success, starts a new key. A success
 * refreshes the page's attachment list.
 */
export function createUseCreatePageLink(
  api: DocsApiClient & DocsFileClient,
  endpoints: PageApiEndpoints,
) {
  const attachmentApi = createAttachmentApi(api, endpoints);
  return function useCreatePageLink(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    const action = useRef<{ fingerprint: string; key: string } | null>(null);
    return useMutation({
      mutationFn: (link: { url: string; name: string }) => {
        const fingerprint = `${link.url}\n${link.name}`;
        if (action.current?.fingerprint !== fingerprint) {
          action.current = { fingerprint, key: crypto.randomUUID() };
        }
        return attachmentApi.createLink(pageId, link, {
          idempotencyKey: action.current.key,
        });
      },
      onSuccess: () => {
        action.current = null;
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.attachments(workspaceId, pageId),
        });
      },
    });
  };
}
