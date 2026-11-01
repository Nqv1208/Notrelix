import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { BulkItemChange } from "../api/item.api";
import type { FullBoardResponse } from "@notrelix/work-management-core";
import { findBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { neededVersion } from "./seen-version";

/**
 * One change for several items of a board as one request: every item changes
 * or none does. Not optimistic: the board is read again once the server has
 * answered, so a refusal leaves exactly what the server holds. Every item
 * names the version the board read showed for it.
 */
export function useBulkUpdateItems(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: {
      readonly itemIds: readonly string[];
      readonly change: BulkItemChange;
    }) => {
      const board = queryClient.getQueryData<FullBoardResponse>(
        wmQueryKeys.fullBoard(workspaceId, boardId),
      );
      const expectedVersions: Record<string, number> = {};
      for (const id of input.itemIds)
        expectedVersions[id] = neededVersion(
          findBoardCard(board, id)?.version,
          id,
        );
      return cards.bulkUpdate(
        boardId,
        input.itemIds,
        input.change,
        expectedVersions,
      );
    },
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      }),
  });
}
