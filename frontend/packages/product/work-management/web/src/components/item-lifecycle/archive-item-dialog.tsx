import {
  CornerDownRight,
  Link2,
  Archive,
  TriangleAlert,
  Loader2,
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
const iconClass = "mt-0.5 size-4 shrink-0 text-meta-foreground";

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
        className={cn(
          "gap-0 overflow-hidden rounded-[14px] p-0 sm:max-w-110",
          "max-sm:top-auto max-sm:bottom-0 max-sm:max-w-full max-sm:translate-y-0 max-sm:rounded-b-none max-sm:rounded-t-2xl",
        )}
        onEscapeKeyDown={(event) => {
          if (busy) event.preventDefault();
        }}
        onInteractOutside={(event) => {
          if (busy) event.preventDefault();
        }}
      >
        <DialogHeader className="flex-row items-center gap-2.5 px-5 pt-4 pb-2">
          <DialogTitle className="min-w-0 flex-1 text-[17px] leading-snug font-semibold">
            {copy.archiveTitle}
          </DialogTitle>
          {busy ? null : (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className="max-sm:size-11"
              aria-label={copy.archiveClose}
              title={copy.archiveClose}
              onClick={onCancel}
            >
              <span aria-hidden="true" className="text-lg leading-none">
                ×
              </span>
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
              className="flex items-start gap-2 rounded-lg bg-destructive/10 px-3 py-2.5 text-destructive"
            >
              <TriangleAlert
                aria-hidden="true"
                className="mt-0.5 size-4 shrink-0"
              />
              <span className="flex flex-col gap-0.5">
                <b>{refusal.title}</b>
                <span>{refusal.body}</span>
              </span>
            </div>
          ) : null}
        </div>
        <DialogFooter className="items-center gap-2 border-t border-line px-5 py-3 max-sm:flex-col-reverse max-sm:items-stretch">
          <Button
            type="button"
            variant="outline"
            disabled={busy}
            className="max-sm:h-11"
            onClick={onCancel}
          >
            {blocked ? copy.archiveClose : copy.archiveCancel}
          </Button>
          {blocked ? null : (
            <Button
              type="button"
              disabled={busy || impact.checking}
              className="max-sm:h-11"
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
