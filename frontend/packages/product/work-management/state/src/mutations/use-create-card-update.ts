import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"
import type { CreateCardUpdateInput } from "@notrelix/work-management-core"

export function useCreateCardUpdate(cardId: string) {
  const queryClient = useQueryClient()
  const { comments } = useWorkManagementServices()

  return useMutation({
    mutationFn: (input: CreateCardUpdateInput) => comments.createCardUpdate(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.updates(cardId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.detail(cardId) })
    },
  })
}
