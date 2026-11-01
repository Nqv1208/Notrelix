import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

/** Who voted on an item for a Vote field; read when the list is opened. */
export function useItemVotes(
  itemId: string,
  fieldId: string,
  workspaceId: string,
  enabled: boolean,
) {
  const { itemCapabilities } = useWorkManagementServices();
  return useQuery({
    queryKey: wmQueryKeys.itemVotes(workspaceId, itemId, fieldId),
    queryFn: () => itemCapabilities.getVotes(itemId, fieldId),
    enabled: enabled && Boolean(itemId && fieldId),
    staleTime: 10_000,
  });
}

/** Who opened an item and when; read for the "Đã xem" chip and its list. */
export function useItemViewers(
  itemId: string,
  workspaceId: string,
  enabled = true,
) {
  const { itemCapabilities } = useWorkManagementServices();
  return useQuery({
    queryKey: wmQueryKeys.itemViewers(workspaceId, itemId),
    queryFn: () => itemCapabilities.getViewers(itemId),
    enabled: enabled && Boolean(itemId),
    staleTime: 30_000,
  });
}

/**
 * Records that the member opened the item, once per open. A refusal (a guest,
 * an archived board) is not an error the member needs to see.
 */
export function useRecordItemView(itemId: string, enabled: boolean) {
  const { itemCapabilities } = useWorkManagementServices();
  useEffect(() => {
    if (!enabled || !itemId) return;
    itemCapabilities.recordView(itemId).catch(() => undefined);
  }, [enabled, itemCapabilities, itemId]);
}

/** The checklists of a board's items, read when the "Chép mục từ" list is opened. */
export function useBoardChecklists(
  boardId: string,
  workspaceId: string,
  enabled: boolean,
) {
  const { itemCapabilities } = useWorkManagementServices();
  return useQuery({
    queryKey: wmQueryKeys.boardChecklists(workspaceId, boardId),
    queryFn: () => itemCapabilities.getBoardChecklists(boardId),
    enabled: enabled && Boolean(boardId),
    staleTime: 15_000,
  });
}

/**
 * The bytes of an image attachment of an item, read with the session and shown through an object URL
 * (an `<img>` cannot send the session itself). Null until read, or when there is no attachment.
 */
export function useCardImageUrl(
  cardId: string,
  attachmentId: string | null | undefined,
): string | null {
  const { cards } = useWorkManagementServices();
  const query = useQuery({
    queryKey: ["work-management", "card-image", cardId, attachmentId],
    queryFn: () => cards.downloadCardFile(cardId, attachmentId ?? ""),
    enabled: Boolean(cardId && attachmentId),
    staleTime: 5 * 60_000,
  });
  const [url, setUrl] = useState<string | null>(null);
  useEffect(() => {
    if (!query.data) {
      setUrl(null);
      return;
    }
    const next = URL.createObjectURL(query.data);
    setUrl(next);
    return () => URL.revokeObjectURL(next);
  }, [query.data]);
  return url;
}
