import { useState, type DragEvent, type KeyboardEvent } from "react";
import { SlidersHorizontal, ThumbsUp } from "lucide-react";
import { PopoverTrigger, cn } from "@notrelix/ui-web";
import type { Card, FieldDefinition } from "@notrelix/work-management-core";
import { useCardPopupCopy } from "../use-card-popup-copy";
import { PanelPopover, SmallBtn } from "../popup-ui";
import { Caption, Hint, PickerHead } from "../pickers/picker-bits";
import {
  faceEligibleFields,
  faceFieldLabel,
  faceFieldText,
} from "./face-fields";
import { fillCapabilityCopy } from "./capabilities.copy";
import { useCapabilitiesCopy } from "./use-capabilities-copy";

export interface FaceFieldsSetting {
  cardFieldIds: string[];
  cardHideEmpty: string[];
}

/** `.swt`: the 36 x 20 switch of a row. */
function Switch({
  on,
  label,
  disabled,
  onToggle,
}: {
  on: boolean;
  label: string;
  disabled: boolean;
  onToggle: () => void;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={on}
      aria-label={label}
      disabled={disabled}
      onClick={onToggle}
      className={cn(
        "relative h-5 w-9 flex-none rounded-[10px] border-0 p-0 transition-colors after:absolute after:top-0.5 after:left-0.5 after:size-4 after:rounded-full after:bg-white after:shadow-[0_1px_2px_rgb(0_0_0/0.3)] after:transition-[left] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:cursor-not-allowed disabled:opacity-60 motion-reduce:transition-none motion-reduce:after:transition-none",
        on ? "bg-primary after:left-[18px]" : "bg-line-strong",
      )}
    />
  );
}

/** A row's name and kind of field, two lines. */
function FieldText({ name, type }: { name: string; type: string }) {
  return (
    <span className="min-w-0 py-px">
      <span className="mb-[1.3px] block h-[18px] truncate text-[13.5px] leading-[18px] font-semibold">
        {name}
      </span>
      <span className="block text-[12px] leading-[1.45] text-meta-foreground">
        {type}
      </span>
    </span>
  );
}

/** The six dots of a row's drag handle. */
function Grip() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="size-4 fill-current">
      {[6, 12, 18].flatMap((y) =>
        [9, 15].map((x) => <circle key={`${x}${y}`} cx={x} cy={y} r="1.4" />),
      )}
    </svg>
  );
}

/**
 * "Trường hiển thị trên thẻ": which fields the Kanban card face shows, in which order, and for each
 * whether it hides while empty, with a preview of the face. It edits the Kanban view's own setting, so
 * it needs edit rights on the board (`canEdit`); without them it only shows the current choice.
 * A row is dragged by its handle, or moved with the arrow keys while the handle has focus.
 */
export function FaceFieldsPopover({
  fields,
  value,
  canEdit,
  onChange,
  trigger,
  sampleCard,
}: {
  fields: readonly FieldDefinition[];
  value: FaceFieldsSetting;
  canEdit: boolean;
  onChange: (next: FaceFieldsSetting) => void;
  trigger?: React.ReactNode;
  /** The card whose face the preview draws; without one the preview shows a sample title and the field names. */
  sampleCard?: Card;
}) {
  const copy = useCapabilitiesCopy();
  const typeNames = useCardPopupCopy().sections.fields.typeNames as Record<
    string,
    string
  >;
  const [open, setOpen] = useState(false);
  const [dragging, setDragging] = useState<number | null>(null);
  const eligible = faceEligibleFields(fields);
  const byId = new Map(eligible.map((field) => [field.id, field]));
  const shown = value.cardFieldIds
    .map((id) => byId.get(id))
    .filter((field): field is FieldDefinition => field !== undefined);
  const rest = eligible.filter(
    (field) => !value.cardFieldIds.includes(field.id),
  );

  const set = (ids: string[], hide = value.cardHideEmpty) =>
    onChange({
      cardFieldIds: ids,
      cardHideEmpty: hide.filter((id) => ids.includes(id)),
    });
  const moveTo = (from: number, to: number) => {
    const ids = shown.map((field) => field.id);
    if (to < 0 || to >= ids.length || to === from) return;
    const [moved] = ids.splice(from, 1);
    ids.splice(to, 0, moved!);
    set(ids);
  };
  const toggleHide = (id: string, on: boolean) =>
    onChange({
      cardFieldIds: value.cardFieldIds,
      cardHideEmpty: on
        ? [...value.cardHideEmpty, id]
        : value.cardHideEmpty.filter((existing) => existing !== id),
    });
  const onHandleKey = (event: KeyboardEvent, index: number) => {
    if (event.key !== "ArrowUp" && event.key !== "ArrowDown") return;
    event.preventDefault();
    moveTo(index, index + (event.key === "ArrowUp" ? -1 : 1));
  };
  const onDrop = (event: DragEvent, index: number) => {
    event.preventDefault();
    if (dragging !== null) moveTo(dragging, index);
    setDragging(null);
  };
  const nameOf = (field: FieldDefinition) => faceFieldLabel(field, typeNames);

  return (
    <PanelPopover
      open={open}
      onOpenChange={setOpen}
      width={392}
      align="end"
      trigger={
        <PopoverTrigger asChild>
          {(trigger as React.ReactElement | undefined) ?? (
            <SmallBtn>
              <SlidersHorizontal aria-hidden="true" className="size-[13px]" />
              {copy.face.open}
            </SmallBtn>
          )}
        </PopoverTrigger>
      }
    >
      <PickerHead title={copy.face.title} onClose={() => setOpen(false)} />
      <Hint className="mx-0.5 mb-2">{copy.face.note}</Hint>
      {!canEdit ? (
        <Hint className="mx-0.5 mb-2">{copy.face.locked}</Hint>
      ) : null}
      <Caption className="mt-0">{copy.face.shown}</Caption>
      {shown.length === 0 ? (
        <Hint className="mx-0.5">{copy.face.empty}</Hint>
      ) : (
        <ul className="m-0 flex list-none flex-col p-0">
          {shown.map((field, index) => (
            <li
              key={field.id}
              draggable={canEdit}
              onDragStart={() => setDragging(index)}
              onDragEnd={() => setDragging(null)}
              onDragOver={(event) => canEdit && event.preventDefault()}
              onDrop={(event) => onDrop(event, index)}
              className={cn(
                "grid min-h-13 grid-cols-[18px_36px_minmax(0,1fr)_auto] items-center gap-2 rounded-[8px] px-1 hover:bg-state-hover-strong md:min-h-11",
                dragging === index && "opacity-60",
              )}
            >
              <button
                type="button"
                disabled={!canEdit}
                aria-label={fillCapabilityCopy(copy.face.reorder, {
                  name: field.name,
                })}
                onKeyDown={(event) => onHandleKey(event, index)}
                className="grid size-[18px] cursor-grab place-items-center rounded-[4px] border-0 bg-transparent p-0 text-meta-foreground focus-visible:outline-2 focus-visible:outline-primary disabled:cursor-default"
              >
                <Grip />
              </button>
              <Switch
                on
                disabled={!canEdit}
                label={fillCapabilityCopy(copy.face.remove, {
                  name: field.name,
                })}
                onToggle={() =>
                  set(value.cardFieldIds.filter((id) => id !== field.id))
                }
              />
              <FieldText name={field.name} type={nameOf(field)} />
              <label className="flex items-center gap-1.5 text-[12.5px] whitespace-nowrap text-muted-foreground">
                <input
                  type="checkbox"
                  disabled={!canEdit}
                  checked={value.cardHideEmpty.includes(field.id)}
                  aria-label={fillCapabilityCopy(copy.face.hideEmptyOf, {
                    name: field.name,
                  })}
                  onChange={(event) =>
                    toggleHide(field.id, event.target.checked)
                  }
                  className="m-0 size-[18px] flex-none accent-primary"
                />
                {copy.face.hideEmpty}
              </label>
            </li>
          ))}
        </ul>
      )}
      {rest.length > 0 ? (
        <>
          <Caption>{copy.face.hidden}</Caption>
          <ul className="m-0 flex list-none flex-col p-0">
            {rest.map((field) => (
              <li
                key={field.id}
                className="grid min-h-13 grid-cols-[18px_36px_minmax(0,1fr)_auto] items-center gap-2 rounded-[8px] px-1 hover:bg-state-hover-strong md:min-h-11"
              >
                <span aria-hidden="true" />
                <Switch
                  on={false}
                  disabled={!canEdit}
                  label={fillCapabilityCopy(copy.face.show, {
                    name: field.name,
                  })}
                  onToggle={() => set([...value.cardFieldIds, field.id])}
                />
                <FieldText name={field.name} type={nameOf(field)} />
                <span aria-hidden="true" />
              </li>
            ))}
          </ul>
        </>
      ) : null}
      <Caption>{copy.face.preview}</Caption>
      <div
        aria-label={copy.face.preview}
        className="box-border flex flex-col gap-1.5 rounded-[10px] border border-line-strong bg-popover p-2.5 text-[13px]"
      >
        <span className="font-semibold">
          {sampleCard?.title ?? copy.face.previewTitle}
        </span>
        <span className="flex flex-wrap gap-1.5 text-[12px] text-muted-foreground">
          {shown.map((field) => (
            <span
              key={field.id}
              className="inline-flex h-[22px] items-center gap-1.5 rounded-[6px] bg-state-hover-strong px-[7px] font-semibold"
            >
              {field.fieldType === "vote" ? (
                <ThumbsUp aria-hidden="true" className="size-[13px]" />
              ) : null}
              {(sampleCard ? faceFieldText(sampleCard, field) : null) ??
                field.name}
            </span>
          ))}
        </span>
      </div>
      <Hint className="mx-0.5 mt-2.5">{copy.face.applies}</Hint>
    </PanelPopover>
  );
}
