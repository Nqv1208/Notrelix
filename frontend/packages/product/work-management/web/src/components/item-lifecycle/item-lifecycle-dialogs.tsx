import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@notrelix/ui-web";
import type { ItemActionsCopy } from "../item-actions/item-actions.copy";
import {
  fillLifecycleCopy,
  type ItemLifecycleCopy,
} from "./item-lifecycle.copy";

/** Asks before an item is deleted; the item can be restored from the board's trash for 30 days. */
export function DeleteItemDialog({
  copy,
  actions,
  title,
  open,
  onCancel,
  onConfirm,
}: {
  copy: ItemLifecycleCopy;
  /** The item actions copy, for the lines on sub-items and links. */
  actions?: ItemActionsCopy;
  title: string;
  open: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  return (
    <AlertDialog
      open={open}
      onOpenChange={(next) => (next ? null : onCancel())}
    >
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>
            {fillLifecycleCopy(copy.deleteTitle, { title })}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {copy.deleteBody}
            {actions ? ` ${actions.deleteSubItems} ${actions.deleteLinks}` : ""}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{copy.deleteCancel}</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm}>
            {copy.deleteConfirm}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
