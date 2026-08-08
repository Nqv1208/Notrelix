import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useDuplicateCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { cards } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(boardId, workspaceId)

  return useMutation({
    mutationFn: cards.duplicateCard,
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
