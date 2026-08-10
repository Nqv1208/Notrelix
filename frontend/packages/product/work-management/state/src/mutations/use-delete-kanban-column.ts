import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import type { FullBoardResponse } from "@notrelix/work-management-core";

type MutationContext = { previous?: FullBoardResponse };

export function useDeleteKanbanColumn(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { lists } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId);

  return useMutation<void, Error, string, MutationContext>({
    mutationFn: (listId) => lists.deleteList(listId),
    onMutate: async (listId) => {
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey);

      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old;
        return {
          ...old,
          groups: old.groups.filter((group) => group.id !== listId),
        };
      });

      return { previous };
    },
    onError: (_error, _input, context) => {
      queryClient.setQueryData(queryKey, context?.previous);
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey });
    },
  });
}
