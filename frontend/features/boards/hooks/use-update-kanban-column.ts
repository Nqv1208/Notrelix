import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { listApi, type UpdateListInput } from "../api/list.api"
import type { FullBoardResponse } from "../types"

type MutationContext = { previous?: FullBoardResponse }

export function useUpdateKanbanColumn(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, UpdateListInput, MutationContext>({
    mutationFn: (input) => listApi.updateList(input),
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)

      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        return {
          ...old,
          groups: old.groups.map((group) => {
            if (group.id !== input.listId) return group
            return {
              ...group,
              title: input.title ?? group.title,
              color: input.color ?? group.color,
            }
          }),
        }
      })

      return { previous }
    },
    onError: (_error, _input, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to update column. Changes reverted.")
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey })
    },
  })
}
