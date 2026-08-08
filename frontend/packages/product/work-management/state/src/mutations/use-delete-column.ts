import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useDeleteColumn(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { columns } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, string>({
    mutationFn: (columnId) => columns.deleteColumn(boardId, columnId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  })
}
