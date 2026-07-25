import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/work-management-core"
import { cardApi } from "../api/item.api"
import type { UploadCardFileInput } from "@notrelix/work-management-core"

export function useUploadCardFile(cardId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: UploadCardFileInput) => cardApi.uploadCardFile(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.files(cardId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.detail(cardId) })
    },
    onError: () => {
      toast.error("Failed to upload file.")
    },
  })
}
