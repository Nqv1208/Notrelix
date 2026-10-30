import { ColourBlindLabelMark } from "./capabilities/colour-blind-label-mark";
import { ChevronDown, Clock, Plus } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import {
  ITEM_LABEL_TONE_CLASS,
  calendarDaysFromToday,
  formatItemDate,
  itemLabelTone,
} from "../shared/item-display";
import { isItemDone } from "../card-detail/item-field-editors";
import { fillPopupCopy } from "./card-popup.copy";
import { useCanEdit, useCardPopup } from "./card-popup-context";
import { AvatarStack, ChipBtn, ChipReadOnly, FieldGroup } from "./popup-ui";
import { FieldControl } from "./sections/field-editors";
import { faceEligibleFields } from "./capabilities/face-fields";
import { usePinnedFieldIds } from "./capabilities/use-pinned-fields";

const ADD_CIRCLE =
  "grid size-8 shrink-0 place-items-center rounded-full border border-dashed border-line-strong bg-transparent p-0 text-meta-foreground hover:bg-state-hover-strong hover:text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

/** The "Ngày" chip: "12/10 – 16/10" and how far the due date is. */
function DatesChip() {
  const { card, copy, now, timeZone, openPicker } = useCardPopup();
  const t = copy.shell;
  const canEdit = useCanEdit();
  const due = card.dueAt ?? card.dueDate;
  const range = [
    card.startDate ? formatItemDate(card.startDate, now, timeZone) : null,
    due ? formatItemDate(due, now, timeZone) : null,
  ];
  const text =
    range[0] && range[1]
      ? `${range[0]} – ${range[1]}`
      : (range[1] ??
        (range[0] ? `${range[0]} –` : copy.sections.chips.noDates));
  const days =
    due && !isItemDone(card) ? calendarDaysFromToday(due, now, timeZone) : null;
  const tag =
    days === null
      ? null
      : days < 0
        ? {
            text: fillPopupCopy(days === -1 ? t.overdue_one : t.overdue_other, {
              count: -days,
            }),
            tone: "text-danger-text",
          }
        : days === 0
          ? { text: t.dueToday, tone: "text-warning-text" }
          : {
              text: fillPopupCopy(
                days === 1 ? t.daysLeft_one : t.daysLeft_other,
                { count: days },
              ),
              tone: days <= 7 ? "text-warning-text" : "text-muted-foreground",
            };
  const body = (
    <>
      <Clock aria-hidden="true" className="size-[13px]" />
      <span>{text}</span>
      {tag ? (
        <span className={cn("text-[12.5px] font-semibold", tag.tone)}>
          {tag.text}
        </span>
      ) : null}
    </>
  );
  return canEdit ? (
    <ChipBtn
      aria-haspopup="dialog"
      onClick={(event) => openPicker("dates", event.currentTarget)}
    >
      {body}
      <ChevronDown
        aria-hidden="true"
        className="ml-0.5 size-[13px] text-meta-foreground"
      />
    </ChipBtn>
  ) : (
    <ChipReadOnly>{body}</ChipReadOnly>
  );
}

/** The labelled chips under the quick row, editable in place. */
export function ChipsRow() {
  const { board, card, copy, openPicker, slots, fields } = useCardPopup();
  const t = copy.shell;
  const canEdit = useCanEdit();
  const pinnedIds = usePinnedFieldIds();
  const eligible = faceEligibleFields(board.fieldDefinitions);
  const pinned = pinnedIds
    .map((id) => eligible.find((field) => field.id === id))
    .filter((field): field is NonNullable<typeof field> => field !== undefined);

  return (
    <>
      <div className="flex flex-wrap items-start gap-x-4 gap-y-3">
        <FieldGroup label={t.assignees}>
          <div className="flex min-h-9 items-center">
            {canEdit || card.members.length > 0 ? (
              <button
                type="button"
                disabled={!canEdit}
                aria-label={[t.assignees, ...card.members.map((m) => m.name)].join(": ").replace(": ", ": ")}
                aria-haspopup="dialog"
                className="box-border flex h-9 items-center rounded-[8px] border-0 bg-transparent px-1.5 hover:bg-state-hover-strong disabled:cursor-default disabled:hover:bg-transparent focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                onClick={(event) =>
                  openPicker("assignees", event.currentTarget)
                }
              >
                {card.members.length > 0 ? (
                  <AvatarStack members={card.members} />
                ) : (
                  <span className="grid size-6 place-items-center rounded-full border border-dashed border-line-strong text-meta-foreground">
                    <Plus aria-hidden="true" className="size-[13px]" />
                  </span>
                )}
              </button>
            ) : null}
          </div>
        </FieldGroup>
        <FieldGroup label={t.labels}>
          <div className="flex min-h-8 items-center gap-1.5">
            {card.labels.map((l) => (
              <span
                key={l.id}
                className={cn(
                  "box-border inline-flex h-8 items-center gap-1.5 rounded-[6px] px-3 text-[13px] font-semibold whitespace-nowrap",
                  ITEM_LABEL_TONE_CLASS[itemLabelTone(l)],
                )}
              >
                <ColourBlindLabelMark color={l.color} className="mr-1" />
                {l.name}
              </span>
            ))}
            {canEdit ? (
              <button
                type="button"
                aria-label={t.addLabel}
                className={cn(ADD_CIRCLE, "rounded-[6px]")}
                onClick={(event) => openPicker("labels", event.currentTarget)}
              >
                <Plus aria-hidden="true" className="size-[13px]" />
              </button>
            ) : null}
          </div>
        </FieldGroup>
        <FieldGroup label={t.dates}>
          <div className="relative flex items-center gap-2">
            <DatesChip />
          </div>
        </FieldGroup>
        {fields.priority ? (
          <FieldGroup label={fields.priority.name}>
            <div className="flex items-center gap-2">
              <FieldControl field={fields.priority} variant="chip" />
            </div>
          </FieldGroup>
        ) : null}
      </div>
      {pinned.length > 0 || slots.chips ? (
        <div className="flex flex-wrap items-start gap-x-4 gap-y-3">
          {pinned.map((field) => (
            <FieldGroup key={field.id} label={field.name}>
              <div className="flex items-center gap-2">
                <FieldControl field={field} variant="chip" />
              </div>
            </FieldGroup>
          ))}
          {slots.chips}
        </div>
      ) : null}
    </>
  );
}
