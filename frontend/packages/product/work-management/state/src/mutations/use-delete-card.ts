import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import type { FullBoardResponse } from "@notrelix/work-management-core";
import {
  conflictDetailOf,
  isVersionConflict,
  reportItemConflict,
} from "../queries/use-item-conflict";

type MutationContext = { previous?: FullBoardResponse };

/** What a delete names: the item and the version the confirmation showed. */
export interface DeleteCardInput {
  readonly cardId: string;
  readonly expectedVersion: number;
  /** The title the caller's messages name (the card has left the board read by then). */
  readonly title?: string;
}

/**
 * Hook-level callbacks run for every delete, also when the card sheet that asked has closed with the
 * optimistic removal (a callback passed to `mutate` is dropped then).
 */
export function useDeleteCard(
  boardId: string,
  workspaceId?: string,
  options: {
    readonly onSuccess?: (variables: DeleteCardInput) => void;
    readonly onError?: (error: unknown, variables: DeleteCardInput) => void;
  } = {},
) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId);

  return useMutation<void, Error, DeleteCardInput, MutationContext>({
    mutationFn: ({ cardId, expectedVersion }) =>
      cards.deleteCard(cardId, expectedVersion),
    onMutate: async ({ cardId }) => {
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey);
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old;
        return {
          ...old,
          groups: old.groups.map((group) => ({
            ...group,
            cards: group.cards.filter((card) => card.id !== cardId),
          })),
        };
      });
      return { previous };
    },
    onError: (error, variables, context) => {
      queryClient.setQueryData(queryKey, context?.previous);
      if (workspaceId && isVersionConflict(error))
        reportItemConflict(
          queryClient,
          workspaceId,
          variables.cardId,
          conflictDetailOf(error),
        );
      options.onError?.(error, variables);
    },
    onSuccess: (_data, variables) => options.onSuccess?.(variables),
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey });
      if (workspaceId) {
        queryClient.invalidateQueries({
          queryKey: wmQueryKeys.boardTrash(workspaceId, boardId),
        });
      }
    },
  });
}
