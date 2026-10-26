import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import type { PageAttachment } from "@notrelix/docs-core";
import { docsQueryKeys } from "../keys";

/**
 * Puts a file the server just stored at the head of the page's file list, so
 * the block that points at it can show its name and size without a refetch.
 */
export function useRememberPageAttachment(workspaceId: string, pageId: string) {
  const queryClient = useQueryClient();
  return useCallback(
    (attachment: PageAttachment) =>
      queryClient.setQueryData<PageAttachment[]>(
        docsQueryKeys.attachments(workspaceId, pageId),
        (known) => [
          attachment,
          ...(known ?? []).filter((entry) => entry.id !== attachment.id),
        ],
      ),
    [queryClient, workspaceId, pageId],
  );
}
