import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useDuplicateGroup(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { groups } = useWorkManagementServices()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation({
    mutationFn: groups.duplicateGroup,
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
