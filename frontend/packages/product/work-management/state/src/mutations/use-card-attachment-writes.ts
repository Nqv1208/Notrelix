import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/**
 * Attaching to an item beyond a file: a web link and a Documents page. Each
 * resolves false when the server refused (the panel then says so and keeps
 * its input); the item's files and history are read again either way.
 */
export function useCardAttachmentWrites(cardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards, itemExtras } = useWorkManagementServices();

  const refresh = useCallback(() => {
    for (const queryKey of [
      wmQueryKeys.cardFiles(workspaceId, cardId),
      wmQueryKeys.cardActivity(workspaceId, cardId),
      wmQueryKeys.cardDetail(workspaceId, cardId),
    ])
      void queryClient.invalidateQueries({ queryKey });
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(
        workspaceId,
        "work-management",
        "item-linked-pages",
        cardId,
      ),
    });
  }, [cardId, queryClient, workspaceId]);

  const addWebLink = useCallback(
    async (url: string, name: string) => {
      try {
        await cards.createCardLink(
          cardId,
          { url, name },
          { idempotencyKey: crypto.randomUUID() },
        );
        return true;
      } catch {
        return false;
      } finally {
        refresh();
      }
    },
    [cardId, cards, refresh],
  );

  const linkPage = useCallback(
    async (pageId: string) => {
      try {
        await itemExtras.linkPage(cardId, pageId);
        return true;
      } catch {
        return false;
      } finally {
        refresh();
      }
    },
    [cardId, itemExtras, refresh],
  );

  return { addWebLink, linkPage };
}
