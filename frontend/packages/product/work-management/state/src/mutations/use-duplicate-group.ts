import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/query"
import { groupApi } from "../api/group.api"

export function useDuplicateGroup(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation({
    mutationFn: groupApi.duplicateGroup,
    onSuccess: () => {
      toast.success("Group duplicated.")
    },
    onError: () => {
      toast.error("Failed to duplicate group.")
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
