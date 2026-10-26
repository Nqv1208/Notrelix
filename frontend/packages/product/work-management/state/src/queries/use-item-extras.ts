import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type { ItemLinkDto } from "../api/item-extras.api";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

const childKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "item-children", itemId);
const followKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "item-following", itemId);

/** The child items of an item (gap `CTR-GAP-WS-ITEM-SHEET`). */
export function useItemChildItems(
  itemId: string,
  workspaceId: string,
  boardId: string,
) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: childKey(workspaceId, itemId),
    queryFn: () => itemExtras.getChildItems(itemId),
    enabled: Boolean(itemId),
    staleTime: 15_000,
  });
  const refresh = () => {
    void queryClient.invalidateQueries({
      queryKey: childKey(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(workspaceId, "work-management", "cards"),
    });
    // The parent's history and detail change with its children.
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardActivity(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardDetail(workspaceId, itemId),
    });
  };
  const toggle = useMutation({
    mutationFn: (input: { childId: string; done: boolean }) =>
      itemExtras.setChildDone(input.childId, input.done),
    onSettled: refresh,
  });
  const add = useMutation({
    mutationFn: (title: string) => itemExtras.addChildItem(itemId, title),
    onSettled: refresh,
  });
  return { ...query, toggle: toggle.mutate, addChild: add.mutate };
}

/** Whether the member follows the item, and the write (gap `CTR-GAP-WS-ITEM-SHEET`). */
export function useItemFollowing(itemId: string, workspaceId: string) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: followKey(workspaceId, itemId),
    queryFn: () => itemExtras.getFollowing(itemId),
    enabled: Boolean(itemId),
    staleTime: 15_000,
  });
  const set = useMutation({
    mutationFn: (following: boolean) =>
      itemExtras.setFollowing(itemId, following),
    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: followKey(workspaceId, itemId),
      });
      // My work and Home list the same relationship.
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(workspaceId, "work-management"),
      });
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(workspaceId, "workspace-feed"),
      });
    },
  });
  return {
    following: query.data?.following ?? null,
    followers: query.data?.followers ?? [],
    setFollowing: set.mutate,
  };
}

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
 * Who completed or archived an item, and the two writes that undo it: reopen
 * and restore (gap `CTR-GAP-WM-ITEM-STATES`).
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
  const reopen = useMutation({
    mutationFn: () => itemExtras.reopenItem(itemId),
    onSettled: refresh,
  });
  return {
    lifecycle: query.data ?? null,
    restore: restore.mutate,
    reopen: reopen.mutate,
  };
}

/** The people an item can name, and the invitation behind "Mời vào" (gap `CTR-GAP-WM-ITEM-STATES`). */
export function usePeopleDirectory(boardId: string, workspaceId: string) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const key = workspaceQueryKey(
    workspaceId,
    "work-management",
    "people-directory",
    boardId,
  );
  const query = useQuery({
    queryKey: key,
    queryFn: () => itemExtras.getPeopleDirectory(boardId, workspaceId),
    enabled: Boolean(boardId),
    staleTime: 30_000,
  });
  const invite = useMutation({
    mutationFn: (userId: string) => itemExtras.invitePerson(boardId, userId),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: key });
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      });
    },
  });
  return { directory: query.data ?? null, invitePerson: invite.mutate };
}
