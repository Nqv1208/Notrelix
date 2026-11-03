import { useEffect, useRef, useState } from "react";
import { Clock, Plus, SlidersHorizontal } from "lucide-react";
import { FaceFieldsButton } from "../capabilities/face-fields-button";
import { usePinnedFieldIds } from "../capabilities/use-pinned-fields";
import { customItemFields } from "../../card-detail/item-field-editors";
import { isFieldValueEmpty } from "../../card-detail/item-field-more-editors";
import { calendarParts, pad2, toValidDate } from "../../shared/item-display";
import { useCanEdit, useCardPopup } from "../card-popup-context";
import {
  EmptySection,
  LinkBtn,
  PanelPopover,
  PopSection,
  SECTION_ICONS,
  SectionHead,
  SmallBtn,
} from "../popup-ui";
import { PopoverTrigger } from "@notrelix/ui-web";
import { AddFieldPanel } from "./add-field-panel";
import { PillBtn, PillKey, PillStatic } from "./field-pill";
import { FieldControl } from "./field-editors";

/** `dd/mm/yyyy hh:mm` of an instant, in the card's time zone. */
export function dateTimeText(
  value: string | undefined,
  timeZone?: string,
): string {
  const date = value ? toValidDate(value) : null;
  if (!date) return "";
  const p = calendarParts(date, timeZone);
  return `${pad2(p.day)}/${pad2(p.month)}/${p.year} ${pad2(p.hour)}:${pad2(p.minute)}`;
}

/** The board's fields in the order the section lists them: priority, then the custom ones. */
export function useFieldList() {
  const { board, fields } = useCardPopup();
  const custom = customItemFields(board);
  return fields.priority
    ? [fields.priority, ...custom.filter((f) => f.id !== fields.priority?.id)]
    : custom;
}

/**
 * The custom fields the section can list: not the priority (its own chip), not the fields pinned on the
 * card face (chips too), split into those holding a value (their rows) and those empty (the add panel).
 */
export function useSectionFields() {
  const { board, card, fields, addedFieldIds } = useCardPopup();
  const pinned = usePinnedFieldIds();
  const rest = customItemFields(board).filter(
    (f) =>
      f.id !== fields.priority?.id &&
      !pinned.includes(f.id) &&
      !addedFieldIds.includes(f.id),
  );
  const filled = (f: (typeof rest)[number]) => {
    const computed = card.computedValues?.[f.id];
    // A calculated property always shows, with its empty, error and stale state.
    if (computed !== undefined) return true;
    return !isFieldValueEmpty(card.fieldValues[f.id]);
  };
  return {
    filled: rest.filter(filled),
    empty: rest.filter((f) => !filled(f) && !computedKind(f)),
  };
}

const computedKind = (f: { fieldType: string }) =>
  f.fieldType === "formula" || f.fieldType === "rollup";

/** The custom fields that hold a value, as label and value rows edited in place; the rest sit behind "+ Thêm trường". */
export function FieldsSection() {
  const {
    board,
    card,
    copy,
    mode,
    picker,
    openPicker,
    timeZone,
    startNewField,
  } = useCardPopup();
  const t = copy.sections.fields;
  const canEdit = useCanEdit();
  const { filled, empty } = useSectionFields();
  const [revealed, setRevealed] = useState<readonly string[]>([]);
  const [open, setOpen] = useState(false);
  // A picked or created field takes the cursor; the panel must not hand it back to "Add field".
  const [restoreFocus, setRestoreFocus] = useState(true);
  // "+ Thêm › Thuộc tính / Tạo trường mới" lands here: the add panel opens.
  useEffect(() => {
    if (picker !== "fields") return;
    setOpen(true);
    openPicker(null);
  }, [picker, openPicker]);
  // A revealed row takes the cursor so its value can be typed at once. A field the person has just
  // created shows up after the board is read again, so the effect also waits for the rows.
  const focusedFor = useRef<string | null>(null);
  useEffect(() => {
    const id = revealed[revealed.length - 1];
    if (!id || focusedFor.current === id) return;
    const row = document.querySelector<HTMLElement>(`[data-field-row="${id}"]`);
    if (!row) return;
    focusedFor.current = id;
    row
      .querySelector<HTMLElement>("input, button")
      ?.focus({ preventScroll: false });
  }, [revealed, filled.length, empty.length]);

  const shownIds = new Set(filled.map((f) => f.id));
  const rows = [
    ...filled,
    ...empty.filter((f) => revealed.includes(f.id) && !shownIds.has(f.id)),
  ];
  const addButton = canEdit ? (
    <PanelPopover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (next) setRestoreFocus(true);
        if (!next) startNewField(null);
      }}
      restoreFocus={restoreFocus}
      width={340}
      trigger={
        <PopoverTrigger asChild>
          {mode === "create" && rows.length === 0 ? (
            // Kanban-CardPopup-Create folds an empty fields section to the link-coloured action.
            <LinkBtn aria-haspopup="dialog">
              <Plus aria-hidden="true" className="size-[13px]" />
              {t.addFieldCreate}
            </LinkBtn>
          ) : (
            <PillBtn aria-haspopup="dialog" dashed>
              <Plus aria-hidden="true" className="size-4" />
              {t.addField}
            </PillBtn>
          )}
        </PopoverTrigger>
      }
    >
      <AddFieldPanel
        empty={empty.filter((f) => !revealed.includes(f.id))}
        onPick={(field) => {
          setRestoreFocus(false);
          setRevealed((ids) => [...ids, field.id]);
          setOpen(false);
        }}
        onCreated={(fieldId) => {
          setRestoreFocus(false);
          setRevealed((ids) => [...ids, fieldId]);
          setOpen(false);
        }}
      />
    </PanelPopover>
  ) : null;

  if (rows.length === 0)
    return addButton ? (
      <PopSection label={t.title} icon={SECTION_ICONS.fields}>
        <div className="flex min-h-11 items-center max-md:pl-2.5 md:min-h-8">
          {addButton}
        </div>
      </PopSection>
    ) : (
      <EmptySection
        label={t.title}
        icon={SECTION_ICONS.fields}
        none={copy.shell.noneYet}
      />
    );

  return (
    <PopSection label={t.title} icon={SECTION_ICONS.fields}>
      <SectionHead title={t.title}>
        {mode === "view" ? (
          <FaceFieldsButton
            boardId={board.id}
            workspaceId={board.workspaceId}
            fields={board.fieldDefinitions}
            canEdit={canEdit}
            sampleCard={card}
            trigger={
              <SmallBtn>
                <SlidersHorizontal aria-hidden="true" className="size-[13px]" />
                {copy.sections.fields.showOnCard}
              </SmallBtn>
            }
          />
        ) : null}
      </SectionHead>
      <div className="mt-1 flex flex-wrap gap-1.5" data-field-pills>
        {rows.map((field) => (
          <span
            key={field.id}
            data-field-row={field.id}
            className="inline-flex max-w-full"
          >
            <FieldControl field={field} variant="pill" />
          </span>
        ))}
        <PillStatic title={copy.shell.createdHint}>
          <Clock
            aria-hidden="true"
            className="size-[13px] text-meta-foreground"
          />
          <PillKey>{t.createdAt}</PillKey>
          {mode === "create"
            ? t.createdValue
            : dateTimeText(card.createdAt, timeZone)}
        </PillStatic>
        {addButton}
      </div>
    </PopSection>
  );
}
