import { useInfiniteQuery } from "@tanstack/react-query";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";
import { ACTIVITY_PAGE_SIZE } from "../api/item.api";

/**
 * The history of a card, a page at a time (newest first); `data` stays the flat list. A full page means more
 * may wait; every write only invalidates the key, so the pages are read again from the first.
 */
export function useCardActivity(cardId: string, workspaceId: string) {
  const { cards } = useWorkManagementServices();
  return useInfiniteQuery({
    queryKey: wmQueryKeys.cardActivity(workspaceId, cardId),
    queryFn: ({ pageParam }) => cards.getCardActivity(cardId, pageParam),
    initialPageParam: 1,
    getNextPageParam: (last, pages) =>
      last.length >= ACTIVITY_PAGE_SIZE ? pages.length + 1 : undefined,
    // A page read later may repeat an entry the first pages already hold (newer events shift the pages).
    select: (data) => [
      ...new Map(data.pages.flat().map((event) => [event.id, event])).values(),
    ],
    enabled: Boolean(cardId),
    staleTime: 30_000,
  });
}
