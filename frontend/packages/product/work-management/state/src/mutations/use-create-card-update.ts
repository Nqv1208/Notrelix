import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"
import type { CreateCardUpdateInput } from "@notrelix/work-management-core"

export function useCreateCardUpdate(cardId: string) {
  const queryClient = useQueryClient()
  const { comments } = useWorkManagementServices()

  return useMutation({
    mutationFn: (input: CreateCardUpdateInput) => comments.createCardUpdate(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardUpdates(cardId) })
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardDetail(cardId) })
    },
  })
}
