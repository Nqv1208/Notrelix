import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
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
  const toggle = useMutation({
    mutationFn: (input: { childId: string; done: boolean }) =>
      itemExtras.setChildDone(input.childId, input.done),
    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: childKey(workspaceId, itemId),
      });
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      });
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(workspaceId, "work-management", "cards"),
      });
    },
  });
  return { ...query, toggle: toggle.mutate };
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
    onSuccess: (_result, following) =>
      queryClient.setQueryData(followKey(workspaceId, itemId), following),
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
  return { following: query.data ?? null, setFollowing: set.mutate };
}
