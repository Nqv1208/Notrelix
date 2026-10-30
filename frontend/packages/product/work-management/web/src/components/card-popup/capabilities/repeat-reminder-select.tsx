import { useId, useState } from "react";
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

function scopeOf(card: CardDetail) {
  return { itemId: card.id, boardId: card.boardId, workspaceId: card.workspaceId };
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
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="text-micro font-semibold text-meta-foreground">
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
        <SelectTrigger id={id} className="w-full">
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
        <p className="text-micro text-meta-foreground">{copy.repeat.needsDue}</p>
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
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="text-micro font-semibold text-meta-foreground">
        {copy.reminder.label}
      </label>
      <Select
        value={String(current)}
        disabled={disabled || writes.pending}
        onValueChange={(value) => {
          setError(null);
          writes.setReminder(Number(value)).catch(() => setError(copy.reminder.failed));
        }}
      >
        <SelectTrigger id={id} className="w-full">
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
      <p className="text-micro text-meta-foreground">{copy.reminder.note}</p>
      {error ? (
        <p role="alert" className="text-micro text-danger-subtle-foreground">
          {error}
        </p>
      ) : null}
    </div>
  );
}
