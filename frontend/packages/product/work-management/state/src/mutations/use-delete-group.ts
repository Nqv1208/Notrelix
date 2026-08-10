import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import type { FullBoardResponse } from "@notrelix/work-management-core";

type MutationContext = { previous?: FullBoardResponse };

export function useDeleteGroup(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { groups } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId);

  return useMutation<void, Error, string, MutationContext>({
    mutationFn: groups.deleteGroup,
    onMutate: async (groupId) => {
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey);
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) =>
        old
          ? {
              ...old,
              groups: old.groups.filter((group) => group.id !== groupId),
            }
          : old,
      );
      return { previous };
    },
    onError: (_error, _groupId, context) => {
      queryClient.setQueryData(queryKey, context?.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey });
    },
  });
}
