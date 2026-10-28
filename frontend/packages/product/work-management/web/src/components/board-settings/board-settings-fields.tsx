import { useEffect, useRef, useState } from "react";
import { Lock, Plus } from "lucide-react";
import { Popover, PopoverAnchor, PopoverContent, cn } from "@notrelix/ui-web";
import type {
  BoardSettingsFieldDto,
  BoardSettingsFieldType,
} from "@notrelix/work-management-core";
import { useSettings } from "./board-settings-context";
import { FieldArchiveDialog } from "./board-settings-field-archive";
import { FieldDeleteDialog } from "./board-settings-field-delete";
import { FieldPanel } from "./board-settings-field-panel";
import { FieldTypePicker } from "./board-settings-field-picker";
import {
  fieldLabel,
  fillCopy,
  plural,
  shortDay,
  systemFieldRows,
  typeName,
} from "./board-settings-format";
import {
  BUTTON_CLASS,
  SectionHeader,
  SectionLine,
  Tag_,
  TypeMark,
  fieldIcon,
} from "./board-settings-parts";
import { SortableList, SortableRow } from "./board-settings-sortable";

const ROW_GRID =
  "grid grid-cols-[22px_minmax(0,1fr)_150px_132px_104px] items-center gap-2.5 min-h-[45px] first:min-h-[44px] pr-3 pl-2 text-item-small";
const HEAD_ROW =
  "min-h-[34px] first:min-h-[34px] bg-background text-metadata font-semibold text-meta-foreground";

/** The "Trường dữ liệu" section: the field tables, the picker, the panel and the archive dialog. */
export function FieldsSection({
  fieldId,
  onSelectField,
  defaultPickerOpen,
  defaultArchiveFieldId,
}: {
  fieldId: string | null;
  onSelectField: (fieldId: string | null) => void;
  defaultPickerOpen?: boolean;
  defaultArchiveFieldId?: string | null;
}) {
  const { copy, settings, actions, run, timeZone } = useSettings();
  const t = copy.fields;
  const [pickerOpen, setPickerOpen] = useState(defaultPickerOpen ?? false);
  const [archiveId, setArchiveId] = useState<string | null>(
    defaultArchiveFieldId ?? null,
  );
  const selected =
    settings.fields.find((field) => field.id === fieldId) ?? null;
  const archiving =
    settings.fields.find((field) => field.id === archiveId) ?? null;
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const deleting =
    settings.fields.find((field) => field.id === deleteId) ?? null;
  const archivedRef = useRef<HTMLDivElement>(null);
  const knownIds = useRef<readonly string[] | null>(null);

  // A field that no longer exists (archived elsewhere) closes the panel.
  useEffect(() => {
    if (fieldId && !selected) onSelectField(null);
  }, [fieldId, onSelectField, selected]);

  async function addField(type: BoardSettingsFieldType) {
    setPickerOpen(false);
    knownIds.current = settings.fields.map((field) => field.id);
    const result = await run(() => actions.createField(type));
    if (!result.ok) knownIds.current = null;
  }

  // The field just added opens in its panel, so it can be named at once.
  useEffect(() => {
    if (!knownIds.current) return;
    const fresh = settings.fields.find(
      (field) => !knownIds.current!.includes(field.id),
    );
    if (!fresh) return;
    knownIds.current = null;
    onSelectField(fresh.id);
  }, [onSelectField, settings.fields]);

  const systemRows = systemFieldRows(copy, settings.systemFields);
  const liveCount = settings.fields.length;

  return (
    <>
      <section
        aria-labelledby="bs-fields-h"
        className="min-w-0 flex-1 overflow-auto px-6 pt-5 pb-6"
      >
        <SectionHeader
          id="bs-fields-h"
          title={t.title}
          intro={t.intro}
          actions={
            <Popover open={pickerOpen} onOpenChange={setPickerOpen}>
              <PopoverAnchor asChild>
                <button
                  type="button"
                  aria-haspopup="dialog"
                  aria-expanded={pickerOpen}
                  onClick={() => setPickerOpen((open) => !open)}
                  className={BUTTON_CLASS.primary}
                >
                  <Plus aria-hidden="true" />
                  {t.add}
                </button>
              </PopoverAnchor>
              <PopoverContent
                aria-label={copy.picker.label}
                align="end"
                side="bottom"
                sideOffset={15}
                className="w-[520px] max-w-[calc(100vw-2rem)] overflow-hidden rounded-xl border border-line-strong bg-popover p-0 shadow-popover"
              >
                <FieldTypePicker
                  copy={copy}
                  allowed={settings.unavailable?.creatableFieldTypes}
                  onPick={addField}
                />
              </PopoverContent>
            </Popover>
          }
        />

        <SectionLine
          title={t.boardFields}
          count={liveCount}
          note={t.dragNote}
        />
        <div
          role="list"
          aria-label={t.listLabel}
          className="overflow-hidden rounded-[10px] border border-line bg-popover"
        >
          <div aria-hidden="true" className={cn(ROW_GRID, HEAD_ROW)}>
            <span />
            <span>{t.columns.name}</span>
            <span>{t.columns.type}</span>
            <span>{t.columns.key}</span>
            <span>{t.columns.usage}</span>
          </div>
          {liveCount === 0 ? (
            <div role="listitem" className="px-4 py-6 text-center">
              <p className="m-0 text-item-small font-semibold">
                {t.empty.title}
              </p>
              <p className="m-0 mt-1 text-metadata text-meta-foreground">
                {t.empty.body}
              </p>
            </div>
          ) : (
            <SortableList
              ids={settings.fields.map((field) => field.id)}
              onReorder={(ids) => void run(() => actions.reorderFields(ids))}
            >
              {(order) =>
                order.map((id) => {
                  const field = settings.fields.find(
                    (entry) => entry.id === id,
                  );
                  if (!field) return null;
                  return (
                    <FieldRow
                      key={field.id}
                      field={field}
                      selected={field.id === fieldId}
                      onOpen={() => onSelectField(field.id)}
                    />
                  );
                })
              }
            </SortableList>
          )}
        </div>

        <SectionLine
          title={t.system}
          count={settings.systemFields.length}
          note={t.systemNote}
        />
        <div
          role="list"
          className="overflow-hidden rounded-[10px] border border-line bg-popover"
        >
          {systemRows.map((row) => (
            <div
              key={row.id}
              role="listitem"
              className={cn(ROW_GRID, "border-t border-line first:border-t-0")}
            >
              <span />
              <span className="flex min-w-0 items-center gap-[9px] font-medium">
                <TypeMark icon={fieldIcon(row.first.type, row.first.key)} />
                <span className="truncate">{row.name}</span>
              </span>
              <span className="text-muted-foreground">
                {row.type}
                {row.detail ? (
                  <small className="block text-[11.5px] leading-[1.45] text-meta-foreground">
                    {row.detail}
                  </small>
                ) : null}
              </span>
              <span className="truncate font-mono text-[12px] text-meta-foreground">
                {row.key}
              </span>
              <span className="inline-flex items-center gap-[5px] text-metadata text-meta-foreground">
                <Lock aria-hidden="true" className="size-3.5" />
                {row.lock === "rename" ? t.renameOnly : t.locked}
              </span>
            </div>
          ))}
        </div>

        {settings.unavailable?.fieldArchive ? null : (
          <div ref={archivedRef} id="bs-archived">
            <SectionLine
              title={t.archived}
              count={settings.archivedFields.length}
              note={t.archivedNote}
            />
            {settings.archivedFields.length === 0 ? null : (
              <div
                role="list"
                className="overflow-hidden rounded-[10px] border border-line bg-popover"
              >
                {settings.archivedFields.map((field) => (
                  <div
                    key={field.id}
                    role="listitem"
                    className={cn(
                      ROW_GRID,
                      "border-t border-line text-meta-foreground first:border-t-0",
                    )}
                  >
                    <span />
                    <span className="flex min-w-0 items-center gap-[9px] font-medium">
                      <TypeMark icon={fieldIcon(field.type, field.key)} />
                      <span className="truncate">{field.name}</span>
                    </span>
                    <span className="text-muted-foreground">
                      {typeName(copy, field.type)}
                      <small className="block text-[11.5px] leading-[1.45] text-meta-foreground">
                        {fillCopy(t.archivedMeta, {
                          date: shortDay(field.archivedAt, timeZone),
                          name: field.archivedByName,
                        })}
                      </small>
                    </span>
                    <span className="truncate font-mono text-[12px] text-meta-foreground">
                      {field.key}
                    </span>
                    <span>
                      <button
                        type="button"
                        aria-label={fillCopy(t.restoreLabel, {
                          name: field.name,
                        })}
                        onClick={() =>
                          void run(() => actions.restoreField(field.id))
                        }
                        className={BUTTON_CLASS.ghostBrand}
                      >
                        {t.restore}
                      </button>
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </section>

      {selected ? (
        <FieldPanel
          key={selected.id}
          field={selected}
          onClose={() => onSelectField(null)}
          onArchive={
            settings.unavailable?.fieldArchive
              ? undefined
              : () => setArchiveId(selected.id)
          }
          onDelete={
            settings.unavailable?.fieldDelete === false
              ? () => setDeleteId(selected.id)
              : undefined
          }
        />
      ) : null}

      {deleting ? (
        <FieldDeleteDialog
          field={deleting}
          onClose={() => setDeleteId(null)}
          onDeleted={() => {
            setDeleteId(null);
            if (deleting.id === fieldId) onSelectField(null);
          }}
        />
      ) : null}

      {archiving ? (
        <FieldArchiveDialog
          field={archiving}
          onClose={() => setArchiveId(null)}
          onArchived={() => {
            setArchiveId(null);
            if (archiving.id === fieldId) onSelectField(null);
          }}
        />
      ) : null}
    </>
  );
}

function FieldRow({
  field,
  selected,
  onOpen,
}: {
  field: BoardSettingsFieldDto;
  selected: boolean;
  onOpen: () => void;
}) {
  const { copy } = useSettings();
  return (
    <SortableRow
      id={field.id}
      handleLabel={`${copy.common.dragHandle}: ${fieldLabel(field)}`}
      role="listitem"
      aria-current={selected ? "true" : undefined}
      onClick={onOpen}
      className={cn(
        ROW_GRID,
        "cursor-pointer border-t border-line bg-popover first:border-t-0",
        selected && "bg-row-selected shadow-[inset_0_0_0_1.5px_var(--primary)]",
      )}
    >
      {(handle) => (
        <>
          <span
            className="grid place-items-center"
            onClick={(event) => event.stopPropagation()}
          >
            {handle}
          </span>
          <button
            type="button"
            aria-label={fillCopy(copy.fields.open, { name: fieldLabel(field) })}
            onClick={(event) => {
              event.stopPropagation();
              onOpen();
            }}
            className="flex min-w-0 cursor-pointer items-center gap-[9px] border-0 bg-transparent p-0 text-left font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <TypeMark icon={fieldIcon(field.type, field.key)} />
            <span className="truncate">{fieldLabel(field)}</span>
          </button>
          <span className="text-muted-foreground">
            {typeName(copy, field.type)}
            {field.detail ? (
              <small className="block text-[11.5px] leading-[1.45] text-meta-foreground">
                {field.detail}
              </small>
            ) : null}
          </span>
          <span className="truncate font-mono text-[12px] text-meta-foreground">
            {field.key}
          </span>
          <span className="truncate text-crumb text-meta-foreground">
            {field.accent && field.usage ? (
              <Tag_>{field.usage}</Tag_>
            ) : field.usage ? (
              field.usage
            ) : field.valueCount > 0 ? (
              plural(
                copy.archiveDialog.values_one,
                copy.archiveDialog.values_other,
                field.valueCount,
              )
            ) : null}
          </span>
        </>
      )}
    </SortableRow>
  );
}
