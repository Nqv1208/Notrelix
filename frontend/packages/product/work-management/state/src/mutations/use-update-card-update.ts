import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "../queries/keys"
import { useWorkManagementServices } from "../services"

export function useUpdateCardUpdate(cardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const { comments } = useWorkManagementServices()

  return useMutation({
    mutationFn: ({ updateId, body }: { updateId: string; body: string }) => comments.updateCardUpdate(updateId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardUpdates(workspaceId, cardId) })
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardActivity(workspaceId, cardId) })
    },
  })
}
