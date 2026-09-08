import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  defineOptimisticUpdate,
  executeOptimisticCommand,
} from "@notrelix/query";
import { wmQueryKeys } from "../queries/keys";
import type { CreateCardInput } from "@notrelix/work-management-core";
import type { FullBoardResponse } from "@notrelix/work-management-core";
import { addOptimisticCard } from "../cache/optimistic-card";
import { useWorkManagementServices } from "../services";

let createCardCommandSequence = 0;

export function useCreateCard(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId, boardId);

  return useMutation({
    mutationFn: (payload: CreateCardInput) => {
      createCardCommandSequence += 1;
      const commandId = `create-card:${boardId}:${createCardCommandSequence}`;
      return executeOptimisticCommand({
        queryClient,
        commandId,
        updates: [
          defineOptimisticUpdate<
            FullBoardResponse | undefined,
            CreateCardInput
          >(queryKey, (current, variables) =>
            addOptimisticCard(current, variables, `optimistic-${commandId}`),
          ),
        ],
        mutationFn: (variables, context) =>
          cards.createCard(boardId, variables, {
            correlationId: context.correlationId,
            idempotencyKey: context.idempotencyKey,
          }),
        variables: payload,
      });
    },
  });
}
