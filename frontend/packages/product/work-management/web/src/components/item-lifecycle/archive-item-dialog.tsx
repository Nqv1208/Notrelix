import { useRef } from "react";
import {
  CornerDownRight,
  Link2,
  Archive,
  TriangleAlert,
  Loader2,
  X,
} from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import {
  fillLifecycleCopy,
  pluralLifecycleCopy,
  type ItemLifecycleCopy,
} from "./item-lifecycle.copy";

/** Where an archive stands: asking, writing, or refused for a reason the dialog names. */
export type ArchiveDialogStatus =
  "idle" | "busy" | "error" | "conflict" | "denied" | "gone";

export interface ArchiveImpact {
  /** Sub-items that are archived with the item. */
  readonly subItems: number;
  /** Dependency links the item has, with the keys of the items on the other end. */
  readonly dependencies: {
    readonly count: number;
    readonly keys: readonly string[];
  };
  /** The links are still being read: the dependency count is not known yet. */
  readonly checking: boolean;
}

const lineClass = "flex items-start gap-2.5";
const iconClass = "size-4 shrink-0";
// The mockup's `.btn.l`: 13 px, 7 px radius, 14 px side padding; 44 px and 14 px type on a
// phone. While the write is out both buttons go flat grey at once (no colour transition, so a measure never catches one half way).
const buttonClass =
  "rounded-[7px] px-3.5 text-compact leading-[1.45] max-sm:h-11 max-sm:text-sm disabled:border-line-strong disabled:bg-state-hover-strong disabled:font-medium disabled:text-meta-foreground disabled:opacity-100 disabled:transition-none dark:disabled:border-line-strong dark:disabled:bg-state-hover-strong";
const cancelClass =
  "border-line-strong bg-popover font-medium shadow-none dark:border-line-strong dark:bg-card";

/**
 * Asks before an item is archived and names what it takes along: its
 * sub-items, and the dependency links that stay (shown as archived). Nothing
 * is lost; the item can be unarchived. While the write is out the dialog
 * cannot be dismissed; a refusal is explained in place, with Try again when
 * trying again can help.
 */
export function ArchiveItemDialog({
  copy,
  title,
  impact,
  status,
  open,
  onCancel,
  onConfirm,
}: {
  copy: ItemLifecycleCopy;
  title: string;
  impact: ArchiveImpact;
  status: ArchiveDialogStatus;
  open: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  const busy = status === "busy";
  const cancelRef = useRef<HTMLButtonElement>(null);
  const total = 1 + impact.subItems;
  const refusal = refusalCopy(copy, status);
  const canRetry = status === "error" || status === "conflict";
  const blocked = status === "denied" || status === "gone";
  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next && !busy) onCancel();
      }}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        className={cn(
          "box-border gap-0 overflow-hidden rounded-[14px] border border-line bg-popover p-0 leading-[1.45] shadow-popover ring-0 sm:max-w-110 dark:bg-card",
          "max-sm:top-auto max-sm:bottom-0 max-sm:max-w-full max-sm:translate-y-0 max-sm:rounded-b-none max-sm:rounded-t-[16px]",
        )}
        // The safe choice has focus, so Enter does not archive by accident.
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          cancelRef.current?.focus();
        }}
        onEscapeKeyDown={(event) => {
          if (busy) event.preventDefault();
        }}
        onInteractOutside={(event) => {
          if (busy) event.preventDefault();
        }}
      >
        <span
          aria-hidden="true"
          className="mx-auto mt-2 hidden h-1 w-9 rounded-full bg-line-strong max-sm:block"
        />
        <DialogHeader className="flex-row items-center gap-2.5 px-5 pt-4 pb-2">
          <DialogTitle className="min-w-0 flex-1 text-[17px] leading-[1.45] font-[650]">
            {copy.archiveTitle}
          </DialogTitle>
          {busy ? null : (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className="rounded-[7px] text-muted-foreground max-sm:size-11"
              aria-label={copy.archiveClose}
              title={copy.archiveClose}
              onClick={onCancel}
            >
              <X aria-hidden="true" className="size-4" strokeWidth={1.8} />
            </Button>
          )}
        </DialogHeader>
        <div className="flex flex-col gap-3.5 px-5 pt-2 pb-4 text-[13.5px]">
          <DialogDescription className="text-[13.5px] text-foreground">
            {fillLifecycleCopy(copy.archiveBody, { title })}
          </DialogDescription>
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {impact.subItems > 0 ? (
              <li className={lineClass}>
                <CornerDownRight aria-hidden="true" className={iconClass} />
                <span>
                  <b>
                    {pluralLifecycleCopy(
                      {
                        one: copy.archiveSubItemsLead_one,
                        other: copy.archiveSubItemsLead_other,
                      },
                      impact.subItems,
                    )}
                  </b>
                  {impact.subItems === 1
                    ? copy.archiveSubItemsTail_one
                    : copy.archiveSubItemsTail_other}
                </span>
              </li>
            ) : null}
            {impact.dependencies.count > 0 ? (
              <li className={lineClass}>
                <Link2 aria-hidden="true" className={iconClass} />
                <span>
                  <b>
                    {pluralLifecycleCopy(
                      {
                        one: copy.archiveLinksLead_one,
                        other: copy.archiveLinksLead_other,
                      },
                      impact.dependencies.count,
                    )}
                  </b>
                  {fillLifecycleCopy(copy.archiveLinksTail, {
                    keys: impact.dependencies.keys.join(", "),
                  })}
                </span>
              </li>
            ) : null}
            <li className={lineClass}>
              <Archive aria-hidden="true" className={iconClass} />
              <span>{copy.archiveRestore}</span>
            </li>
          </ul>
          {refusal ? (
            <div
              role="alert"
              className="flex items-start gap-2.5 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-[13px] leading-[1.4] text-danger-text dark:bg-danger-text/14"
            >
              <TriangleAlert
                aria-hidden="true"
                className="mt-0.5 size-4 shrink-0"
              />
              <span className="flex min-w-0 flex-1 flex-col gap-px">
                <b className="font-semibold text-foreground">{refusal.title}</b>
                <span>{refusal.body}</span>
              </span>
            </div>
          ) : null}
        </div>
        <DialogFooter className="items-center gap-2 border-t border-line px-5 py-3 max-sm:flex-col-reverse max-sm:items-stretch">
          <Button
            ref={cancelRef}
            type="button"
            variant="outline"
            disabled={busy}
            className={cn(buttonClass, cancelClass, busy && "sm:h-8")}
            onClick={onCancel}
          >
            {blocked ? copy.archiveClose : copy.archiveCancel}
          </Button>
          {blocked ? null : (
            <Button
              type="button"
              disabled={busy || impact.checking}
              className={cn(
                buttonClass,
                "border-primary font-semibold",
                // The artboard draws the pending buttons flat and 32 px tall.
                busy && "font-medium sm:h-8",
              )}
              onClick={onConfirm}
            >
              {busy ? (
                <>
                  <Loader2 aria-hidden="true" className="animate-spin" />
                  {copy.archiveBusy}
                </>
              ) : impact.checking ? (
                copy.archiveChecking
              ) : canRetry ? (
                copy.archiveRetry
              ) : total > 1 ? (
                fillLifecycleCopy(copy.archiveConfirmMany, { count: total })
              ) : (
                copy.archiveConfirm
              )}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function refusalCopy(
  copy: ItemLifecycleCopy,
  status: ArchiveDialogStatus,
): { title: string; body: string } | null {
  switch (status) {
    case "error":
      return { title: copy.archiveErrorTitle, body: copy.archiveErrorBody };
    case "conflict":
      return {
        title: copy.archiveConflictTitle,
        body: copy.archiveConflictBody,
      };
    case "denied":
      return { title: copy.archiveDeniedTitle, body: copy.archiveDeniedBody };
    case "gone":
      return { title: copy.archiveGoneTitle, body: copy.archiveGoneBody };
    default:
      return null;
  }
}
