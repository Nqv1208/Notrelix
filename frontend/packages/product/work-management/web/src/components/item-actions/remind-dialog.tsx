import { Info, TriangleAlert } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@notrelix/ui-web";
import { fillLifecycleCopy } from "../item-lifecycle/item-lifecycle.copy";
import type { ItemActionsCopy } from "./item-actions.copy";

export type RemindDialogState =
  | "confirm"
  | "busy"
  | "error"
  /** The server refused: already reminded within 24 hours. */
  | "tooSoon"
  /** Nobody else is in charge. */
  | "nobody";

/**
 * Confirms a reminder, naming who will get it. The server decides the 24 hour
 * limit: when it refuses, the dialog says when the next reminder is possible.
 * Seeing the item is enough to ask, so viewers get this too.
 */
export function RemindDialog({
  copy,
  itemKey,
  recipients,
  state,
  nextRemindAt,
  open,
  onCancel,
  onConfirm,
  onAssign,
}: {
  copy: ItemActionsCopy;
  itemKey: string | null;
  /** Names of the people in charge, the caller excluded. */
  recipients: readonly string[];
  state: RemindDialogState;
  /** When the next reminder is possible, already formatted for the person. */
  nextRemindAt: string | null;
  open: boolean;
  onCancel: () => void;
  onConfirm: () => void;
  onAssign?: () => void;
}) {
  const nobody = state === "nobody" || recipients.length === 0;
  const busy = state === "busy";
  const who = recipients.join(", ");
  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next && !busy) onCancel();
      }}
    >
      <DialogContent
        className="sm:max-w-[520px]"
        showCloseButton={!busy}
        onEscapeKeyDown={(event) => {
          if (busy) event.preventDefault();
        }}
        onInteractOutside={(event) => {
          if (busy) event.preventDefault();
        }}
      >
        <DialogHeader>
          <DialogTitle>
            {nobody ? copy.remindNobody : copy.remindTitle}
          </DialogTitle>
          <DialogDescription>
            {nobody
              ? copy.remindNobodyBody
              : itemKey
                ? fillLifecycleCopy(copy.remindBody, { who, key: itemKey })
                : fillLifecycleCopy(copy.remindBodyNoKey, { who })}
          </DialogDescription>
        </DialogHeader>
        {state === "tooSoon" ? (
          <p
            role="alert"
            className="flex items-start gap-2 rounded-lg bg-warning-subtle px-2.5 py-2 text-compact text-warning-subtle-foreground"
          >
            <Info aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
            {nextRemindAt
              ? fillLifecycleCopy(copy.remindTooSoon, { when: nextRemindAt })
              : copy.remindTooSoonNow}
          </p>
        ) : null}
        {state === "error" ? (
          <p
            role="alert"
            className="flex items-start gap-2 rounded-lg bg-destructive/10 px-2.5 py-2 text-compact text-destructive"
          >
            <TriangleAlert
              aria-hidden="true"
              className="mt-0.5 size-3.5 shrink-0"
            />
            {copy.remindFailed}
          </p>
        ) : null}
        <DialogFooter>
          {!busy ? (
            <Button type="button" variant="ghost" onClick={onCancel}>
              {state === "tooSoon" || nobody ? copy.close : copy.cancel}
            </Button>
          ) : null}
          {nobody && onAssign ? (
            <Button type="button" onClick={onAssign}>
              {copy.remindAssign}
            </Button>
          ) : null}
          {!nobody && state !== "tooSoon" ? (
            <Button type="button" disabled={busy} onClick={onConfirm}>
              {busy
                ? copy.remindBusy
                : state === "error"
                  ? copy.retry
                  : copy.remindSend}
            </Button>
          ) : null}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
