import { useCallback } from "react";
import { toast } from "sonner";
import { useCreateGroup } from "@notrelix/work-management-state";
import { fillCopy } from "../views/kanban/kanban-card.copy";
import { useStatusOptionFormCopy } from "./use-status-option-form-copy";

/** What to say after a create; `false` says nothing (the caller reports it). */
export type AddGroupNotice =
  false | { readonly success?: string; readonly failure?: string };

/**
 * The one way a board group is created from the board surfaces: Table "Add
 * group", Kanban "Add group", the no-group notice and the create-item dialog
 * all go through it. It creates the BoardGroup (the item container) with the
 * optimistic add of `useCreateGroup`, appended last, and names the result.
 * It resolves when the group is created and rejects when it is not, after
 * the failure was said, so a form can stay open.
 */
export function useAddGroup(boardId: string, workspaceId: string) {
  const copy = useStatusOptionFormCopy();
  const { mutateAsync, isPending } = useCreateGroup(boardId, workspaceId);

  const addGroup = useCallback(
    async (
      draft: { readonly title: string; readonly color?: string },
      notice?: AddGroupNotice,
    ): Promise<void> => {
      try {
        await mutateAsync({
          title: draft.title,
          ...(draft.color ? { color: draft.color } : {}),
        });
      } catch (error) {
        if (notice !== false) {
          toast.error(notice?.failure ?? copy.groupAddFailed);
        }
        throw error;
      }
      if (notice !== false) {
        toast.success(
          notice?.success ?? fillCopy(copy.groupAdded, { name: draft.title }),
        );
      }
    },
    [copy, mutateAsync],
  );

  return { addGroup, isPending };
}
