import { useEffect, useState } from "react";
import {
  Calendar,
  Hash,
  Link2,
  List,
  SquareCheck,
  Type,
  type LucideIcon,
} from "lucide-react";
import type { FieldDefinition } from "@notrelix/work-management-core";
import { AddPropertyPopover } from "../../card-detail/item-add-property";
import { useItemSheetCopy } from "../../card-detail/use-item-sheet-copy";
import { foldForSearch } from "../../item-page/item-pickers";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import { Caption, Hint, SearchField } from "../pickers/picker-bits";

const TYPE_ICON: Readonly<Record<string, LucideIcon>> = {
  text: Type,
  long_text: Type,
  number: Hash,
  checkbox: SquareCheck,
  select: List,
  multi_select: List,
  date: Calendar,
  link: Link2,
};

/** The icon of a field type, shared by this panel and the "+ Thêm" menu. */
export function fieldTypeIcon(type: string): LucideIcon {
  return TYPE_ICON[type] ?? Type;
}

/**
 * "+ Thêm trường": the board's fields that hold nothing on this card, found by name. A pick shows that
 * field's row so its value can be typed; "Tạo trường mới" makes a field on the board.
 */
export function AddFieldPanel({
  empty,
  onPick,
  onCreated,
}: {
  empty: readonly FieldDefinition[];
  onPick: (field: FieldDefinition) => void;
  /** A field was just created on the board: its row opens on this card. */
  onCreated: (fieldId: string) => void;
}) {
  const { copy, callbacks, newFieldName } = useCardPopup();
  const t = copy.sections.fields;
  const sheet = useItemSheetCopy();
  const [query, setQuery] = useState("");
  // The new-field form of a typed name opens after the panel has taken focus, or it would close at once.
  const [settled, setSettled] = useState(newFieldName === null);
  useEffect(() => {
    if (newFieldName === null) return;
    const id = window.setTimeout(() => setSettled(true), 200);
    return () => window.clearTimeout(id);
  }, [newFieldName]);
  const needle = foldForSearch(query);
  const list = empty.filter((f) => !needle || foldForSearch(f.name).includes(needle));
  return (
    <div>
      <SearchField
        value={query}
        onChange={setQuery}
        label={t.searchFields}
        placeholder={t.searchFields}
      />
      {empty.length === 0 ? (
        <Hint className="px-1 py-2">{t.allSet}</Hint>
      ) : (
        <>
          <Caption className="mt-2.5 px-1">{t.emptyFieldsCaption}</Caption>
          <div role="listbox" aria-label={t.emptyFieldsCaption}>
            {list.map((field) => {
              const Icon = fieldTypeIcon(field.fieldType);
              return (
                <button
                  key={field.id}
                  type="button"
                  role="option"
                  aria-selected={false}
                  onClick={() => onPick(field)}
                  className="flex h-9 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                >
                  <Icon
                    aria-hidden="true"
                    className="size-[13px] shrink-0 text-muted-foreground"
                  />
                  <span className="min-w-0 flex-1 truncate">{field.name}</span>
                  <small className="text-[12.5px] text-meta-foreground">
                    {t.typeNames[field.fieldType] ?? field.fieldType}
                  </small>
                </button>
              );
            })}
            {list.length === 0 ? (
              <Hint className="px-2.5 py-2">
                {fillPopupCopy(t.noMatch, { query })}
              </Hint>
            ) : null}
          </div>
        </>
      )}
      {callbacks.onCreateField && settled ? (
        <div className="mt-1 border-t border-line pt-1">
          <AddPropertyPopover
            t={sheet.properties}
            emptyFields={[]}
            onReveal={() => undefined}
            onCreate={callbacks.onCreateField}
            onCreated={onCreated}
            triggerLabel={t.createField}
            initialName={newFieldName ?? undefined}
            triggerClassName="flex h-9 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] font-semibold text-brand-ink hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          />
        </div>
      ) : (
        <p className="m-0 mt-1 border-t border-line px-1 pt-2 text-[12.5px] leading-[1.45] text-meta-foreground">
          {t.addNote}
        </p>
      )}
    </div>
  );
}
