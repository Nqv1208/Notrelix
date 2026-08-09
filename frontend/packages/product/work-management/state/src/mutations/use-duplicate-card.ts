import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "../queries/keys"
import { useWorkManagementServices } from "../services"

export function useDuplicateCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { cards } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId)

  return useMutation({
    mutationFn: cards.duplicateCard,
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
