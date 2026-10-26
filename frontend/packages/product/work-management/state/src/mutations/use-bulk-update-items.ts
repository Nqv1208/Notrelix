import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { BulkItemChange } from "../api/item.api";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/**
 * One change for several items of a board as one request: every item changes
 * or none does. Not optimistic: the board is read again once the server has
 * answered, so a refusal leaves exactly what the server holds.
 */
export function useBulkUpdateItems(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: {
      readonly itemIds: readonly string[];
      readonly change: BulkItemChange;
    }) => cards.bulkUpdate(boardId, input.itemIds, input.change),
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      }),
  });
}
