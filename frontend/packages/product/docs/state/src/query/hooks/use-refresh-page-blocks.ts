import { useCallback, useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { docsQueryKeys } from "../keys";

/** Frames of one burst of changes refetch once, this long after the first. */
const COALESCE_MS = 250;

/**
 * Refetches the open page's blocks and detail from the producer (changes made
 * elsewhere, a reconnect). Calls close together share one refetch; the saver
 * merges the refetched blocks without touching blocks being edited.
 */
export function useRefreshPageBlocks(workspaceId: string, pageId: string) {
  const queryClient = useQueryClient();
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(
    () => () => {
      if (timer.current) clearTimeout(timer.current);
      timer.current = null;
    },
    [workspaceId, pageId],
  );

  return useCallback(() => {
    if (timer.current) return;
    timer.current = setTimeout(() => {
      timer.current = null;
      void queryClient.invalidateQueries({
        queryKey: docsQueryKeys.blocks(workspaceId, pageId),
      });
      void queryClient.invalidateQueries({
        queryKey: docsQueryKeys.detail(workspaceId, pageId),
      });
    }, COALESCE_MS);
  }, [queryClient, workspaceId, pageId]);
}
