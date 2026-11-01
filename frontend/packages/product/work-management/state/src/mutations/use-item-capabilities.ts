import { useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { CardCover, CardRepeatRule } from "@notrelix/work-management-core";
import { workspaceQueryKey } from "@notrelix/query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** Where an item lives, for the reads a write makes stale. */
export interface ItemScope {
  readonly itemId: string;
  readonly boardId: string;
  readonly workspaceId: string;
}

function useRefreshItem({ itemId, boardId, workspaceId }: ItemScope) {
  const queryClient = useQueryClient();
  return useCallback(() => {
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardDetail(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(workspaceId, "work-management", "cards"),
    });
  }, [boardId, itemId, queryClient, workspaceId]);
}

/**
 * The cover, repeat rule and reminder of an item. Each write carries the item
 * version it was read at; a conflict rejects so the control can say so, and
 * the reads are fetched again either way.
 */
export function useItemSettingsWrites(scope: ItemScope, version: number) {
  const { itemCapabilities } = useWorkManagementServices();
  const refresh = useRefreshItem(scope);
  const setCover = useMutation({
    mutationFn: (cover: CardCover | null) =>
      itemCapabilities.setCover(scope.itemId, cover, version),
    onSettled: refresh,
  });
  const setRepeat = useMutation({
    mutationFn: (rule: CardRepeatRule | null) =>
      itemCapabilities.setRepeat(scope.itemId, rule, version),
    onSettled: refresh,
  });
  const setReminder = useMutation({
    mutationFn: (offsetMinutes: number) =>
      itemCapabilities.setReminder(scope.itemId, offsetMinutes, version),
    onSettled: refresh,
  });
  return {
    setCover: setCover.mutateAsync,
    setRepeat: setRepeat.mutateAsync,
    setReminder: setReminder.mutateAsync,
    pending: setCover.isPending || setRepeat.isPending || setReminder.isPending,
  };
}

/** Casts or withdraws the viewer's vote on one Vote field; the item and its voter list are read again. */
export function useVoteWrites(scope: ItemScope, fieldId: string) {
  const { itemCapabilities } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const refresh = useRefreshItem(scope);
  return useMutation({
    mutationFn: (voted: boolean) =>
      itemCapabilities.setVote(scope.itemId, fieldId, voted),
    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.itemVotes(
          scope.workspaceId,
          scope.itemId,
          fieldId,
        ),
      });
      refresh();
    },
  });
}

/** Copies the items of another checklist of the board into a new checklist of this item. */
export function useCopyChecklist(scope: ItemScope) {
  const { itemCapabilities } = useWorkManagementServices();
  const refresh = useRefreshItem(scope);
  return useMutation({
    mutationFn: (input: { sourceChecklistId: string; title?: string }) =>
      itemCapabilities.copyChecklist(
        scope.itemId,
        input.sourceChecklistId,
        input.title,
      ),
    onSettled: refresh,
  });
}

/** Removes every link of the item. */
export function useClearItemLinks(scope: ItemScope) {
  const { itemCapabilities } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const refresh = useRefreshItem(scope);
  return useMutation({
    mutationFn: () => itemCapabilities.clearLinks(scope.itemId),
    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(
          scope.workspaceId,
          "work-management",
          "item-links",
          scope.itemId,
        ),
      });
      refresh();
    },
  });
}
