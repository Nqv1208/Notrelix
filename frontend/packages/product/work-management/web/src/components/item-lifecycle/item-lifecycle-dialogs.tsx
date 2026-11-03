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
import {
  fillLifecycleCopy,
  pluralLifecycleCopy,
  type ItemLifecycleCopy,
} from "./item-lifecycle.copy";

/** What deleting takes along: the sub-items and the dependency links (with the keys at the other end). */
export interface DeleteImpact {
  readonly subItems: number;
  readonly dependencies: {
    readonly count: number;
    readonly keys: readonly string[];
  };
}

/** Asks before an item is deleted; the item can be restored from the board's trash for 30 days. */
export function DeleteItemDialog({
  copy,
  impact,
  title,
  open,
  onCancel,
  onConfirm,
}: {
  copy: ItemLifecycleCopy;
  /** The sub-items and dependency links that go with the item; the lines show only when there are some. */
  impact?: DeleteImpact;
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
            {impact && impact.subItems > 0
              ? ` ${pluralLifecycleCopy(
                  {
                    one: copy.deleteSubItems_one,
                    other: copy.deleteSubItems_other,
                  },
                  impact.subItems,
                )}`
              : ""}
            {impact && impact.dependencies.count > 0
              ? ` ${fillLifecycleCopy(
                  impact.dependencies.count === 1
                    ? copy.deleteLinks_one
                    : copy.deleteLinks_other,
                  {
                    count: impact.dependencies.count,
                    keys: impact.dependencies.keys.join(", "),
                  },
                )}`
              : ""}
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
