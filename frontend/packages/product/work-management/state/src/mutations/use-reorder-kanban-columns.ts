import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "../queries/keys"
import { useWorkManagementServices } from "../services"
import type { FullBoardResponse, BoardGroup } from "@notrelix/work-management-core"

type MutationContext = { previous?: FullBoardResponse }

export function useReorderKanbanColumns(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const { lists } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId)

  return useMutation<void, Error, BoardGroup[], MutationContext>({
    mutationFn: (groups) => lists.reorderLists(boardId, groups),
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
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey })
    },
  })
}
