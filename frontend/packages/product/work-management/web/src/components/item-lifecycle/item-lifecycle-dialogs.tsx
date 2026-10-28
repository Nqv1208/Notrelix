import { useState } from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  cn,
} from "@notrelix/ui-web";
import {
  fillLifecycleCopy,
  type ItemLifecycleCopy,
} from "./item-lifecycle.copy";

/** Asks before an item is deleted; the item can be restored from the board's trash for 30 days. */
export function DeleteItemDialog({
  copy,
  title,
  open,
  onCancel,
  onConfirm,
}: {
  copy: ItemLifecycleCopy;
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
          <AlertDialogDescription>{copy.deleteBody}</AlertDialogDescription>
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

export interface MoveTargetBoard {
  readonly id: string;
  readonly title: string;
}

/** Picks the board an item moves to; only boards where the member may add items are offered. */
export function MoveItemDialog({
  copy,
  title,
  boards,
  open,
  onCancel,
  onConfirm,
}: {
  copy: ItemLifecycleCopy;
  title: string;
  boards: readonly MoveTargetBoard[];
  open: boolean;
  onCancel: () => void;
  onConfirm: (boardId: string) => void;
}) {
  const [query, setQuery] = useState("");
  const [chosen, setChosen] = useState<string | null>(null);
  const needle = query.trim().toLowerCase();
  const shown = boards.filter((board) =>
    board.title.toLowerCase().includes(needle),
  );
  const close = () => {
    setQuery("");
    setChosen(null);
    onCancel();
  };
  return (
    <Dialog open={open} onOpenChange={(next) => (next ? null : close())}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {fillLifecycleCopy(copy.moveTitle, { title })}
          </DialogTitle>
          <DialogDescription>{copy.moveBody}</DialogDescription>
        </DialogHeader>
        {boards.length === 0 ? (
          <p className="text-compact text-muted-foreground">{copy.moveEmpty}</p>
        ) : (
          <>
            <Input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder={copy.moveSearch}
              aria-label={copy.moveSearch}
            />
            <ul
              role="listbox"
              aria-label={copy.moveToBoard}
              className="max-h-60 overflow-y-auto"
            >
              {shown.map((board) => (
                <li key={board.id} role="presentation">
                  <button
                    type="button"
                    role="option"
                    aria-selected={chosen === board.id}
                    onClick={() => setChosen(board.id)}
                    className={cn(
                      "flex w-full cursor-pointer items-center rounded-md px-2.5 py-2 text-left text-compact hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
                      chosen === board.id &&
                        "bg-brand-subtle font-semibold text-brand-subtle-foreground",
                    )}
                  >
                    {board.title}
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}
        <DialogFooter>
          <Button type="button" variant="ghost" onClick={close}>
            {copy.moveCancel}
          </Button>
          <Button
            type="button"
            disabled={!chosen}
            onClick={() => {
              if (!chosen) return;
              const id = chosen;
              setQuery("");
              setChosen(null);
              onConfirm(id);
            }}
          >
            {copy.moveConfirm}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
