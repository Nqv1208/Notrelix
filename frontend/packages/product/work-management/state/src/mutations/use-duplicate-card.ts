import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import type { DuplicateItemOptions } from "../api/item.api";

export interface DuplicateCardInput {
  readonly cardId: string;
  readonly options?: DuplicateItemOptions;
}

export function useDuplicateCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId);

  return useMutation({
    mutationFn: (input: string | DuplicateCardInput) =>
      typeof input === "string"
        ? cards.duplicateCard(input)
        : cards.duplicateCard(input.cardId, input.options),
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey });
    },
  });
}
