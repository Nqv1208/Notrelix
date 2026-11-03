import {
  useInfiniteQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type { BoardArchivedEntryDto } from "../api/item-extras.api";
import { neededVersion } from "../mutations/seen-version";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

/**
 * The items deleted from a board in the last 30 days (only people who may
 * edit items can read it), a page at a time and optionally narrowed by name or key, and the way back: restoring puts
 * the item (and the sub-items that went with it) in its group again and
 * refreshes the board and the lists that show items. A refusal (past the 30
 * days, the parent still deleted) is thrown to the caller, which tells why.
 */
export function useBoardTrash(
  boardId: string,
  workspaceId: string,
  search = "",
  enabled = true,
) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useInfiniteQuery({
    queryKey: wmQueryKeys.boardTrash(workspaceId, boardId, search),
    queryFn: ({ pageParam }) =>
      itemExtras.listTrash(boardId, search, pageParam ?? undefined),
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.nextCursor,
    enabled: enabled && Boolean(boardId && workspaceId),
    staleTime: 0,
  });
  const restore = useMutation({
    mutationFn: (itemId: string) => itemExtras.restoreDeleted(itemId),
    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.boardTrash(workspaceId, boardId),
      });
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      });
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(workspaceId, "work-management"),
      });
    },
  });
  return {
    entries: query.data?.pages.flatMap((page) => page.rows) ?? [],
    total: query.data?.pages[0]?.total ?? 0,
    isLoading: query.isLoading,
    isError: query.isError,
    retry: () => void query.refetch(),
    hasMore: query.hasNextPage,
    loadingMore: query.isFetchingNextPage,
    loadMore: () => void query.fetchNextPage(),
    restore: restore.mutateAsync,
    restoringId: restore.isPending ? restore.variables : undefined,
  };
}

/**
 * The items archived on a board, a page at a time and optionally narrowed by
 * name or key, and the way back: unarchiving puts the item (and the sub-items
 * archived with it) on the board again.
 */
export function useBoardArchive(
  boardId: string,
  workspaceId: string,
  search: string,
  enabled = true,
) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const key = wmQueryKeys.boardArchive(workspaceId, boardId, search);
  const query = useInfiniteQuery({
    queryKey: key,
    queryFn: ({ pageParam }) =>
      itemExtras.listArchived(boardId, search, pageParam ?? undefined),
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.nextCursor,
    enabled: enabled && Boolean(boardId && workspaceId),
    staleTime: 0,
  });
  const unarchive = useMutation({
    mutationFn: (itemId: string) =>
      itemExtras.restoreItem(
        itemId,
        neededVersion(
          queryClient
            .getQueryData<{
              pages: { rows: readonly BoardArchivedEntryDto[] }[];
            }>(key)
            ?.pages.flatMap((page) => page.rows)
            .find((entry) => entry.id === itemId)?.version,
          itemId,
        ),
      ),
    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.boardArchive(workspaceId, boardId),
      });
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      });
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(workspaceId, "work-management"),
      });
    },
  });
  return {
    entries: query.data?.pages.flatMap((page) => page.rows) ?? [],
    total: query.data?.pages[0]?.total ?? 0,
    isLoading: query.isLoading,
    isError: query.isError,
    retry: () => void query.refetch(),
    hasMore: query.hasNextPage,
    loadingMore: query.isFetchingNextPage,
    loadMore: () => void query.fetchNextPage(),
    unarchive: unarchive.mutateAsync,
    unarchivingId: unarchive.isPending ? unarchive.variables : undefined,
  };
}
