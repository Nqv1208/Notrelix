import {
  Button,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@notrelix/ui-web";
import {
  fillLifecycleCopy,
  type ItemLifecycleCopy,
} from "./item-lifecycle.copy";

export interface BoardTrashRow {
  readonly id: string;
  readonly name: string;
  readonly itemKey: string | null;
  /** `YYYY-MM-DD` of the last day the item can be restored. */
  readonly restorableUntilDay: string;
}

/** The board's deleted items with the way back; shown from the board menu. */
export function BoardTrashSheet({
  copy,
  open,
  rows,
  loading,
  failed,
  restoringId,
  onRestore,
  onClose,
}: {
  copy: ItemLifecycleCopy;
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
          <SheetTitle>{copy.trashTitle}</SheetTitle>
          <SheetDescription>{copy.trashBody}</SheetDescription>
        </SheetHeader>
        <div className="flex-1 overflow-y-auto px-4 pb-4">
          {loading ? (
            <p role="status" className="text-compact text-muted-foreground">
              {copy.trashLoading}
            </p>
          ) : failed ? (
            <p role="alert" className="text-compact text-destructive">
              {copy.trashError}
            </p>
          ) : rows.length === 0 ? (
            <p className="text-compact text-muted-foreground">
              {copy.trashEmpty}
            </p>
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
                      {fillLifecycleCopy(copy.trashUntil, {
                        day: row.restorableUntilDay,
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
                    {copy.trashRestore}
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
