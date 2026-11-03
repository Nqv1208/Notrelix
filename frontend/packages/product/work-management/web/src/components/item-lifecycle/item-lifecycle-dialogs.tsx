import { useRef } from "react";
import { CornerDownRight, Link2, TriangleAlert, X } from "lucide-react";
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

/** What deleting takes along: the sub-items and the dependency links (with the keys at the other end). */
export interface DeleteImpact {
  readonly subItems: number;
  readonly dependencies: {
    readonly count: number;
    readonly keys: readonly string[];
  };
}

/** Where a delete stands: asking, writing (cannot be dismissed), or refused (try again). */
export type DeleteDialogStatus = "idle" | "busy" | "error";

// The WM-Actions-Trash `.btn`: 30 px (48 px on a phone), 7 px radius, 13 px type.
const buttonClass =
  "h-[30px] rounded-[7px] px-[11px] text-[13px] leading-[1.45] max-sm:h-12 max-sm:rounded-xl max-sm:text-[15px] max-sm:w-full";
const dangerClass =
  "border-danger bg-danger font-semibold text-danger-foreground hover:bg-danger/90 aria-disabled:pointer-events-none aria-disabled:opacity-50";

/**
 * Asks before an item is deleted and names what goes with it: its sub-items
 * and the dependency links that are hidden until it is restored. The item
 * can be restored from the board's deleted items for 30 days. While the write
 * is out the dialog cannot be dismissed; a failure is explained in place with
 * Try again. On a phone it is a bottom sheet.
 */
export function DeleteItemDialog({
  copy,
  impact,
  title,
  status = "idle",
  open,
  onCancel,
  onConfirm,
}: {
  copy: ItemLifecycleCopy;
  /** The sub-items and dependency links that go with the item; each line shows only when there are some. */
  impact?: DeleteImpact;
  title: string;
  status?: DeleteDialogStatus;
  open: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  const busy = status === "busy";
  const failed = status === "error";
  const confirmRef = useRef<HTMLButtonElement>(null);
  const subItems = impact?.subItems ?? 0;
  const links = impact?.dependencies;
  const hasImpact = subItems > 0 || (links?.count ?? 0) > 0;
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
          "box-border gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 leading-[1.45] shadow-[0_24px_60px_rgba(0,0,0,.28)] ring-0 sm:max-w-135 dark:bg-card dark:shadow-[0_24px_60px_rgba(0,0,0,.6)]",
          "max-sm:top-auto max-sm:bottom-0 max-sm:max-w-full max-sm:translate-y-0 max-sm:rounded-b-none max-sm:rounded-t-[14px] max-sm:pb-6",
        )}
        // The drawn focus is on the delete button; Esc cancels.
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          confirmRef.current?.focus();
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
          className="mx-auto mt-1.5 mb-0.5 hidden h-[5px] w-9 rounded-[3px] bg-line-strong max-sm:block"
        />
        <DialogHeader className="relative gap-0 pt-5 pr-[52px] pb-1.5 pl-[22px] text-left max-sm:flex max-sm:min-h-13 max-sm:flex-row max-sm:flex-wrap max-sm:items-center max-sm:gap-x-2 max-sm:p-0 max-sm:pr-2 max-sm:pl-5">
          <DialogTitle className="text-[18px] leading-[1.45] font-[650] max-sm:min-w-0 max-sm:flex-1 max-sm:text-[17px]">
            {fillLifecycleCopy(copy.deleteTitle, { title })}
          </DialogTitle>
          <DialogDescription
            className={cn(
              "mt-1.5 text-[13.5px] text-muted-foreground max-sm:order-last max-sm:mt-0 max-sm:basis-full max-sm:py-0.5 max-sm:pr-3 max-sm:pb-2 max-sm:text-sm",
              failed && "sr-only",
            )}
          >
            {busy ? copy.deleteBusyBody : copy.deleteBody}
            {subItems > 0 ? (
              <span className="sm:hidden">
                {" "}
                {pluralLifecycleCopy(
                  {
                    one: copy.deleteSubItemsShort_one,
                    other: copy.deleteSubItemsShort_other,
                  },
                  subItems,
                )}
              </span>
            ) : null}
          </DialogDescription>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={copy.deleteClose}
            title={copy.deleteClose}
            aria-disabled={busy || undefined}
            className={cn(
              "absolute top-3.5 right-3 size-7 rounded-[7px] text-meta-foreground max-sm:static max-sm:size-11 max-sm:text-foreground",
              busy && "opacity-40",
            )}
            onClick={busy ? undefined : onCancel}
          >
            <X aria-hidden="true" className="size-4" strokeWidth={1.8} />
          </Button>
        </DialogHeader>
        {failed ? (
          <div className="px-[22px] pt-2.5 max-sm:px-0 max-sm:pt-0">
            <div
              role="alert"
              className="flex items-start gap-2 rounded-lg bg-danger-subtle px-2.5 py-2 text-[12.5px] leading-[1.45] text-danger-text max-sm:mx-4 max-sm:mb-2 max-sm:text-[13.5px]"
            >
              <TriangleAlert
                aria-hidden="true"
                className="mt-0.5 size-3.5 shrink-0"
              />
              <span>{copy.deleteErrorBody}</span>
            </div>
          </div>
        ) : hasImpact && !busy ? (
          <div className="px-[22px] pt-2.5 max-sm:hidden">
            <ul className="m-0 flex list-none flex-col rounded-[10px] border border-line p-0">
              {subItems > 0 ? (
                <ImpactLine
                  icon={
                    <CornerDownRight aria-hidden="true" className="size-4" />
                  }
                  lead={pluralLifecycleCopy(
                    {
                      one: copy.deleteSubItemsLead_one,
                      other: copy.deleteSubItemsLead_other,
                    },
                    subItems,
                  )}
                  note={copy.deleteSubItemsNote}
                />
              ) : null}
              {links && links.count > 0 ? (
                <ImpactLine
                  warn
                  icon={<Link2 aria-hidden="true" className="size-4" />}
                  lead={fillLifecycleCopy(
                    links.count === 1
                      ? copy.deleteLinksLead_one
                      : copy.deleteLinksLead_other,
                    {
                      count: links.count,
                      keys: links.keys.join(", ") || String(links.count),
                    },
                  )}
                  note={copy.deleteLinksNote}
                />
              ) : null}
            </ul>
          </div>
        ) : null}
        <DialogFooter className="flex-row items-center gap-2 px-[22px] pt-4 pb-[18px] max-sm:flex-col-reverse max-sm:items-stretch max-sm:px-4 max-sm:pt-2 max-sm:pb-0">
          <span className="min-w-35 flex-1 text-xs leading-[1.45] text-meta-foreground max-sm:hidden">
            {busy || failed ? "" : copy.deleteHint}
          </span>
          {busy ? null : (
            <Button
              type="button"
              variant="outline"
              className={cn(
                buttonClass,
                "border-line-strong bg-popover font-medium shadow-none dark:border-line-strong dark:bg-card",
              )}
              onClick={onCancel}
            >
              {copy.deleteCancel}
            </Button>
          )}
          <Button
            ref={confirmRef}
            type="button"
            aria-disabled={busy || undefined}
            className={cn(buttonClass, dangerClass)}
            onClick={busy ? undefined : onConfirm}
          >
            {busy
              ? copy.deleteBusy
              : failed
                ? copy.deleteRetry
                : copy.deleteConfirm}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function ImpactLine({
  icon,
  lead,
  note,
  warn = false,
}: {
  icon: React.ReactNode;
  lead: string;
  note: string;
  warn?: boolean;
}) {
  return (
    <li className="flex gap-[11px] border-t border-line px-3.5 py-2.5 text-[13px] first:border-t-0">
      <span
        aria-hidden="true"
        className={cn(
          "box-border grid size-[30px] flex-none place-items-center rounded-[7px] border",
          warn
            ? "border-transparent bg-warning-subtle text-warning-text"
            : "border-line bg-background text-muted-foreground",
        )}
      >
        {icon}
      </span>
      <span className="min-w-0">
        <b className="block font-semibold">{lead}</b>
        <small className="block text-[12.5px] text-meta-foreground">
          {note}
        </small>
      </span>
    </li>
  );
}
