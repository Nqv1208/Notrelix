"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { cardApi } from "@/features/work-management/items/api/item.api"

export function useDuplicateCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation({
    mutationFn: cardApi.duplicateCard,
    onSuccess: () => {
      toast.success("Task duplicated.")
    },
    onError: () => {
      toast.error("Failed to duplicate task.")
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
