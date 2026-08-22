import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import type { CreateCardInput } from "@notrelix/work-management-core";
import type { Card, FullBoardResponse } from "@notrelix/work-management-core";
import { createOptimisticCard } from "../cache/optimistic-card";
import { useWorkManagementServices } from "../services";

type CreateCardContext = {
  previous?: FullBoardResponse;
  optimisticId: string;
};

export function useCreateCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId);

  return useMutation<void, Error, CreateCardInput, CreateCardContext>({
    mutationFn: (payload) => cards.createCard(boardId, payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey);
      const optimisticId = `optimistic-${Date.now()}`;

      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old;
        const targetGroup = old.groups.find(
          (group) => group.id === payload.listId,
        );
        if (!targetGroup) return old;
        const optimisticCard = createOptimisticCard(old, payload, optimisticId);
        return {
          ...old,
          groups: old.groups.map((group) =>
            group.id === payload.listId
              ? {
                  ...group,
                  cards: [...group.cards, optimisticCard].sort(
                    (a, b) => a.position - b.position,
                  ),
                }
              : group,
          ),
        };
      });

      return { previous, optimisticId };
    },
    onError: (_error, _payload, context) => {
      queryClient.setQueryData(queryKey, context?.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey });
    },
  });
}
