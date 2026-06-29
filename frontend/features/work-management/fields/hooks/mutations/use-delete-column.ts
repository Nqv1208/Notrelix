"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { columnApi } from "@/features/work-management/fields/api/field.api"

export function useDeleteColumn(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, string>({
    mutationFn: (columnId) => columnApi.deleteColumn(boardId, columnId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
    onError: () => toast.error("Failed to delete column."),
  })
}
