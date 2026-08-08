import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import type { CreateColumnInput } from "../api/field.api"
import { useWorkManagementServices } from "../services"

export function useCreateColumn(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { columns } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(boardId, workspaceId)

  return useMutation<string, Error, Omit<CreateColumnInput, "boardId">>({
    mutationFn: (input) => columns.createColumn({ ...input, boardId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
