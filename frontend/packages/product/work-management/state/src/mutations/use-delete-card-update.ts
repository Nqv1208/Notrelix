import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useDeleteCardUpdate(cardId: string) {
  const queryClient = useQueryClient()
  const { comments } = useWorkManagementServices()

  return useMutation({
    mutationFn: comments.deleteCardUpdate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardUpdates(cardId) })
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardActivity(cardId) })
    },
  })
}
