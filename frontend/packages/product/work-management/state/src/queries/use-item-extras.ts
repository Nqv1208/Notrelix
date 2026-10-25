import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type { ItemLinkDto } from "../api/item-extras.api";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

/** The plan of a board: phases, milestones, dependencies (gap `CTR-GAP-WM-PLAN-ITEMS`). */
export function useBoardPlanItems(boardId: string, workspaceId: string) {
  const { itemExtras } = useWorkManagementServices();
  return useQuery({
    queryKey: workspaceQueryKey(
      workspaceId,
      "work-management",
      "plan-items",
      boardId,
    ),
    queryFn: () => itemExtras.getPlan(boardId),
    enabled: Boolean(boardId),
    staleTime: 30_000,
  });
}

const linksKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "item-links", itemId);

/** The links of an item and the writes on them (gap `CTR-GAP-WM-ITEM-DETAIL`). */
export function useItemLinks(
  itemId: string,
  workspaceId: string,
  boardId: string,
) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: linksKey(workspaceId, itemId),
    queryFn: () => itemExtras.getLinks(itemId),
    enabled: Boolean(itemId),
    staleTime: 15_000,
  });
  const refresh = () => {
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(workspaceId, "work-management", "item-links"),
    });
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(workspaceId, "work-management", "plan-items"),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardActivity(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
    });
  };
  const add = useMutation({
    mutationFn: (link: { itemId: string; relation: ItemLinkDto["relation"] }) =>
      itemExtras.addLink(itemId, link),
    onSettled: refresh,
  });
  const remove = useMutation({
    mutationFn: (linkId: string) => itemExtras.removeLink(itemId, linkId),
    onSettled: refresh,
  });
  return {
    ...query,
    addLink: add.mutate,
    removeLink: remove.mutate,
  };
}

const lifecycleKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "item-lifecycle", itemId);

/**
 * Whether an item is completed or archived, read from the item itself, and
 * the restore of an archived one. Reopening a done item has no contract yet.
 */
export function useItemLifecycle(
  itemId: string,
  workspaceId: string,
  boardId: string,
) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: lifecycleKey(workspaceId, itemId),
    queryFn: () => itemExtras.getLifecycle(itemId),
    enabled: Boolean(itemId),
    staleTime: 15_000,
  });
  const refresh = () => {
    void queryClient.invalidateQueries({
      queryKey: lifecycleKey(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardDetail(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardActivity(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(workspaceId, "work-management", "cards"),
    });
  };
  const restore = useMutation({
    mutationFn: () => itemExtras.restoreItem(itemId),
    onSettled: refresh,
  });
  return {
    lifecycle: query.data ?? null,
    restore: restore.mutate,
  };
}

/** The people an item can name: the workspace members and whether they see the board. */
export function usePeopleDirectory(boardId: string, workspaceId: string) {
  const { itemExtras } = useWorkManagementServices();
  const query = useQuery({
    queryKey: workspaceQueryKey(
      workspaceId,
      "work-management",
      "people-directory",
      boardId,
    ),
    queryFn: () => itemExtras.getPeopleDirectory(boardId, workspaceId),
    enabled: Boolean(boardId && workspaceId),
    staleTime: 30_000,
  });
  return { directory: query.data ?? null };
}
