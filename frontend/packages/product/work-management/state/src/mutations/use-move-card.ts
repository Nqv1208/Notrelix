import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  defineOptimisticUpdate,
  executeOptimisticCommand,
} from "@notrelix/query";
import { wmQueryKeys } from "../queries/keys";
import type { MoveCardInput } from "@notrelix/work-management-core";
import type { FullBoardResponse } from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";

let moveCardCommandSequence = 0;

export function useMoveCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId);

  return useMutation({
    mutationFn: (payload: MoveCardInput) =>
      executeOptimisticCommand({
        queryClient,
        commandId: createMoveCardCommandId(payload),
        updates: [
          defineOptimisticUpdate<FullBoardResponse | undefined, MoveCardInput>(
            queryKey,
            (old, variables) => {
              if (!old) return old;
              let movedCard = old.groups
                .flatMap((group) => group.cards)
                .find((card) => card.id === variables.cardId);
              if (!movedCard) return old;
              movedCard = {
                ...movedCard,
                listId: variables.listId,
                position: variables.position,
                updatedAt: new Date().toISOString(),
              };
              return {
                ...old,
                groups: old.groups.map((group) => {
                  const withoutCard = group.cards.filter(
                    (card) => card.id !== variables.cardId,
                  );
                  if (group.id !== variables.listId)
                    return { ...group, cards: withoutCard };
                  return {
                    ...group,
                    cards: [...withoutCard, movedCard].sort(
                      (a, b) => a.position - b.position,
                    ),
                  };
                }),
              };
            },
          ),
        ],
        mutationFn: (variables, context) =>
          cards.moveCard(variables, {
            correlationId: context.correlationId,
            idempotencyKey: context.idempotencyKey,
          }),
        variables: payload,
      }),
  });
}

function createMoveCardCommandId(payload: MoveCardInput): string {
  moveCardCommandSequence += 1;
  return `move-card:${payload.cardId}:${moveCardCommandSequence}`;
}
