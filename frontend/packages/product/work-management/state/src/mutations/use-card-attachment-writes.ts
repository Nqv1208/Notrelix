import { useCallback, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** Why a web link did not attach, so the panel can say which. */
export type LinkAttachFailure =
  /** Not an absolute http(s) address (found before sending). */
  | "invalid"
  /** The server answered 400. */
  | "rejected"
  /** The server answered 409: the item holds the most attachments it can. */
  | "limitReached"
  | "forbidden"
  | "notFound"
  /** No answer: the network is down. */
  | "offline"
  | "failed";

export type LinkAttachResult =
  | { readonly ok: true }
  | { readonly ok: false; readonly reason: LinkAttachFailure };

/** Maps what the API answered to the reason shown to the member. */
export function classifyLinkAttachError(error: unknown): LinkAttachFailure {
  const { kind, status } =
    typeof error === "object" && error !== null
      ? (error as { kind?: unknown; status?: unknown })
      : {};
  if (kind === "network") return "offline";
  if (status === 400) return "rejected";
  if (status === 409) return "limitReached";
  if (status === 403) return "forbidden";
  if (status === 404) return "notFound";
  return "failed";
}

/** True for an absolute http or https address. */
export function isWebUrl(value: string): boolean {
  try {
    return /^https?:$/.test(new URL(value).protocol);
  } catch {
    return false;
  }
}

/**
 * Attaching to an item beyond a file: a web link and a Documents page. A link
 * says why the server refused it (the panel then keeps its input); a page
 * resolves false. The item's files and history are read again either way.
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

  // One key per user action: a retry of the same address and name reuses it,
  // so a link the server already stored is not added twice.
  const linkKeys = useRef(new Map<string, string>());

  const addWebLink = useCallback(
    async (url: string, name: string): Promise<LinkAttachResult> => {
      if (!isWebUrl(url)) return { ok: false, reason: "invalid" };
      const action = JSON.stringify([url, name]);
      const idempotencyKey = linkKeys.current.get(action) ?? crypto.randomUUID();
      linkKeys.current.set(action, idempotencyKey);
      try {
        await cards.createCardLink(cardId, { url, name }, { idempotencyKey });
        linkKeys.current.delete(action);
        return { ok: true };
      } catch (error) {
        return { ok: false, reason: classifyLinkAttachError(error) };
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
