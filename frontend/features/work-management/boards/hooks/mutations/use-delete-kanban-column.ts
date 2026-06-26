import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { listApi } from "@/features/work-management/groups/api/list.api"
import type { FullBoardResponse } from "@/features/work-management/types"

type MutationContext = { previous?: FullBoardResponse }

export function useDeleteKanbanColumn(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, string, MutationContext>({
    mutationFn: (listId) => listApi.deleteList(listId),
    onMutate: async (listId) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)

      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        return {
          ...old,
          groups: old.groups.filter((group) => group.id !== listId),
        }
      })

      return { previous }
    },
    onError: (_error, _input, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to delete column. Changes reverted.")
    },
    onSuccess: () => {
      toast.success("Column deleted.")
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey })
    },
  })
}
