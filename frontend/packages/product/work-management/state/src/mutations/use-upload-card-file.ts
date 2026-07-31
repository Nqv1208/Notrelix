import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"
import type { UploadCardFileInput } from "@notrelix/work-management-core"

export function useUploadCardFile(cardId: string) {
  const queryClient = useQueryClient()
  const { cards } = useWorkManagementServices()

  return useMutation({
    mutationFn: (input: UploadCardFileInput) => cards.uploadCardFile(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.files(cardId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.detail(cardId) })
    },
  })
}
