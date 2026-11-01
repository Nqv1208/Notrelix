import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type { ItemLinkDto } from "../api/item-extras.api";
import { useWorkManagementServices } from "../services";
import { neededVersion } from "../mutations/seen-version";
import type {
  ItemChildItemDto,
  ItemLifecycleDto,
} from "../api/item-extras.api";
import { wmQueryKeys } from "./keys";

const childKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "item-children", itemId);

/** The sub-items of an item, with ticking one and adding one. */
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
      itemExtras.setChildDone(
        input.childId,
        input.done,
        neededVersion(
          queryClient
            .getQueryData<readonly ItemChildItemDto[]>(
              childKey(workspaceId, itemId),
            )
            ?.find((child) => child.id === input.childId)?.version,
          input.childId,
        ),
      ),
    onSettled: refresh,
  });
  const add = useMutation({
    mutationFn: (title: string) => itemExtras.addChildItem(itemId, title),
    onSettled: refresh,
  });
  return { ...query, toggle: toggle.mutate, addChild: add.mutate };
}

const followKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "item-following", itemId);

/** Whether the member follows the item, who else does, and the write. */
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

const approvalsKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "item-approvals", itemId);

/** The approvals asked about the item, the write that asks a person for one, and the answers to it. */
export function useItemApprovals(itemId: string, workspaceId: string) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: approvalsKey(workspaceId, itemId),
    queryFn: () => itemExtras.getApprovals(itemId),
    enabled: Boolean(itemId),
    staleTime: 15_000,
  });
  const request = useMutation({
    mutationFn: (input: { approverUserId: string; note: string | null }) =>
      itemExtras.requestApproval(itemId, input.approverUserId, input.note),
    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: approvalsKey(workspaceId, itemId),
      });
    },
  });
  const refresh = () => {
    void queryClient.invalidateQueries({
      queryKey: approvalsKey(workspaceId, itemId),
    });
    // The item's history and the feeds that list approvals waiting for a person change with it.
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardActivity(workspaceId, itemId),
    });
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(workspaceId, "workspace-feed"),
    });
  };
  const decide = useMutation({
    mutationFn: (input: {
      requestId: string;
      decision: "approve" | "reject";
      comment: string | null;
    }) =>
      itemExtras.decideApproval(
        workspaceId,
        input.requestId,
        input.decision,
        input.comment,
      ),
    onSettled: refresh,
  });
  const cancel = useMutation({
    mutationFn: (requestId: string) => {
      const version = query.data?.find((a) => a.id === requestId)?.version;
      if (version === undefined) {
        return Promise.reject(new Error("The approval is no longer loaded."));
      }
      return itemExtras.cancelApproval(requestId, version);
    },
    onSettled: refresh,
  });
  return {
    approvals: query.data,
    request: request.mutate,
    requesting: request.isPending,
    /** The named approver approves or rejects, with an optional comment. */
    decide: decide.mutate,
    /** The requester withdraws a pending request. */
    cancel: cancel.mutate,
    /** The server's refusal of the last request (a person already asked, an approver who cannot read the item). */
    error: request.error,
    /** The server's refusal of the last decision or cancel (already decided, not allowed). */
    answerError: decide.error ?? cancel.error,
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

/**
 * Timeline dependencies `from -> to` (`to` waits for `from`). Each write is a
 * request of its own, so an undo is the inverse call, never a cache rewind;
 * the plan is refetched after every settle and stays the single truth.
 */
export function useTimelineDependencies(boardId: string, workspaceId: string) {
  const { itemExtras } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const refresh = () => {
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(workspaceId, "work-management", "plan-items"),
    });
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(workspaceId, "work-management", "item-links"),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
    });
  };
  const add = useMutation({
    mutationFn: (v: { from: string; to: string }) =>
      itemExtras.addDependency(v.from, v.to),
    onSettled: refresh,
  });
  const remove = useMutation({
    mutationFn: (v: { from: string; to: string }) =>
      itemExtras.removeDependency(v.from, v.to),
    onSettled: refresh,
  });
  return {
    createDependency: add.mutateAsync,
    removeDependency: remove.mutateAsync,
  };
}

const lifecycleKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "item-lifecycle", itemId);

/**
 * Whether an item is completed or archived, read from the item itself, the
 * restore of an archived one and the reopening of a completed one.
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
  // Both writes name the version the lifecycle read showed, never a fresh one.
  const lifecycleVersion = () =>
    neededVersion(
      queryClient.getQueryData<ItemLifecycleDto>(
        lifecycleKey(workspaceId, itemId),
      )?.version,
      itemId,
    );
  const restore = useMutation({
    mutationFn: () => itemExtras.restoreItem(itemId, lifecycleVersion()),
    onSettled: refresh,
  });
  const reopen = useMutation({
    mutationFn: () => itemExtras.reopenItem(itemId, lifecycleVersion()),
    onSettled: refresh,
  });
  return {
    lifecycle: query.data ?? null,
    restore: restore.mutate,
    reopen: reopen.mutate,
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
