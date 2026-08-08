import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"
import type { CreateGroupInput } from "../api/group.api"
import type { BoardGroup, FullBoardResponse } from "@notrelix/work-management-core"

type MutationContext = { previous?: FullBoardResponse }

export function useCreateGroup(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { groups } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(boardId, workspaceId)

  return useMutation<string, Error, Omit<CreateGroupInput, "boardId">, MutationContext>({
    mutationFn: (input) => groups.createGroup({ ...input, boardId }),
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      const optimisticId = `optimistic-group-${Date.now()}`
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        const group: BoardGroup = {
          id: optimisticId,
          title: input.title,
          color: input.color,
          position: input.position ?? ((old.groups.at(-1)?.position ?? 0) + 1),
          isCollapsed: false,
          cards: [],
        }
        return { ...old, groups: [...old.groups, group].sort((a, b) => a.position - b.position) }
      })
      return { previous }
    },
    onError: (_error, _input, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
