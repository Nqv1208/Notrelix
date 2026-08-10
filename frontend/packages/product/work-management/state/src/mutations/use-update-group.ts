import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import type { UpdateGroupInput } from "../api/group.api";
import type { FullBoardResponse } from "@notrelix/work-management-core";

type MutationContext = { previous?: FullBoardResponse };

export function useUpdateGroup(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { groups } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId);

  return useMutation<void, Error, UpdateGroupInput, MutationContext>({
    mutationFn: groups.updateGroup,
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey);
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old;
        return {
          ...old,
          groups: old.groups.map((group) =>
            group.id === input.groupId
              ? {
                  ...group,
                  title: input.title ?? group.title,
                  color: input.color ?? group.color,
                }
              : group,
          ),
        };
      });
      return { previous };
    },
    onError: (_error, _input, context) => {
      queryClient.setQueryData(queryKey, context?.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey });
    },
  });
}
