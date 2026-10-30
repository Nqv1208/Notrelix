import { useEffect, useRef } from "react";
import { cn } from "@notrelix/ui-web";
import { customItemFields } from "../../card-detail/item-field-editors";
import { AddPropertyPopover } from "../../card-detail/item-add-property";
import { useItemSheetCopy } from "../../card-detail/use-item-sheet-copy";
import { calendarParts, pad2, toValidDate } from "../../shared/item-display";
import { useCanEdit, useCardPopup } from "../card-popup-context";
import { PopSection, SECTION_ICONS, SectionHead } from "../popup-ui";
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

/** Every custom field with its editor; the created time is read-only. */
export function FieldsSection() {
  const { card, copy, mode, callbacks, picker, openPicker, timeZone } =
    useCardPopup();
  const t = copy.sections.fields;
  const canEdit = useCanEdit();
  const sheet = useItemSheetCopy();
  const list = useFieldList();
  const add = useRef<HTMLDivElement>(null);
  // "+ Thêm › Trường tuỳ chỉnh" opens the add-field form of this section.
  useEffect(() => {
    if (picker !== "fields") return;
    add.current?.querySelector<HTMLButtonElement>("button")?.click();
    openPicker(null);
  }, [picker, openPicker]);

  return (
    <PopSection label={t.title} icon={SECTION_ICONS.fields}>
      <SectionHead title={t.title}>
        {canEdit && callbacks.onCreateField ? (
          <div ref={add}>
            <AddPropertyPopover
              t={sheet.properties}
              emptyFields={[]}
              onReveal={() => undefined}
              onCreate={callbacks.onCreateField}
              onCreated={() => undefined}
              triggerClassName="inline-grid size-8 place-items-center rounded-lg border border-line-strong bg-popover text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            />
          </div>
        ) : null}
      </SectionHead>
      <div className="mt-3 grid grid-cols-2 gap-x-5 gap-y-3.5 max-md:grid-cols-1">
        {list.map((field) => (
          <div key={field.id} className="flex min-w-0 flex-col gap-1.5">
            <span className="flex items-center gap-1.5 text-[12px] leading-[18px] font-semibold whitespace-nowrap text-muted-foreground">
              {field.name}
            </span>
            <FieldControl field={field} variant="ctl" />
          </div>
        ))}
        <div className="flex min-w-0 flex-col gap-1.5">
          <span className="text-[12px] leading-[18px] font-semibold whitespace-nowrap text-muted-foreground">
            {t.createdAt}
          </span>
          <span
            title={copy.shell.createdHint}
            className={cn(
              "box-border flex h-9 w-full items-center gap-2 rounded-[8px] border border-dashed border-line-strong bg-background px-3 text-[13.5px] text-muted-foreground",
            )}
          >
            {mode === "create"
              ? t.createdValue
              : dateTimeText(card.createdAt, timeZone)}
          </span>
        </div>
      </div>
      {list.length === 0 ? (
        <p className="m-0 mt-2 text-[12.5px] text-meta-foreground">{t.none}</p>
      ) : null}
    </PopSection>
  );
}
