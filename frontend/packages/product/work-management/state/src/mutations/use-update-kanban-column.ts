import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"
import type { UpdateListInput } from "../api/list.api"
import type { FullBoardResponse } from "@notrelix/work-management-core"

type MutationContext = { previous?: FullBoardResponse }

export function useUpdateKanbanColumn(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const { lists } = useWorkManagementServices()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, UpdateListInput, MutationContext>({
    mutationFn: (input) => lists.updateList(input),
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
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey })
    },
  })
}
