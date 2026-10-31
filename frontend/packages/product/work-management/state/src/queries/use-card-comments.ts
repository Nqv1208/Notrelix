import { useInfiniteQuery } from "@tanstack/react-query";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

/**
 * The comments of a card, a page at a time (oldest first); `data` stays the flat list. Every write only
 * invalidates the key, so the pages are read again from the first.
 */
export function useCardComments(cardId: string, workspaceId: string) {
  const { comments } = useWorkManagementServices();
  return useInfiniteQuery({
    queryKey: wmQueryKeys.cardUpdates(workspaceId, cardId),
    queryFn: ({ pageParam }) =>
      comments.getCardUpdatesPage(cardId, pageParam ?? undefined),
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.nextCursor,
    select: (data) => data.pages.flatMap((page) => page.items),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  });
}
