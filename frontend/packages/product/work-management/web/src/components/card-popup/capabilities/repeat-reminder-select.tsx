import { useId, useState } from "react";
import { Bell, Repeat } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@notrelix/ui-web";
import {
  CARD_REMINDER_DEFAULT,
  CARD_REMINDER_OFFSETS,
  CARD_REPEAT_RULES,
  type CardDetail,
  type CardRepeatRule,
} from "@notrelix/work-management-core";
import { useItemSettingsWrites } from "@notrelix/work-management-state";
import { useCapabilitiesCopy } from "./use-capabilities-copy";

const NONE = "none";

/** The select button of the dates panel: `.in` with its icon and chevron (36 px, 8 px radius). */
const TRIGGER =
  "box-border h-11 w-full justify-start data-[size=default]:h-11 md:data-[size=default]:h-9 md:h-9 gap-2 rounded-[8px] border border-line-strong bg-popover px-3 text-[14px] text-foreground [&>svg:last-child]:ml-auto [&>svg:last-child]:size-[13px]";

function scopeOf(card: CardDetail) {
  return {
    itemId: card.id,
    boardId: card.boardId,
    workspaceId: card.workspaceId,
  };
}

/** "Lặp lại": how the item repeats. Completing a repeating item reopens it at the next due date. */
export function RepeatSelect({
  card,
  disabled = false,
}: {
  card: CardDetail;
  disabled?: boolean;
}) {
  const copy = useCapabilitiesCopy();
  const id = useId();
  const writes = useItemSettingsWrites(scopeOf(card), card.version ?? 0);
  const [error, setError] = useState<string | null>(null);
  const hasDue = Boolean(card.dueAt ?? card.dueDate);
  return (
    <div className="mt-3 flex flex-col gap-1.5">
      <label
        htmlFor={id}
        className="text-[12px] leading-[18px] font-semibold text-muted-foreground"
      >
        {copy.repeat.label}
      </label>
      <Select
        value={card.repeatRule ?? NONE}
        disabled={disabled || writes.pending || (!hasDue && !card.repeatRule)}
        onValueChange={(value) => {
          setError(null);
          writes
            .setRepeat(value === NONE ? null : (value as CardRepeatRule))
            .catch(() => setError(copy.repeat.failed));
        }}
      >
        <SelectTrigger id={id} className={TRIGGER}>
          <Repeat aria-hidden="true" className="size-4 shrink-0" />
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={NONE}>{copy.repeat.none}</SelectItem>
          {CARD_REPEAT_RULES.map((rule) => (
            <SelectItem key={rule} value={rule}>
              {copy.repeat[rule]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {!hasDue && !card.repeatRule ? (
        <p className="text-micro text-meta-foreground">
          {copy.repeat.needsDue}
        </p>
      ) : null}
      {error ? (
        <p role="alert" className="text-micro text-danger-subtle-foreground">
          {error}
        </p>
      ) : null}
    </div>
  );
}

function reminderLabel(
  copy: ReturnType<typeof useCapabilitiesCopy>,
  offset: (typeof CARD_REMINDER_OFFSETS)[number],
): string {
  return offset === -1 ? copy.reminder.off : copy.reminder[offset];
}

/** "Nhắc trước hạn": one reminder per item, sent to everyone assigned and everyone following. */
export function ReminderSelect({
  card,
  disabled = false,
}: {
  card: CardDetail;
  disabled?: boolean;
}) {
  const copy = useCapabilitiesCopy();
  const id = useId();
  const writes = useItemSettingsWrites(scopeOf(card), card.version ?? 0);
  const [error, setError] = useState<string | null>(null);
  const current = card.reminderOffsetMinutes ?? CARD_REMINDER_DEFAULT;
  return (
    <>
      <div className="mt-3 flex flex-col gap-1.5">
        <label
          htmlFor={id}
          className="text-[12px] leading-[18px] font-semibold text-muted-foreground"
        >
          {copy.reminder.label}
        </label>
        <Select
          value={String(current)}
          disabled={disabled || writes.pending}
          onValueChange={(value) => {
            setError(null);
            writes
              .setReminder(Number(value))
              .catch(() => setError(copy.reminder.failed));
          }}
        >
          <SelectTrigger id={id} className={TRIGGER}>
            <Bell aria-hidden="true" className="size-4 shrink-0" />
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {CARD_REMINDER_OFFSETS.map((offset) => (
              <SelectItem key={offset} value={String(offset)}>
                {reminderLabel(copy, offset)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {error ? (
          <p role="alert" className="text-micro text-danger-subtle-foreground">
            {error}
          </p>
        ) : null}
      </div>
      <p className="m-0 mt-2 text-[12.5px] leading-[1.45] text-meta-foreground">
        {copy.reminder.note}
      </p>
    </>
  );
}
