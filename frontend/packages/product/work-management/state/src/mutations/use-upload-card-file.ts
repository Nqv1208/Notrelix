import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "../queries/keys"
import { useWorkManagementServices } from "../services"
import type { UploadCardFileInput } from "@notrelix/work-management-core"

export function useUploadCardFile(cardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const { cards } = useWorkManagementServices()

  return useMutation({
    mutationFn: (input: UploadCardFileInput) => cards.uploadCardFile(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardFiles(workspaceId, cardId) })
      queryClient.invalidateQueries({ queryKey: wmQueryKeys.cardDetail(workspaceId, cardId) })
    },
  })
}
