import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "../queries/keys"
import { useWorkManagementServices } from "../services"

export function useDeleteColumn(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { columns } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId)

  return useMutation<void, Error, string>({
    mutationFn: (columnId) => columns.deleteColumn(boardId, columnId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  })
}
