import { useEffect } from "react";
import {
  watchDocumentChanges,
  type DocsRealtime,
} from "@notrelix/docs-collaboration";

/**
 * Follows the open page: when others change it, or the connection came back
 * after a drop, `refresh` refetches the stored blocks. The realtime frames
 * carry no payload, so what changed is read from the producer, never patched.
 */
export function useDocLive(input: {
  realtime: DocsRealtime | undefined;
  workspaceId: string;
  pageId: string;
  refresh: () => void;
}) {
  const { realtime, workspaceId, pageId, refresh } = input;
  useEffect(() => {
    if (!realtime) return;
    return watchDocumentChanges(realtime, {
      workspaceId,
      documentId: pageId,
      onChange: refresh,
    });
  }, [realtime, workspaceId, pageId, refresh]);
}
