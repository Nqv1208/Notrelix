import { useRef, type ReactNode } from "react";
import { AlertCircle, ExternalLink, Loader2, Lock } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { StaleSchemaDetail } from "@notrelix/work-management-state";
import {
  specOf,
  type ComputedValue,
  type FieldDefinition,
} from "@notrelix/work-management-core";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";
import {
  AnchoredNote,
  CheckboxEditor,
  DateEditor,
  InlineInputEditor,
  LongTextEditor,
  OptionChip,
  OptionEditor,
  PersonMark,
  type EditorOption,
} from "./table-field-editors";
import {
  computedText,
  fieldIds,
  fieldText,
  type TableFieldMember,
} from "./table-field-format";

/** The write state of one cell: saving, failed (the value typed is kept), or in conflict. */
export interface CellWrite {
  readonly status: "pending" | "error" | "conflict" | "schema" | "reloading";
  /** What the board changed under the write (`schema`). */
  readonly schema?: StaleSchemaDetail;
  /** The refusal is "this field is required": nothing was stored and retrying cannot help. */
  readonly required?: boolean;
  /** The value the person wrote; shown while the write has not settled. */
  readonly value: unknown;
}

/** What the surface hands a row to edit its custom field cells. */
export interface TableFieldEditing {
  /** The viewer may write field values. */
  readonly canEdit: boolean;
  readonly members: readonly TableFieldMember[];
  readonly writeOf: (rowId: string, fieldId: string) => CellWrite | undefined;
  /** Fields with a refused cell (a column the board changed): the header takes the danger colour. */
  readonly refusedFieldIds?: ReadonlySet<string>;
  readonly onWrite: (
    rowId: string,
    fieldId: string,
    value: unknown,
    stale?: boolean,
  ) => void;
  readonly onRetry: (rowId: string, fieldId: string) => void;
  readonly onKeepMine: (rowId: string, fieldId: string) => void;
  readonly onUseTheirs: (rowId: string, fieldId: string) => void;
  /** Reload the board's fields after a stale write; the unsaved value is dropped. */
  readonly onReloadSchema: (rowId: string, fieldId: string) => void;
}

export interface TableFieldCellProps {
  field: FieldDefinition;
  cell: { readonly value: unknown; readonly computed?: ComputedValue };
  /** The row's key (or title): names the cell for assistive technology. */
  rowKey: string;
  copy: MainTableCopy;
  members: readonly TableFieldMember[];
  /** The viewer may write this field. */
  editable: boolean;
  write?: CellWrite;
  onWrite: (value: unknown, stale?: boolean) => void;
  onRetry: () => void;
  onKeepMine: () => void;
  onUseTheirs: () => void;
  onReloadSchema: () => void;
}

/** At most two of a list, then "+N" that names the rest in its tooltip. */
function Stack({
  items,
  render,
  moreTitle,
  copy,
}: {
  items: readonly string[];
  render: (item: string) => ReactNode;
  moreTitle: (rest: readonly string[]) => string;
  copy: MainTableCopy["fields"];
}) {
  const rest = items.slice(2);
  return (
    <span className="flex min-w-0 items-center gap-[7px]">
      {items.slice(0, 2).map((item) => (
        <span key={item} className="flex min-w-0 items-center">
          {render(item)}
        </span>
      ))}
      {rest.length > 0 ? (
        <span
          title={moreTitle(rest)}
          className="inline-flex h-5.5 shrink-0 items-center rounded-sm bg-state-hover-strong px-2 text-caption font-semibold text-muted-foreground"
        >
          {fillTableCopy(copy.more, { count: rest.length })}
        </span>
      ) : null}
    </span>
  );
}

function ComputedCell({
  field,
  computed,
  copy,
}: {
  field: FieldDefinition;
  computed: ComputedValue | undefined;
  copy: MainTableCopy["fields"];
}) {
  const view = computedText(field, computed, copy);
  return (
    <span
      role="img"
      aria-label={
        view.tone === "value" || view.tone === "stale"
          ? fillTableCopy(copy.computedValue, { value: view.text })
          : view.title
      }
      title={view.title}
      data-computed-state={view.tone}
      className={cn(
        "ml-auto inline-flex min-w-0 items-center gap-1.25 text-nav font-normal tabular-nums text-foreground",
        // Dimmed by italic and the spinner, never by opacity: the text keeps its 4.5:1 contrast in both themes.
        view.tone === "stale" && "gap-[7px] italic",
        view.tone === "error" &&
          "text-caption leading-[1.45] font-semibold text-danger-text",
        view.tone === "empty" && "text-meta-foreground",
      )}
    >
      {view.tone === "error" ? (
        <AlertCircle className="size-4 flex-none" aria-hidden="true" />
      ) : null}
      <span className="truncate">{view.text}</span>
      {view.tone === "stale" ? (
        <Loader2
          className="size-4.5 flex-none animate-spin motion-reduce:animate-none"
          aria-hidden="true"
        />
      ) : null}
    </span>
  );
}

/**
 * What a custom field column shows in one cell, and its editor. Formula and
 * Rollup cells are read-only (the ƒx mark and the lock sit in the column header); a viewer, or a type with no
 * inline editor, gets the value alone (the lock sits in the column header).
 * A write shows "Đang lưu" while it runs, an inline error with "Thử lại"
 * when it fails (the typed value stays), and the in-cell conflict choice.
 */
export function TableFieldCell({
  field,
  cell,
  rowKey,
  copy,
  members,
  editable,
  write,
  onWrite,
  onRetry,
  onKeepMine,
  onUseTheirs,
  onReloadSchema,
}: TableFieldCellProps) {
  const f = copy.fields;
  const spec = specOf(field);
  const useNew = useRef<HTMLButtonElement>(null);
  if (spec.computed)
    return <ComputedCell field={field} computed={cell.computed} copy={f} />;

  const value = write ? write.value : cell.value;
  const text = fieldText(field, value, members);
  const label = fillTableCopy(f.edit, { field: field.name, key: rowKey });
  const canEdit = editable && spec.editable;
  const ids = fieldIds(value);
  const empty = spec.isEmpty(value);

  const optionOf = (id: string): EditorOption => {
    const option = field.options.find((entry) => entry.id === id);
    return { id, label: option?.label ?? id, color: option?.color };
  };
  const memberOf = (id: string): EditorOption => {
    const member = members.find((entry) => entry.userId === id);
    return { id, label: member?.name ?? id, initials: member?.initials ?? "?" };
  };
  const notSet = (
    <span className="truncate italic text-meta-foreground">{f.notSet}</span>
  );
  const dash = <span className="text-meta-foreground">{f.empty}</span>;

  let display: ReactNode;
  let editor: ReactNode;
  switch (field.fieldType) {
    case "checkbox": {
      const on = value === true;
      editor = (
        <CheckboxEditor
          checked={on}
          editable={canEdit}
          saving={write?.status === "pending"}
          caption={
            write?.status === "pending"
              ? f.saving
              : on
                ? fillTableCopy(f.checkedCaption, {
                    field: field.name.toLocaleLowerCase(),
                  })
                : f.uncheckedCaption
          }
          label={fillTableCopy(on ? f.checked : f.unchecked, {
            field: field.name,
            key: rowKey,
          })}
          onCommit={onWrite}
        />
      );
      break;
    }
    case "select":
    case "multi_select":
    case "person": {
      const isPerson = field.fieldType === "person";
      const multi = isPerson
        ? field.multiple === true
        : field.fieldType !== "select";
      const options: EditorOption[] = isPerson
        ? members.map((member) => ({
            id: member.userId,
            label: member.name,
            initials: member.initials,
          }))
        : field.options.map((option) => optionOf(option.id));
      display = empty ? (
        dash
      ) : (
        <Stack
          items={ids}
          copy={f}
          moreTitle={(rest) =>
            rest
              .map((id) => (isPerson ? memberOf(id) : optionOf(id)).label)
              .join(", ")
          }
          render={(id) =>
            isPerson ? (
              <PersonMark option={memberOf(id)} />
            ) : (
              <OptionChip option={optionOf(id)} />
            )
          }
        />
      );
      editor = canEdit ? (
        <OptionEditor
          field={field}
          options={options}
          selected={ids}
          multi={multi}
          label={label}
          copy={f}
          onCommit={onWrite}
        >
          {display}
        </OptionEditor>
      ) : (
        display
      );
      break;
    }
    case "date": {
      display = empty ? (
        notSet
      ) : (
        <span className="text-nav tabular-nums text-muted-foreground">
          {text}
        </span>
      );
      editor = canEdit ? (
        <DateEditor
          value={typeof value === "string" ? value : ""}
          text={text}
          label={label}
          copy={f}
          tableCopy={copy}
          onCommit={onWrite}
        >
          {display}
        </DateEditor>
      ) : (
        display
      );
      break;
    }
    case "long_text": {
      display = empty ? (
        dash
      ) : (
        <span className="min-w-0 truncate text-nav text-muted-foreground">
          {text}
        </span>
      );
      editor = canEdit ? (
        <LongTextEditor text={text} label={label} copy={f} onCommit={onWrite}>
          {display}
        </LongTextEditor>
      ) : (
        <span title={text || undefined} className="flex min-w-0">
          {display}
        </span>
      );
      break;
    }
    default: {
      // Text, Number and Link are typed in the cell; the other types only show.
      const isNumber = field.fieldType === "number";
      const isLink = field.fieldType === "link";
      const unit = isNumber && !empty && field.unit ? field.unit : null;
      display = empty ? (
        dash
      ) : (
        <span
          className={cn(
            "min-w-0 truncate text-nav",
            field.fieldType === "text" && "text-muted-foreground",
            isNumber && "ml-auto tabular-nums",
            isLink && "text-state-selected-foreground underline",
          )}
        >
          {isLink ? text.replace(/^https?:\/\//, "") : text}
          {unit ? (
            <span className="ml-1 text-crumb text-meta-foreground">{unit}</span>
          ) : null}
        </span>
      );
      const open =
        isLink && !empty && /^https?:\/\//.test(text) ? (
          <a
            href={text}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={f.openLink}
            title={f.openLink}
            className="inline-flex flex-none text-meta-foreground hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
          >
            <ExternalLink className="size-4" aria-hidden="true" />
          </a>
        ) : null;
      const inline =
        canEdit && (field.fieldType === "text" || isNumber || isLink) ? (
          <InlineInputEditor
            field={field}
            value={value}
            text={text}
            label={label}
            copy={f}
            alignEnd={isNumber}
            unit={isNumber ? field.unit : null}
            trailing={open}
            onCommit={onWrite}
          >
            {display}
          </InlineInputEditor>
        ) : (
          <>
            <span title={text || undefined} className="flex min-w-0 flex-1">
              {display}
            </span>
            {open}
          </>
        );
      editor = inline;
    }
  }

  if (!write)
    return (
      <>
        {editor}
        {/* A viewer's cell ends with a lock that names the field (`WM-Field-Editors`, "Chỉ xem"). */}
        {!editable && spec.editable ? (
          <span
            role="img"
            aria-label={fillTableCopy(f.viewOnlyLabel, { field: field.name })}
            title={f.viewOnly}
            className={cn(
              "inline-flex flex-none text-meta-foreground",
              field.fieldType !== "number" && "ml-auto",
            )}
          >
            <Lock className="size-4" aria-hidden="true" />
          </span>
        ) : null}
      </>
    );
  const valueLabel = text || f.empty;
  if (write.status === "reloading")
    return (
      <span
        role="status"
        aria-label={f.staleReloading}
        className="h-3 w-[70px] animate-pulse rounded-sm bg-state-hover-strong motion-reduce:animate-none"
      />
    );
  if (write.status === "schema" && write.schema) {
    const { schema } = write;
    const who = schema.byName ?? f.staleSomeone;
    const title = fillTableCopy(
      schema.kind === "field-deleted"
        ? f.staleDeleted
        : schema.kind === "option-removed"
          ? f.staleOption
          : f.staleType,
      { who, field: field.name },
    );
    return (
      <AnchoredNote
        tone="danger"
        className="bg-danger-soft ring-2 ring-danger-text"
        noteClassName="w-[330px] gap-2.5 rounded-[12px] p-3"
        note={
          <div role="alert" className="flex flex-col gap-2.5">
            <span className="flex items-start gap-2 font-semibold text-danger-text">
              <AlertCircle className="size-4 flex-none" aria-hidden="true" />
              {title}
            </span>
            <span className="leading-normal text-muted-foreground">
              {fillTableCopy(f.staleDeletedBody, { value: valueLabel })}
            </span>
            <span className="flex justify-end gap-2">
              <button
                type="button"
                onClick={onUseTheirs}
                className="h-8 cursor-pointer rounded-[7px] border border-line-strong bg-popover px-3 text-compact font-medium whitespace-nowrap hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring dark:bg-card"
              >
                {f.staleDrop}
              </button>
              <button
                type="button"
                onClick={onReloadSchema}
                className="h-8 cursor-pointer rounded-[7px] border-0 bg-primary px-3 text-compact font-semibold whitespace-nowrap text-primary-foreground focus-visible:outline-2 focus-visible:outline-ring"
              >
                {f.staleReload}
              </button>
            </span>
          </div>
        }
      >
        {editor}
      </AnchoredNote>
    );
  }
  if (write.status === "pending")
    return (
      <>
        {editor}
        <span
          role="status"
          className={cn(
            "inline-flex flex-none items-center gap-1 text-crumb text-muted-foreground",
            field.fieldType === "checkbox" && "ml-auto",
          )}
        >
          <Loader2
            className={cn(
              "animate-spin motion-reduce:animate-none",
              // The saving row draws an 18 px ring at the cell's right padding.
              field.fieldType === "checkbox" ? "size-4.5" : "size-3",
            )}
            aria-hidden="true"
          />
          {field.fieldType === "checkbox" ? null : f.saving}
        </span>
      </>
    );
  if (write.status === "error" && write.required)
    return (
      <AnchoredNote
        tone="danger"
        className="ring-1 ring-danger-text"
        note={
          <div role="alert" className="flex flex-col gap-1">
            <span className="flex items-center gap-1 text-crumb font-semibold text-danger-text">
              <AlertCircle className="size-3.5" aria-hidden="true" />
              {fillTableCopy(f.requiredRefused, { field: field.name })}
            </span>
            <span>{f.requiredRefusedBody}</span>
            <button
              type="button"
              onClick={onUseTheirs}
              className="h-6 w-fit cursor-pointer rounded-md border border-line-strong bg-popover px-2 text-crumb font-medium text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring dark:bg-card"
            >
              {f.requiredDismiss}
            </button>
          </div>
        }
      >
        {editor}
      </AnchoredNote>
    );
  if (write.status === "error")
    return (
      <>
        <AnchoredNote
          tone="danger"
          className="ring-1 ring-danger-text"
          note={
            <div role="alert" className="flex flex-col gap-1">
              <span className="flex items-center gap-1 text-crumb font-semibold text-danger-text">
                <AlertCircle className="size-3.5" aria-hidden="true" />
                {f.saveFailed}
              </span>
              <span>{f.saveFailedBody}</span>
            </div>
          }
        >
          {editor}
        </AnchoredNote>
        <button
          type="button"
          onClick={onRetry}
          className="h-6 flex-none cursor-pointer rounded-md border border-line-strong bg-popover px-2 text-crumb font-medium text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring dark:bg-card"
        >
          {f.retry}
        </button>
      </>
    );
  return (
    <AnchoredNote
      tone="warning"
      className="ring-1 ring-warning-text"
      focusRef={useNew}
      onEscape={onUseTheirs}
      note={
        <div
          role="alertdialog"
          aria-label={f.conflictLabel}
          className="flex flex-col gap-2"
        >
          <span className="font-semibold text-warning-subtle-foreground">
            {fillTableCopy(f.conflictTitle, {
              field: field.name,
              value: fieldText(field, cell.value, members) || f.empty,
            })}
          </span>
          <span>{fillTableCopy(f.conflictYours, { value: valueLabel })}</span>
          <span className="flex gap-2">
            <button
              type="button"
              onClick={onKeepMine}
              className="h-7 cursor-pointer rounded-md border border-line-strong bg-popover px-2.5 text-crumb font-medium whitespace-nowrap hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring dark:bg-card"
            >
              {f.conflictMine}
            </button>
            <button
              ref={useNew}
              type="button"
              onClick={onUseTheirs}
              className="h-7 cursor-pointer rounded-md border-0 bg-primary px-2.5 text-crumb font-semibold whitespace-nowrap text-primary-foreground focus-visible:outline-2 focus-visible:outline-ring"
            >
              {fillTableCopy(f.conflictTheirs, {
                value: fieldText(field, cell.value, members) || f.empty,
              })}
            </button>
          </span>
        </div>
      }
    >
      {editor}
    </AnchoredNote>
  );
}
