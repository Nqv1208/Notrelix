import { useLayoutEffect, useRef, useState, type ReactNode } from "react";
import {
  Check,
  Columns3,
  Info,
  Loader2,
  Lock,
  TriangleAlert,
  Undo2,
  Users,
} from "lucide-react";
import {
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import type {
  FieldDefinition,
  TableDensity,
} from "@notrelix/work-management-core";
import { kanbanPillClass } from "../kanban/kanban-chrome";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";
import {
  allTableColumns,
  moveColumnId,
  placeColumnId,
  visibleTableColumns,
} from "./table-columns";

/** The member's own columns and what they can do with them. */
export interface TableColumnState {
  /** The member has hidden, ordered or resized columns of their own. */
  personal: boolean;
  /** An editor may make them the shared view's; a viewer sees why not. */
  canSaveToView: boolean;
  savingToView: boolean;
  onSaveToView: () => void;
  /** Drops the member's columns: the shared view's show again. */
  onReset: () => void;
  /** The last save of the member's columns failed; the screen keeps them. */
  saveFailed: boolean;
  onRetrySave: () => void;
  /** The shared view refused the member's columns. */
  saveToViewFailed: boolean;
  /** Someone saved the shared view first: saving again overwrites their change. */
  saveToViewConflict: boolean;
  /** Who saved first, when the server named them. */
  saveToViewConflictBy?: string | null;
  /** The shared view just took the member's columns. */
  savedToView: boolean;
}

export interface TableColumnsMenuProps {
  copy: MainTableCopy;
  customFields: readonly FieldDefinition[];
  /** The column order in effect (personal, else the shared view's); empty = the default order. */
  columnOrder: readonly string[];
  onColumnOrderChange?: (order: string[]) => void;
  columnState?: TableColumnState;
  hiddenColumnIds: readonly string[];
  onColumnVisibleChange: (columnId: string, visible: boolean) => void;
  density: TableDensity;
  onDensityChange: (density: TableDensity) => void;
}

const BTN =
  "inline-flex h-11 cursor-pointer items-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-3.5 text-[13px] font-medium whitespace-nowrap text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring aria-disabled:cursor-not-allowed disabled:cursor-not-allowed disabled:opacity-50";
const BTN_PRIMARY =
  "border-primary bg-primary font-semibold text-primary-foreground hover:bg-primary/90";
const BTN_LOCKED =
  "border-line-strong bg-state-hover-strong text-meta-foreground";

const TONE = {
  err: "bg-danger-subtle text-danger-text dark:bg-danger-text/14",
  warn: "bg-warning-subtle text-warning-text dark:bg-warning-text/14",
  ok: "bg-success-subtle text-success-text dark:bg-success-text/14",
} as const;

/** `.bn`: a notice inside the menu, with its action when the person can act. */
function Notice({
  tone,
  title,
  body,
  action,
  icon,
  alert,
}: {
  tone: keyof typeof TONE;
  title: string;
  body: string;
  action?: ReactNode;
  icon: ReactNode;
  alert?: boolean;
}) {
  return (
    <div className="p-1">
      <div
        role={alert ? "alert" : "status"}
        className={cn(
          "flex items-start gap-2.5 rounded-[10px] px-3.5 py-2.5 text-[13px] leading-[1.4]",
          TONE[tone],
        )}
      >
        <span className="mt-0.5 flex">{icon}</span>
        <span className="flex min-w-0 flex-1 flex-col gap-px">
          <b className="font-semibold text-foreground">{title}</b>
          <span>{body}</span>
        </span>
        {action ? (
          <span className="flex flex-none items-center">{action}</span>
        ) : null}
      </div>
    </div>
  );
}

/**
 * The "Cột" menu: which columns this person sees and in what order, kept on their own board preference.
 * A row toggles with Space, moves with Alt+Up / Alt+Down or by dragging; the buttons under the list put the
 * person's columns on the shared view (an editor) or drop them (anyone).
 */
export function TableColumnsMenu({
  copy,
  customFields,
  columnOrder,
  onColumnOrderChange,
  columnState,
  hiddenColumnIds,
  onColumnVisibleChange,
  density,
  onDensityChange,
}: TableColumnsMenuProps) {
  const t = copy.toolbar;
  const [open, setOpen] = useState(false);
  const columnName = (column: ReturnType<typeof allTableColumns>[number]) =>
    column.field?.name ?? copy.columns[column.id as keyof typeof copy.columns];
  const ordered = visibleTableColumns([], customFields, columnOrder);
  const shown = visibleTableColumns(
    hiddenColumnIds,
    customFields,
    columnOrder,
  ).length;
  const movable = ordered
    .filter((entry) => entry.id !== "key" && entry.id !== "title")
    .map((entry) => entry.id);
  const heading = fillTableCopy(t.columns.heading, {
    shown,
    total: ordered.length,
  });

  // A moved row is re-inserted by the browser and loses focus: put it back on the same column.
  const refocus = useRef<string | null>(null);
  const list = useRef<HTMLDivElement>(null);
  useLayoutEffect(() => {
    if (!refocus.current) return;
    list.current
      ?.querySelector<HTMLElement>(`[data-column="${refocus.current}"] button`)
      ?.focus();
    refocus.current = null;
  });
  const dragging = useRef<string | null>(null);
  const [over, setOver] = useState<string | null>(null);

  const c = columnState;
  const saveDisabled = !c || !c.personal || c.savingToView;
  const canSave = c?.canSaveToView ?? false;
  const triggerLabel = `${t.columns.label} ${shown}/${ordered.length}`;

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          aria-label={triggerLabel}
          className={`${kanbanPillClass} tablet:size-11 tablet:justify-center tablet:px-0`}
        >
          <Columns3 className="size-4" aria-hidden="true" />
          {/* The tablet button is the icon alone (44 px); the name and count stay for assistive tech. */}
          <span className="tablet:sr-only">
            {t.columns.label}{" "}
            <b className="font-semibold leading-[1.45] text-foreground untablet:@max-[820px]/bar:sr-only">
              {shown}/{ordered.length}
            </b>
          </span>
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        role="dialog"
        aria-label={t.columns.label}
        className="w-[380px] max-w-[calc(100vw-2rem)] gap-0.5 rounded-[12px] border border-line-strong bg-popover p-1.5 text-[14px] shadow-popover ring-0"
      >
        <div className="px-2.5 pt-1.5 pb-0.5 text-[12px] leading-[1.45] font-semibold text-meta-foreground">
          {heading}
        </div>
        <div ref={list} className="max-h-[300px] overflow-auto">
          {ordered.map((column) => {
            const name = columnName(column);
            const fixed = Boolean(column.locked);
            const checked = fixed || !hiddenColumnIds.includes(column.id);
            const draggable = !fixed;
            return (
              <label
                key={column.id}
                data-column={column.id}
                draggable={draggable || undefined}
                title={
                  draggable
                    ? fillTableCopy(t.columns.moveColumn, { name })
                    : undefined
                }
                onDragStart={(event) => {
                  dragging.current = column.id;
                  event.dataTransfer.effectAllowed = "move";
                }}
                onDragOver={(event) => {
                  if (!dragging.current || !draggable) return;
                  event.preventDefault();
                  setOver(column.id);
                }}
                onDragLeave={() => setOver(null)}
                onDrop={(event) => {
                  event.preventDefault();
                  const from = dragging.current;
                  dragging.current = null;
                  setOver(null);
                  const next = from
                    ? placeColumnId(movable, from, column.id)
                    : null;
                  if (next) onColumnOrderChange?.(next);
                }}
                onDragEnd={() => {
                  dragging.current = null;
                  setOver(null);
                }}
                className={cn(
                  "flex min-h-[34px] items-center gap-2.5 rounded-[7px] px-2.5 py-1.5 text-[13.5px] leading-[1.45] text-foreground max-md:min-h-11",
                  draggable && "cursor-grab",
                  over === column.id && "bg-state-hover-strong",
                )}
              >
                <button
                  type="button"
                  role="checkbox"
                  aria-checked={checked}
                  aria-label={name}
                  disabled={fixed}
                  aria-keyshortcuts={
                    draggable ? "Alt+ArrowUp Alt+ArrowDown" : undefined
                  }
                  onClick={() => onColumnVisibleChange(column.id, !checked)}
                  onKeyDown={(event) => {
                    // Alt+Up / Alt+Down move the column; the order is the member's own.
                    if (
                      !draggable ||
                      !event.altKey ||
                      (event.key !== "ArrowUp" && event.key !== "ArrowDown")
                    )
                      return;
                    event.preventDefault();
                    const next = moveColumnId(
                      movable,
                      column.id,
                      event.key === "ArrowUp" ? -1 : 1,
                    );
                    if (!next) return;
                    refocus.current = column.id;
                    onColumnOrderChange?.(next);
                  }}
                  className={cn(
                    "grid size-[18px] flex-none place-items-center rounded-[5px] border-[1.5px] border-line-strong bg-popover p-0 text-primary-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
                    checked && "border-primary bg-primary",
                    fixed && "cursor-default",
                  )}
                >
                  {checked ? (
                    <Check className="size-[13px]" aria-hidden="true" />
                  ) : null}
                </button>
                <span>{name}</span>
                {fixed ? (
                  <span
                    role="img"
                    aria-label={t.columns.fixed}
                    className="ml-auto text-meta-foreground"
                    title={t.columns.fixed}
                  >
                    <Lock className="inline size-[13px]" aria-hidden="true" />
                  </span>
                ) : null}
              </label>
            );
          })}
        </div>
        {c ? (
          <>
            <div className="mt-1 flex items-center gap-2 border-t border-line px-2.5 py-2 text-[12.5px] leading-[1.45] text-muted-foreground">
              <Users className="size-[13px] flex-none" aria-hidden="true" />
              {t.columns.own}
            </div>
            {c.savingToView ? (
              <div
                role="status"
                className="flex items-center gap-2 px-2.5 py-2 text-[12.5px] leading-[1.45] text-muted-foreground"
              >
                <Loader2
                  className="size-4 animate-spin motion-reduce:animate-none"
                  aria-hidden="true"
                />
                {t.columns.saving}
              </div>
            ) : null}
            {c.saveFailed ? (
              <Notice
                tone="err"
                alert
                icon={<TriangleAlert className="size-4" aria-hidden="true" />}
                title={t.columns.saveFailed}
                body={t.columns.saveFailedBody}
                action={
                  <button type="button" className={BTN} onClick={c.onRetrySave}>
                    {t.columns.retry}
                  </button>
                }
              />
            ) : null}
            {c.saveToViewFailed ? (
              <Notice
                tone="err"
                alert
                icon={<TriangleAlert className="size-4" aria-hidden="true" />}
                title={t.columns.saveToViewFailed}
                body={t.columns.saveToViewFailedBody}
                action={
                  <button
                    type="button"
                    className={BTN}
                    onClick={c.onSaveToView}
                  >
                    {t.columns.retry}
                  </button>
                }
              />
            ) : null}
            {c.saveToViewConflict ? (
              <Notice
                tone="warn"
                alert
                icon={<TriangleAlert className="size-4" aria-hidden="true" />}
                title={
                  c.saveToViewConflictBy
                    ? fillTableCopy(t.columns.conflictBy, {
                        name: c.saveToViewConflictBy,
                      })
                    : t.columns.conflict
                }
                body={t.columns.conflictBody}
                action={
                  <button
                    type="button"
                    className={BTN}
                    onClick={c.onSaveToView}
                  >
                    {t.columns.overwrite}
                  </button>
                }
              />
            ) : null}
            {c.savedToView ? (
              <Notice
                tone="ok"
                icon={<Check className="size-4" aria-hidden="true" />}
                title={t.columns.saved}
                body={t.columns.savedBody}
              />
            ) : null}
            <div className="flex justify-end gap-2 px-2.5 pt-2 pb-1.5">
              <button
                type="button"
                className={BTN}
                aria-disabled={!c.personal || undefined}
                onClick={() => {
                  if (c.personal) c.onReset();
                }}
              >
                <Undo2 className="size-4" aria-hidden="true" />
                {t.columns.backToView}
              </button>
              <button
                type="button"
                className={cn(BTN, canSave ? BTN_PRIMARY : BTN_LOCKED)}
                // A viewer keeps the button, reads why in place and cannot press it.
                aria-disabled={!canSave || saveDisabled || undefined}
                aria-describedby={canSave ? undefined : "columns-save-reason"}
                onClick={() => {
                  if (canSave && !saveDisabled) c.onSaveToView();
                }}
              >
                {canSave ? null : (
                  <Lock className="size-4" aria-hidden="true" />
                )}
                {t.columns.saveToView}
              </button>
            </div>
            {canSave ? null : (
              <span
                id="columns-save-reason"
                className="flex items-center gap-1.5 px-2.5 pb-2 text-[12px] leading-[1.45] text-meta-foreground"
              >
                <Info className="size-[13px] flex-none" aria-hidden="true" />
                {t.columns.saveToViewViewer}
              </span>
            )}
          </>
        ) : null}
        <div
          role="radiogroup"
          aria-label={t.columns.density}
          className="mt-1 flex items-center gap-2 border-t border-line px-2.5 py-2 text-[12.5px] text-muted-foreground"
        >
          <span className="mr-auto">{t.columns.density}</span>
          {(["comfortable", "compact"] as const).map((value) => (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={density === value}
              onClick={() => value !== density && onDensityChange(value)}
              className={cn(
                "h-8 cursor-pointer rounded-[7px] border-0 bg-transparent px-2.5 text-[12.5px] text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring max-md:h-11",
                density === value &&
                  "bg-state-selected font-semibold text-state-selected-foreground",
              )}
            >
              {t.columns[value]}
            </button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}
