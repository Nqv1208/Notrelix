"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { cardApi } from "@/features/work-management/items/api/item.api"
import type { UploadCardFileInput } from "@/features/work-management/schemas"

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
