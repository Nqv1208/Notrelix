import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/query"
import { listApi } from "../api/list.api"
import type { FullBoardResponse, BoardGroup } from "@notrelix/work-management-core"

type MutationContext = { previous?: FullBoardResponse }

export function useReorderKanbanColumns(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, BoardGroup[], MutationContext>({
    mutationFn: (groups) => listApi.reorderLists(boardId, groups),
    onMutate: async (groups) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)

      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        return {
          ...old,
          groups: [...groups].sort((a, b) => a.position - b.position),
        }
      })

      return { previous }
    },
    onError: (_error, _input, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to reorder columns. Changes reverted.")
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey })
    },
  })
}
