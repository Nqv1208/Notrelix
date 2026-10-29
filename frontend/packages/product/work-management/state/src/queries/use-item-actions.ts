import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

const linkedPagesKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(
    workspaceId,
    "work-management",
    "item-linked-pages",
    itemId,
  );

/**
 * The Documents pages linked to an item that the caller may open, with the
 * writes to link and unlink one. A page the caller cannot see is only counted.
 */
export function useItemLinkedPages(itemId: string, workspaceId: string) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: linkedPagesKey(workspaceId, itemId),
    queryFn: () => itemExtras.getLinkedPages(itemId),
    enabled: Boolean(itemId),
    staleTime: 15_000,
  });
  const refresh = () => {
    void queryClient.invalidateQueries({
      queryKey: linkedPagesKey(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardActivity(workspaceId, itemId),
    });
  };
  const link = useMutation({
    mutationFn: (pageId: string) => itemExtras.linkPage(itemId, pageId),
    onSettled: refresh,
  });
  const unlink = useMutation({
    mutationFn: (pageId: string) => itemExtras.unlinkPage(itemId, pageId),
    onSettled: refresh,
  });
  return {
    pages: query.data?.pages ?? [],
    hiddenCount: query.data?.hiddenCount ?? 0,
    isLoading: query.isLoading,
    isError: query.isError,
    refetch: query.refetch,
    link,
    unlink,
  };
}

/** The pages of the workspace the person may see, read only while the picker is open. */
export function useLinkablePages(workspaceId: string, enabled: boolean) {
  const { itemExtras } = useWorkManagementServices();
  const query = useQuery({
    queryKey: workspaceQueryKey(
      workspaceId,
      "work-management",
      "linkable-pages",
    ),
    queryFn: () => itemExtras.listWorkspacePages(workspaceId),
    enabled: enabled && Boolean(workspaceId),
    staleTime: 15_000,
  });
  return {
    pages: query.data ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
    refetch: query.refetch,
  };
}

/** Reads the item, its history, the board and its lifecycle again after an action on one item. */
export function useRefreshItem(workspaceId: string, boardId: string) {
  const queryClient = useQueryClient();
  return (itemId: string) => {
    for (const queryKey of [
      wmQueryKeys.cardDetail(workspaceId, itemId),
      wmQueryKeys.cardActivity(workspaceId, itemId),
      wmQueryKeys.fullBoard(workspaceId, boardId),
      workspaceQueryKey(
        workspaceId,
        "work-management",
        "item-lifecycle",
        itemId,
      ),
    ]) {
      void queryClient.invalidateQueries({ queryKey });
    }
  };
}
