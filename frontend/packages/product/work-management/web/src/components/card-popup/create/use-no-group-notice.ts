import { useCallback } from "react";
import { toast } from "sonner";
import { useAddGroup } from "../../status-option/use-add-group";
import { useCardPopupCopy } from "../use-card-popup-copy";

/**
 * What an add path says instead of doing nothing: a board without a group
 * (every item belongs to a group) names the reason and offers to add one, and a
 * missing permission names that. Used by the quick add of a lane and the table.
 * The offered group is created through the same flow as "Add group".
 */
export function useNoGroupNotice(boardId: string, workspaceId: string) {
  const copy = useCardPopupCopy().create;
  const { addGroup } = useAddGroup(boardId, workspaceId);

  const noGroup = useCallback(() => {
    toast.error(copy.noGroup, {
      action: {
        label: copy.addGroup,
        onClick: () =>
          void addGroup(
            { title: copy.defaultGroupTitle },
            { success: copy.groupAdded, failure: copy.groupAddFailed },
          ).catch(() => undefined),
      },
    });
  }, [copy, addGroup]);

  const noPermission = useCallback(
    () => toast.error(copy.cannotAdd),
    [copy.cannotAdd],
  );

  return { noGroup, noPermission };
}
