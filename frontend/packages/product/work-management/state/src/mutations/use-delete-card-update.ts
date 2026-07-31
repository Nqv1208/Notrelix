import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useDeleteCardUpdate(cardId: string) {
  const queryClient = useQueryClient()
  const { comments } = useWorkManagementServices()

  return useMutation({
    mutationFn: comments.deleteCardUpdate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.updates(cardId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.activity(cardId) })
    },
  })
}
