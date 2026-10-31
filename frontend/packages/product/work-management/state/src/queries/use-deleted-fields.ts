import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

const deletedKey = (workspaceId: string, boardId: string) =>
  workspaceQueryKey(
    workspaceId,
    "work-management",
    "boards",
    "deleted-fields",
    boardId,
  );

/**
 * The fields deleted from a board in the last 30 days and the way back
 * (a Manager reads and restores). Restoring brings the values back, so the
 * board, its settings and this list are read again. No old list is kept on
 * error: restoring from stale data could restore the wrong field.
 */
export function useDeletedFields(
  boardId: string,
  workspaceId: string,
  enabled = true,
) {
  const { boardSettings } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: deletedKey(workspaceId, boardId),
    queryFn: () => boardSettings.listDeletedFields(boardId),
    enabled: enabled && Boolean(boardId && workspaceId),
    staleTime: 0,
    retry: false,
  });
  const restore = useMutation({
    mutationFn: (fieldId: string) =>
      boardSettings.restoreDeletedField(boardId, fieldId),
    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: deletedKey(workspaceId, boardId),
      });
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(workspaceId, "work-management", "boards"),
      });
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      });
    },
  });
  return {
    fields: query.data ?? null,
    isLoading: query.isLoading,
    isError: query.isError,
    refetch: query.refetch,
    restore: restore.mutateAsync,
    restoringId: restore.isPending ? restore.variables : undefined,
  };
}
