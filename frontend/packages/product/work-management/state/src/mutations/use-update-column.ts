import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/work-management-core"
import { columnApi } from "../api/field.api"
import type { UpdateColumnInput } from "../api/field.api"

export function useUpdateColumn(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, Omit<UpdateColumnInput, "boardId">>({
    mutationFn: (input) => columnApi.updateColumn({ ...input, boardId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
    onError: () => toast.error("Failed to update column."),
  })
}
