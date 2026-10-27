import { useCallback } from "react";
import { toast } from "sonner";
import { useCreateGroup } from "@notrelix/work-management-state";
import { useItemCreateCopy } from "./use-item-create-copy";

/**
 * What an add path says instead of doing nothing: a board without a group
 * (every item belongs to a group) names the reason and offers to add one, and a
 * missing permission names that. Used by the quick add of a lane and the table.
 */
export function useNoGroupNotice(boardId: string, workspaceId: string) {
  const copy = useItemCreateCopy();
  const createGroup = useCreateGroup(boardId, workspaceId);
  const { mutate } = createGroup;

  const noGroup = useCallback(() => {
    toast.error(copy.noGroup, {
      action: {
        label: copy.addGroup,
        onClick: () =>
          mutate(
            { title: copy.defaultGroupTitle },
            {
              onSuccess: () => toast.success(copy.groupAdded),
              onError: () => toast.error(copy.groupAddFailed),
            },
          ),
      },
    });
  }, [copy, mutate]);

  const noPermission = useCallback(
    () => toast.error(copy.cannotAdd),
    [copy.cannotAdd],
  );

  return { noGroup, noPermission };
}
