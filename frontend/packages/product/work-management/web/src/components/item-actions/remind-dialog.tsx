import { useRef } from "react";
import { Bell, Clock } from "lucide-react";
import { fillLifecycleCopy } from "../item-lifecycle/item-lifecycle.copy";
import {
  ActionBanner,
  ActionButton,
  ActionDialog,
  ImpactLine,
} from "./action-dialog";
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
 * limit: when it refuses, the dialog says when the reminder went out and when
 * the next one is possible. Seeing the item is enough to ask, so viewers get
 * this too. Once sent it cannot be dismissed.
 */
export function RemindDialog({
  copy,
  itemKey,
  recipients,
  state,
  nextRemindAt,
  remindedAt = null,
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
  /** When the person's last reminder went out, already formatted. */
  remindedAt?: string | null;
  open: boolean;
  onCancel: () => void;
  onConfirm: () => void;
  onAssign?: () => void;
}) {
  const nobody = state === "nobody" || recipients.length === 0;
  const busy = state === "busy";
  const tooSoon = state === "tooSoon";
  const failed = state === "error";
  const asking = !nobody && !busy && !tooSoon && !failed;
  const focus = useRef<HTMLButtonElement>(null);
  const who = recipients.join(", ");
  const body = itemKey
    ? fillLifecycleCopy(copy.remindBody, { who, key: itemKey })
    : fillLifecycleCopy(copy.remindBodyNoKey, { who });
  return (
    <ActionDialog
      open={open}
      busy={busy}
      title={nobody ? copy.remindNobody : copy.remindTitle}
      description={
        nobody
          ? copy.remindNobodyBody
          : busy
            ? copy.remindBusyBody
            : copy.remindDesc
      }
      phoneDescription={asking ? body : undefined}
      hideDescription={nobody || tooSoon || failed}
      closeLabel={copy.close}
      maxWidth="sm:max-w-130"
      initialFocus={focus}
      footer={
        <>
          {busy ? null : (
            <ActionButton
              buttonRef={tooSoon || (nobody && !onAssign) ? focus : undefined}
              onClick={onCancel}
            >
              {tooSoon || nobody ? copy.close : copy.cancel}
            </ActionButton>
          )}
          {nobody && onAssign ? (
            <ActionButton primary buttonRef={focus} onClick={onAssign}>
              {copy.remindAssign}
            </ActionButton>
          ) : null}
          {!nobody && !tooSoon ? (
            <ActionButton
              primary
              buttonRef={focus}
              disabled={busy}
              onClick={onConfirm}
            >
              {busy ? copy.remindBusy : failed ? copy.retry : copy.remindSend}
            </ActionButton>
          ) : null}
        </>
      }
      onCancel={onCancel}
    >
      {asking ? (
        <div className="px-[22px] pt-2.5 max-sm:hidden">
          <ul className="m-0 flex list-none flex-col rounded-[10px] border border-line p-0">
            <ImpactLine
              icon={<Bell aria-hidden="true" className="size-4" />}
              lead={fillLifecycleCopy(copy.remindWho, { who })}
              note={
                itemKey
                  ? fillLifecycleCopy(copy.remindWhoNote, { key: itemKey })
                  : copy.remindWhoNoteNoKey
              }
            />
            <ImpactLine
              icon={<Clock aria-hidden="true" className="size-4" />}
              lead={copy.remindLimit}
              note={copy.remindLimitNote}
            />
          </ul>
        </div>
      ) : null}
      {tooSoon || nobody || failed ? (
        <div className="px-[22px] pt-2.5 max-sm:px-0 max-sm:pt-0">
          <ActionBanner
            tone={failed ? "err" : tooSoon ? "warn" : "info"}
            className="max-sm:mx-4 max-sm:mb-2"
          >
            {failed
              ? copy.remindFailed
              : tooSoon
                ? remindedAt && nextRemindAt
                  ? fillLifecycleCopy(copy.remindTooSoon, {
                      at: remindedAt,
                      when: nextRemindAt,
                    })
                  : copy.remindTooSoonNow
                : copy.remindNobodyBody}
          </ActionBanner>
        </div>
      ) : null}
    </ActionDialog>
  );
}
