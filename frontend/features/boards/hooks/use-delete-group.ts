"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { groupApi } from "../api/group.api"
import type { FullBoardResponse } from "../types"

type MutationContext = { previous?: FullBoardResponse }

export function useDeleteGroup(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, string, MutationContext>({
    mutationFn: groupApi.deleteGroup,
    onMutate: async (groupId) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) =>
        old ? { ...old, groups: old.groups.filter((group) => group.id !== groupId) } : old
      )
      return { previous }
    },
    onError: (_error, _groupId, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to delete group. Changes reverted.")
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
