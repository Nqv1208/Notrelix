import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "../queries/keys"
import { useWorkManagementServices } from "../services"

export function useDuplicateGroup(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { groups } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId)

  return useMutation({
    mutationFn: groups.duplicateGroup,
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
