import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/query"
import { columnApi, type CreateColumnInput } from "../api/field.api"

export function useCreateColumn(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<string, Error, Omit<CreateColumnInput, "boardId">>({
    mutationFn: (input) => columnApi.createColumn({ ...input, boardId }),
    onSuccess: () => {
      toast.success("Column created.")
      queryClient.invalidateQueries({ queryKey })
    },
    onError: () => toast.error("Failed to create column."),
  })
}
