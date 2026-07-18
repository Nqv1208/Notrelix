import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/query"
import { groupApi } from "../api/group.api"
import type { UpdateGroupInput } from "../api/group.api"
import type { FullBoardResponse } from "@notrelix/work-management-core"

type MutationContext = { previous?: FullBoardResponse }

export function useUpdateGroup(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, UpdateGroupInput, MutationContext>({
    mutationFn: groupApi.updateGroup,
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        return {
          ...old,
          groups: old.groups.map((group) =>
            group.id === input.groupId
              ? { ...group, title: input.title ?? group.title, color: input.color ?? group.color }
              : group
          ),
        }
      })
      return { previous }
    },
    onError: (_error, _input, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to update group. Changes reverted.")
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
