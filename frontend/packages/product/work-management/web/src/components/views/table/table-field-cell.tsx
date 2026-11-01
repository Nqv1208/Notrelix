import { useRef, type ReactNode } from "react";
import { AlertCircle, ExternalLink, Loader2 } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import {
  specOf,
  type ComputedValue,
  type FieldDefinition,
} from "@notrelix/work-management-core";
import { identityMarkClass } from "../../shared/item-display";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";
import {
  AnchoredNote,
  CheckboxEditor,
  DateEditor,
  InlineInputEditor,
  LongTextEditor,
  OptionEditor,
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
  readonly status: "pending" | "error" | "conflict";
  /** The value the person wrote; shown while the write has not settled. */
  readonly value: unknown;
}

/** What the surface hands a row to edit its custom field cells. */
export interface TableFieldEditing {
  /** The viewer may write field values. */
  readonly canEdit: boolean;
  readonly members: readonly TableFieldMember[];
  readonly writeOf: (rowId: string, fieldId: string) => CellWrite | undefined;
  readonly onWrite: (
    rowId: string,
    fieldId: string,
    value: unknown,
    stale?: boolean,
  ) => void;
  readonly onRetry: (rowId: string, fieldId: string) => void;
  readonly onKeepMine: (rowId: string, fieldId: string) => void;
  readonly onUseTheirs: (rowId: string, fieldId: string) => void;
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
}

const CHIP_CLASS =
  "inline-flex h-5.5 min-w-0 items-center gap-1.5 rounded-sm bg-state-hover px-2 text-micro font-semibold text-foreground";

function OptionChip({ option }: { option: EditorOption }) {
  return (
    <span className={CHIP_CLASS}>
      {option.color ? (
        <span
          aria-hidden="true"
          className="size-2 shrink-0 rounded-full"
          style={{ backgroundColor: option.color }}
        />
      ) : null}
      <span className="truncate">{option.label}</span>
    </span>
  );
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
    <span className="flex min-w-0 items-center gap-1">
      {items.slice(0, 2).map((item) => (
        <span key={item} className="flex min-w-0 items-center">
          {render(item)}
        </span>
      ))}
      {rest.length > 0 ? (
        <span
          title={moreTitle(rest)}
          className="inline-flex h-5.5 shrink-0 items-center rounded-sm bg-state-hover-strong px-1.5 text-micro font-semibold text-muted-foreground"
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
        "inline-flex min-w-0 items-center gap-1.25 text-nav tabular-nums text-muted-foreground",
        // Dimmed by italic and the spinner, never by opacity: the text keeps its 4.5:1 contrast in both themes.
        view.tone === "stale" && "italic",
        view.tone === "error" && "font-semibold text-danger-text",
        view.tone === "empty" && "text-meta-foreground",
      )}
    >
      <span
        aria-hidden="true"
        className="inline-flex h-4.5 flex-none items-center rounded-[5px] bg-state-hover-strong px-1.25 font-mono text-micro font-medium text-muted-foreground"
      >
        ƒx
      </span>
      {view.tone === "error" ? (
        <AlertCircle className="size-3.5 flex-none" aria-hidden="true" />
      ) : null}
      <span className="truncate">{view.text}</span>
      {view.tone === "stale" ? (
        <Loader2
          className="size-3 flex-none animate-spin motion-reduce:animate-none"
          aria-hidden="true"
        />
      ) : null}
    </span>
  );
}

/**
 * What a custom field column shows in one cell, and its editor. Formula and
 * Rollup cells are read-only with the ƒx mark; a viewer, or a type with no
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
      const multi = field.fieldType !== "select";
      const options: EditorOption[] = isPerson
        ? members.map((member) => ({
            id: member.userId,
            label: member.name,
            initials: member.initials,
          }))
        : field.options.map((option) => optionOf(option.id));
      const mark = (id: string) => {
        const person = memberOf(id);
        return (
          <span
            aria-hidden="true"
            title={person.label}
            className={cn(
              "flex size-6 shrink-0 items-center justify-center rounded-full text-avatar-md select-none",
              identityMarkClass(id),
            )}
          >
            {person.initials}
          </span>
        );
      };
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
            isPerson ? mark(id) : <OptionChip option={optionOf(id)} />
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
      const inline = (
        <>
          {canEdit && (field.fieldType === "text" || isNumber || isLink) ? (
            <InlineInputEditor
              field={field}
              value={value}
              text={text}
              label={label}
              copy={f}
              alignEnd={isNumber}
              onCommit={onWrite}
            >
              {display}
            </InlineInputEditor>
          ) : (
            <span title={text || undefined} className="flex min-w-0 flex-1">
              {display}
            </span>
          )}
          {isLink && !empty && /^https?:\/\//.test(text) ? (
            <a
              href={text}
              target="_blank"
              rel="noopener noreferrer"
              aria-label={f.openLink}
              title={f.openLink}
              className="inline-flex flex-none text-meta-foreground hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
            >
              <ExternalLink className="size-3.5" aria-hidden="true" />
            </a>
          ) : null}
        </>
      );
      editor = inline;
    }
  }

  if (!write) return <>{editor}</>;
  const valueLabel = text || f.empty;
  if (write.status === "pending")
    return (
      <>
        {editor}
        <span
          role="status"
          className="inline-flex flex-none items-center gap-1 text-crumb text-muted-foreground"
        >
          <Loader2
            className="size-3 animate-spin motion-reduce:animate-none"
            aria-hidden="true"
          />
          {f.saving}
        </span>
      </>
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
