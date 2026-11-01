import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

export function useDeleteColumn(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { columns } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId);

  return useMutation<
    void,
    Error,
    { columnId: string; expectedVersion: number }
  >({
    mutationFn: ({ columnId, expectedVersion }) =>
      columns.deleteColumn(boardId, columnId, expectedVersion),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  });
}
