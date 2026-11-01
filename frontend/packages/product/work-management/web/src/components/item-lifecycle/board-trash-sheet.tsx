import {
  fillLifecycleCopy,
  type ItemLifecycleCopy,
} from "./item-lifecycle.copy";
import {
  Button,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@notrelix/ui-web";

export interface BoardTrashRow {
  readonly id: string;
  readonly name: string;
  readonly itemKey: string | null;
  /** `YYYY-MM-DD` of the last day the item can be restored. */
  readonly restorableUntilDay?: string;
  /** The line under the name when it is not the restore day (the archive list). */
  readonly note?: string;
}

/** The words of one list: the same sheet shows the deleted items and the archived ones. */
export interface BoardTrashLabels {
  readonly title: string;
  readonly body: string;
  readonly empty: string;
  readonly loading: string;
  readonly error: string;
  readonly action: string;
}

const trashLabels = (copy: ItemLifecycleCopy): BoardTrashLabels => ({
  title: copy.trashTitle,
  body: copy.trashBody,
  empty: copy.trashEmpty,
  loading: copy.trashLoading,
  error: copy.trashError,
  action: copy.trashRestore,
});

/** The board's deleted items with the way back; shown from the board menu. */
export function BoardTrashSheet({
  copy,
  labels = trashLabels(copy),
  open,
  rows,
  loading,
  failed,
  restoringId,
  onRestore,
  onClose,
}: {
  copy: ItemLifecycleCopy;
  /** The words of the list; the deleted items' when absent. */
  labels?: BoardTrashLabels;
  open: boolean;
  rows: readonly BoardTrashRow[];
  loading: boolean;
  failed: boolean;
  restoringId?: string;
  onRestore: (row: BoardTrashRow) => void;
  onClose: () => void;
}) {
  return (
    <Sheet open={open} onOpenChange={(next) => (next ? null : onClose())}>
      <SheetContent side="right" className="w-full sm:max-w-md">
        <SheetHeader>
          <SheetTitle>{labels.title}</SheetTitle>
          <SheetDescription>{labels.body}</SheetDescription>
        </SheetHeader>
        <div className="flex-1 overflow-y-auto px-4 pb-4">
          {loading ? (
            <p role="status" className="text-compact text-muted-foreground">
              {labels.loading}
            </p>
          ) : failed ? (
            <p role="alert" className="text-compact text-destructive">
              {labels.error}
            </p>
          ) : rows.length === 0 ? (
            <p className="text-compact text-muted-foreground">{labels.empty}</p>
          ) : (
            <ul className="flex flex-col gap-1">
              {rows.map((row) => (
                <li
                  key={row.id}
                  className="flex items-center gap-3 rounded-md border border-line px-3 py-2"
                >
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-item-ui font-medium">
                      {row.itemKey ? (
                        <span className="mr-1.5 text-meta-foreground">
                          {row.itemKey}
                        </span>
                      ) : null}
                      {row.name}
                    </p>
                    <p className="text-metadata text-muted-foreground">
                      {row.note ??
                        fillLifecycleCopy(copy.trashUntil, {
                          day: row.restorableUntilDay ?? "",
                        })}
                    </p>
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={restoringId === row.id}
                    onClick={() => onRestore(row)}
                  >
                    {labels.action}
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </SheetContent>
    </Sheet>
  );
}
