import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"
import type { UpdateColumnInput } from "../api/field.api"

export function useUpdateColumn(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { columns } = useWorkManagementServices()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, Omit<UpdateColumnInput, "boardId">>({
    mutationFn: (input) => columns.updateColumn({ ...input, boardId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  })
}
