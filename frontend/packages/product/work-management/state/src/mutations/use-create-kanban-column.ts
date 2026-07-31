import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"
import type { CreateListInput } from "../api/list.api"
import type { FullBoardResponse, BoardGroup } from "@notrelix/work-management-core"

type MutationContext = { previous?: FullBoardResponse }

export function useCreateKanbanColumn(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const { lists } = useWorkManagementServices()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<string, Error, { title: string; color?: string; position?: number }, MutationContext>({
    mutationFn: (input) => lists.createList({ boardId, ...input }),
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      const optimisticId = `optimistic-column-${Date.now()}`

      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        const nextGroup: BoardGroup = {
          id: optimisticId,
          title: input.title,
          color: input.color || "var(--primary)",
          position: input.position ?? ((old.groups.at(-1)?.position ?? 0) + 1),
          isCollapsed: false,
          cards: [],
        }
        return {
          ...old,
          groups: [...old.groups, nextGroup].sort((a, b) => a.position - b.position),
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
