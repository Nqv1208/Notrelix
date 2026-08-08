import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useUpdateCardUpdate(cardId: string) {
  const queryClient = useQueryClient()
  const { comments } = useWorkManagementServices()

  return useMutation({
    mutationFn: ({ updateId, body }: { updateId: string; body: string }) => comments.updateCardUpdate(updateId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardUpdates(cardId) })
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardActivity(cardId) })
    },
  })
}
