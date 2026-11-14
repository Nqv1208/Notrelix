import { useState } from "react";
import { MoreHorizontal } from "lucide-react";
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@notrelix/ui-web";
import { fillBoardHeaderCopy, type BoardHeaderCopy } from "./board-header.copy";
import type { ViewTabSavedView } from "./view-tabs";

/** How a write on a view ended: a stale version names the other person's save first. */
export type ViewWriteOutcome = "ok" | "conflict" | "failed";

/** What the tab strip may do to the saved views; absent for a person who may not edit them. */
export interface ViewTabActions {
  readonly archivedViews: readonly ViewTabSavedView[];
  readonly onSetDefault: (viewId: string) => Promise<ViewWriteOutcome>;
  /** `nextDefaultId` is named when the view is the default. */
  readonly onArchive: (
    viewId: string,
    nextDefaultId?: string,
  ) => Promise<ViewWriteOutcome>;
  readonly onDelete: (
    viewId: string,
    nextDefaultId?: string,
  ) => Promise<ViewWriteOutcome>;
  readonly onRestore: (viewId: string) => Promise<ViewWriteOutcome>;
}

type Confirm = "archive" | "delete";

/**
 * The menu of the opened saved view: set as default, archive and delete, and the
 * archived views to restore. Deleting or archiving the default view asks for the
 * view that takes its place first, so the board never has two or none.
 */
export function ViewTabMenu({
  view,
  savedViews,
  actions,
  copy,
  tablet = false,
}: {
  view: ViewTabSavedView;
  savedViews: readonly ViewTabSavedView[];
  actions: ViewTabActions;
  copy: BoardHeaderCopy;
  /** The 44 px button of the tablet toolbar. */
  tablet?: boolean;
}) {
  const t = copy.viewMenu;
  const [confirm, setConfirm] = useState<Confirm | null>(null);
  const [nextDefaultId, setNextDefaultId] = useState("");
  const [working, setWorking] = useState(false);
  const [failure, setFailure] = useState<ViewWriteOutcome | null>(null);
  // Only a shared view can take the default; a private one is its creator's alone.
  const successors = savedViews.filter(
    (entry) => entry.id !== view.id && entry.visibility !== "me",
  );
  const isShared = view.visibility !== "me";
  const lastShared = view.isDefault === true && successors.length === 0;
  const canDefault = isShared && view.isDefault !== true;
  const defaultReason = !isShared
    ? t.privateCannotDefault
    : view.isDefault
      ? t.isDefault
      : null;

  function ask(kind: Confirm) {
    setFailure(null);
    setNextDefaultId(successors[0]?.id ?? "");
    setConfirm(kind);
  }

  async function run(send: () => Promise<ViewWriteOutcome>) {
    setWorking(true);
    const outcome = await send();
    setWorking(false);
    if (outcome === "ok") setConfirm(null);
    else setFailure(outcome);
  }

  const next = view.isDefault ? nextDefaultId || undefined : undefined;
  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={fillBoardHeaderCopy(t.open, { name: view.name })}
            className={
              tablet
                ? "size-11 rounded-[10px] text-muted-foreground hover:text-foreground"
                : "-mb-px h-9 w-10 rounded-none text-muted-foreground hover:text-foreground max-md:h-11 max-md:w-11"
            }
          >
            <MoreHorizontal aria-hidden="true" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-64">
          <DropdownMenuItem
            disabled={!canDefault}
            onSelect={() => void actions.onSetDefault(view.id)}
          >
            {t.makeDefault}
          </DropdownMenuItem>
          {defaultReason ? (
            <DropdownMenuLabel className="pt-0 text-[12.5px] font-normal text-muted-foreground">
              {defaultReason}
            </DropdownMenuLabel>
          ) : null}
          <DropdownMenuItem
            disabled={lastShared}
            onSelect={() => ask("archive")}
          >
            {t.archive}
          </DropdownMenuItem>
          <DropdownMenuItem
            disabled={lastShared}
            variant="destructive"
            onSelect={() => ask("delete")}
          >
            {t.delete}
          </DropdownMenuItem>
          {lastShared ? (
            <DropdownMenuLabel className="pt-0 text-[12.5px] font-normal text-muted-foreground">
              {t.onlyShared}
            </DropdownMenuLabel>
          ) : null}
          {actions.archivedViews.length > 0 ? (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuLabel>{t.archived}</DropdownMenuLabel>
              {actions.archivedViews.map((archived) => (
                <DropdownMenuItem
                  key={archived.id}
                  onSelect={() => void actions.onRestore(archived.id)}
                >
                  {fillBoardHeaderCopy(t.restore, { name: archived.name })}
                </DropdownMenuItem>
              ))}
            </>
          ) : null}
        </DropdownMenuContent>
      </DropdownMenu>
      <AlertDialog
        open={confirm !== null}
        onOpenChange={(open) => {
          if (!open && !working) setConfirm(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {fillBoardHeaderCopy(
                confirm === "delete" ? t.deleteTitle : t.archiveTitle,
                { name: view.name },
              )}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {confirm === "delete" ? t.deleteBody : t.archiveBody}
            </AlertDialogDescription>
          </AlertDialogHeader>
          {view.isDefault ? (
            <div className="flex flex-col gap-2">
              <p className="m-0 text-[13px] text-meta-foreground">
                {fillBoardHeaderCopy(t.nextDefaultHint, { name: view.name })}
              </p>
              <label
                htmlFor="view-next-default"
                className="text-[13px] font-medium"
              >
                {t.nextDefault}
              </label>
              <Select value={nextDefaultId} onValueChange={setNextDefaultId}>
                <SelectTrigger
                  id="view-next-default"
                  className="w-full max-sm:min-h-11"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {successors.map((entry) => (
                    <SelectItem key={entry.id} value={entry.id}>
                      {entry.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          ) : null}
          {failure ? (
            <p role="alert" className="m-0 text-[13.5px] text-danger-text">
              {failure === "conflict" ? t.conflict : t.failed}
            </p>
          ) : null}
          <AlertDialogFooter>
            <AlertDialogCancel disabled={working} className="max-sm:min-h-11">
              {t.keep}
            </AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              className="max-sm:min-h-11"
              disabled={working || (view.isDefault === true && !nextDefaultId)}
              onClick={(event) => {
                event.preventDefault();
                void run(() =>
                  confirm === "delete"
                    ? actions.onDelete(view.id, next)
                    : actions.onArchive(view.id, next),
                );
              }}
            >
              {working
                ? t.working
                : confirm === "delete"
                  ? t.delete.replace("…", "")
                  : t.archive.replace("…", "")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
