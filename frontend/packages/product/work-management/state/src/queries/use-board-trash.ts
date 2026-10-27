import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

/**
 * The items deleted from a board in the last 30 days (only people who may
 * edit items can read it) and the way back: restoring puts the item in its
 * group again and refreshes the board and the lists that show items.
 */
export function useBoardTrash(
  boardId: string,
  workspaceId: string,
  enabled = true,
) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: wmQueryKeys.boardTrash(workspaceId, boardId),
    queryFn: () => itemExtras.listTrash(boardId),
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
    entries: query.data ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
    restore: restore.mutateAsync,
    restoringId: restore.isPending ? restore.variables : undefined,
  };
}
