import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "../queries/keys"
import { useWorkManagementServices } from "../services"
import type { CreateCardUpdateInput } from "@notrelix/work-management-core"

export function useCreateCardUpdate(cardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const { comments } = useWorkManagementServices()

  return useMutation({
    mutationFn: (input: CreateCardUpdateInput) => comments.createCardUpdate(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardUpdates(workspaceId, cardId) })
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardDetail(workspaceId, cardId) })
    },
  })
}
