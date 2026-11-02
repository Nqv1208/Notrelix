import {
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
} from "react";
import {
  ChevronRight,
  Flag,
  MessageCircle,
  Paperclip,
  SquareCheck,
} from "lucide-react";
import { PriorityGlyph } from "../../shared/priority-glyph";
import {
  cn,
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import {
  ITEM_LABEL_TONE_CLASS,
  identityMarkClass,
} from "../../shared/item-display";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";
import { STATUS_TINT_CLASS, tablePanelClass } from "./table-chrome";
import type { TableColumnSpec } from "./table-columns";
import type { TableRowModel, TableStatusOption } from "./table-model";
import {
  AssigneeEditor,
  DueEditor,
  PriorityEditor,
  type TableEditors,
} from "./table-cell-editors";
import { TableFieldCell, type TableFieldEditing } from "./table-field-cell";
import { StatusDot, TableCheckbox } from "./table-parts";
import { useTableLayout } from "./table-layout";
import { TableStatusPicker } from "./table-status-picker";

/** The row's own menu (right click, or the context-menu key). */
export interface TableRowActions {
  /** Absent: the viewer may not add items, so no "Nhân bản". */
  readonly onDuplicate?: () => void;
  readonly onCopyLink: () => void;
  /** Absent: the viewer may not archive, so no "Lưu trữ". */
  readonly onArchive?: () => void;
  /** Absent: the viewer may not delete, so no "Xoá". */
  readonly onDelete?: () => void;
  /** Absent: the viewer may not move items, so no "Chuyển sang board khác". */
  readonly onMoveToBoard?: () => void;
}

export interface TableRowProps {
  row: TableRowModel;
  /** The row's place among every row of the table, loaded or not (a table read in pages). */
  rowIndex?: number | undefined;
  copy: MainTableCopy;
  columns: readonly TableColumnSpec[];
  /** Accent of the row's group: the left edge takes its colour. */
  accent: string;
  statusOptions: readonly TableStatusOption[];
  selected: boolean;
  /** The item whose side sheet is open. */
  active: boolean;
  /** The row that takes the Tab stop of the grid. */
  tabStop: boolean;
  /** The viewer may change the status of items. */
  canEdit: boolean;
  /** In-place editors of assignee, due date and priority; absent = read-only. */
  editors?: TableEditors;
  /** Editing of the custom field cells; absent = they are read-only. */
  fieldEditing?: TableFieldEditing;
  /** The row's menu; absent = no menu. */
  actions?: TableRowActions;
  /** Saves a new title typed in place; absent = the title cannot be edited here. */
  onRename?: (title: string) => void;
  /** The sub-items of this row can be shown under it; absent = the row has none. */
  expand?: { expanded: boolean; count: number; onToggle: () => void };
  /** The status list is open on this row. */
  editing: boolean;
  /** Option highlighted when the status list opens (stories and tests). */
  highlightStatusId?: string | null;
  onSelectedChange: (selected: boolean) => void;
  onOpen: () => void;
  onEditingChange: (editing: boolean) => void;
  onStatusChange: (optionId: string) => void;
  onKeyDown: (event: KeyboardEvent<HTMLDivElement>) => void;
}

const DUE_CLASS: Readonly<Record<TableRowModel["due"]["kind"], string>> = {
  overdue: "font-semibold text-danger-text",
  today: "font-semibold text-warning-subtle-foreground",
  date: "text-muted-foreground",
  none: "italic text-meta-foreground",
};

const PRIORITY_CLASS = {
  urgent: "font-semibold text-danger-text",
  high: "font-semibold text-priority-high-text",
  medium: "font-medium text-muted-foreground",
  low: "font-normal text-meta-foreground",
} as const;

/**
 * A cell of the row. On the tablet drawing it is 52 px tall with 12 px of padding; the checkbox and the title cell
 * stay pinned while the other columns scroll (`pinned` is the left offset in px).
 */
function Cell({
  className,
  selected,
  pinned,
  children,
}: {
  className?: string;
  selected: boolean;
  pinned?: 0 | 48;
  children?: React.ReactNode;
}) {
  const tablet = useTableLayout() === "tablet";
  return (
    <div
      role="cell"
      style={tablet && pinned !== undefined ? { left: pinned } : undefined}
      className={cn(
        "relative box-border flex min-w-0 items-center overflow-hidden border-b border-border whitespace-nowrap",
        tablet
          ? "h-13 gap-2 px-3 text-sm"
          : "h-(--table-row-height) max-md:h-11 gap-[7px] px-2.5",
        tablet && pinned !== undefined && "sticky z-3",
        tablet && pinned === 0 && "justify-center px-0.5",
        tablet && pinned === 48 && "border-r border-line-strong",
        selected
          ? "bg-row-selected"
          : cn(
              tablePanelClass,
              "group-hover/row:bg-state-hover",
              tablet && "group-data-[accent-active=true]/row:bg-state-selected",
            ),
        className,
      )}
    >
      {children}
    </div>
  );
}

/**
 * One row of the table. Its left edge carries the group's colour: faint at
 * rest, full on hover, keyboard focus and while its item is open. A click on
 * the row (or the title) opens the item; the status pill opens the status
 * list and the checkbox selects, neither opens the item. The row itself takes
 * the arrow keys, Enter and Space (open the item) and X (select) from the
 * table.
 */
export function TableRow({
  row,
  rowIndex,
  copy,
  columns,
  accent,
  statusOptions,
  selected,
  active,
  tabStop,
  canEdit,
  editors,
  fieldEditing,
  actions,
  onRename,
  expand,
  editing,
  highlightStatusId,
  onSelectedChange,
  onOpen,
  onEditingChange,
  onStatusChange,
  onKeyDown,
}: TableRowProps) {
  const keyLabel = row.key ?? row.title;
  const tablet = useTableLayout() === "tablet";
  const [renaming, setRenaming] = useState(false);
  const [draft, setDraft] = useState("");
  const settled = useRef(false);
  const refocus = useRef(false);
  const openTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const titleButton = useRef<HTMLButtonElement>(null);
  useEffect(
    () => () => {
      if (openTimer.current) clearTimeout(openTimer.current);
    },
    [],
  );
  useEffect(() => {
    if (!renaming && refocus.current) {
      refocus.current = false;
      titleButton.current?.focus();
    }
  }, [renaming]);
  const startRename = () => {
    if (!onRename) return;
    if (openTimer.current) clearTimeout(openTimer.current);
    settled.current = false;
    setDraft(row.title);
    setRenaming(true);
  };
  // Blank or unchanged input writes nothing and the old title stays, as in the sheet.
  const endRename = (save: boolean, restoreFocus: boolean) => {
    if (settled.current) return;
    settled.current = true;
    const next = draft.trim();
    if (save && next && next !== row.title) onRename?.(next);
    refocus.current = restoreFocus;
    setRenaming(false);
  };

  const render = (column: TableColumnSpec) => {
    switch (column.id) {
      case "key":
        return (
          <Cell key="key" selected={selected}>
            <span className="font-mono text-metadata leading-[1.45] text-meta-foreground">
              {row.key}
            </span>
          </Cell>
        );
      case "title":
        return (
          <Cell
            key="title"
            selected={selected}
            pinned={48}
            className={tablet ? "whitespace-normal" : undefined}
          >
            {expand ? (
              <button
                type="button"
                data-row-expand=""
                aria-expanded={expand.expanded}
                aria-label={fillTableCopy(
                  expand.expanded
                    ? copy.row.collapseChildren
                    : copy.row.expandChildren,
                  { key: keyLabel, count: expand.count },
                )}
                onClick={expand.onToggle}
                className="grid size-5 max-xl:h-11 max-xl:w-6 shrink-0 cursor-pointer place-items-center rounded-sm border-0 bg-transparent p-0 text-meta-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-ring"
              >
                <ChevronRight
                  aria-hidden="true"
                  className={cn(
                    "size-4 transition-transform duration-150",
                    expand.expanded && "rotate-90",
                  )}
                  strokeWidth={1.8}
                />
              </button>
            ) : null}
            {renaming ? (
              <input
                autoFocus
                value={draft}
                aria-label={fillTableCopy(copy.row.editTitle, {
                  key: keyLabel,
                })}
                onChange={(event) => setDraft(event.target.value)}
                onFocus={(event) => event.currentTarget.select()}
                onBlur={() => endRename(true, false)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    endRename(true, true);
                  } else if (event.key === "Escape") {
                    event.preventDefault();
                    event.stopPropagation();
                    endRename(false, true);
                  }
                }}
                className="h-6.5 min-w-0 flex-1 rounded-[7px] border border-line-strong bg-popover px-2 text-nav font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card"
              />
            ) : (
              <button
                ref={titleButton}
                type="button"
                // A double click renames, so an editor's single click waits
                // out the second click before it opens the item.
                onClick={() => {
                  if (!onRename) return onOpen();
                  if (openTimer.current) clearTimeout(openTimer.current);
                  openTimer.current = setTimeout(onOpen, 220);
                }}
                onDoubleClick={startRename}
                onKeyDown={(event) => {
                  if (!onRename) return;
                  if (event.key === "F2" || event.key === "Enter") {
                    event.preventDefault();
                    event.stopPropagation();
                    startRename();
                  }
                }}
                aria-keyshortcuts={onRename ? "F2 Enter" : undefined}
                title={row.title}
                data-row-open=""
                className={
                  tablet
                    ? // The whole cell is the target: the key and the title wrap under it, two lines at most.
                      "line-clamp-2 min-w-0 flex-1 text-left text-[14.5px] leading-[1.38] font-medium text-pretty text-foreground after:absolute after:inset-0 after:content-[''] focus-visible:outline-none focus-visible:after:rounded-[8px] focus-visible:after:outline-2 focus-visible:after:-outline-offset-3 focus-visible:after:outline-ring"
                    : "min-w-0 truncate text-left text-nav font-medium text-foreground max-md:min-h-11 focus-visible:outline-2 focus-visible:outline-ring"
                }
              >
                {tablet && row.key ? (
                  <span className="mr-1.5 font-mono text-[12.5px] font-medium text-meta-foreground">
                    {row.key}
                  </span>
                ) : null}
                {row.title}
              </button>
            )}
          </Cell>
        );
      case "status": {
        const status = row.status;
        const pill = (
          <button
            type="button"
            disabled={!canEdit}
            aria-haspopup="listbox"
            aria-expanded={editing}
            aria-label={fillTableCopy(copy.row.changeStatus, { key: keyLabel })}
            className={cn(
              "relative inline-flex h-6 items-center gap-1.5 rounded-full border-0 px-[9px] text-crumb font-semibold text-foreground",
              // A phone target is 44 px: the pill stays 24 px tall and its hit area reaches the row edges.
              "max-md:after:absolute max-md:after:-inset-y-2.5 max-md:after:inset-x-0 max-md:after:content-['']",
              // The tablet pill is 28 px tall and its hit area reaches the 44 px the drawing gives the cell.
              tablet &&
                "h-7 px-2.5 text-compact after:absolute after:-inset-y-2 after:inset-x-0 after:content-['']",
              status ? STATUS_TINT_CLASS[status.category] : "bg-state-hover",
              editing && "ring-2 ring-primary",
              canEdit ? "cursor-pointer" : "cursor-default",
            )}
          >
            <StatusDot category={status?.category ?? null} />
            {status?.name ?? copy.group.noStatus}
          </button>
        );
        return (
          <Cell key="status" selected={selected}>
            {canEdit ? (
              <Popover open={editing} onOpenChange={onEditingChange}>
                <PopoverTrigger asChild>{pill}</PopoverTrigger>
                <PopoverContent
                  align="start"
                  side="bottom"
                  aria-label={copy.status.change}
                  sideOffset={2}
                  alignOffset={-4}
                  className="w-58.5 gap-0 rounded-[10px] border border-line-strong bg-popover p-1.5 text-compact shadow-pop ring-0 dark:bg-card"
                >
                  <TableStatusPicker
                    copy={copy.status}
                    options={statusOptions}
                    currentId={status?.id ?? null}
                    initialHighlightId={highlightStatusId}
                    onSelect={(optionId) => {
                      onEditingChange(false);
                      if (optionId !== status?.id) onStatusChange(optionId);
                    }}
                  />
                </PopoverContent>
              </Popover>
            ) : (
              pill
            )}
          </Cell>
        );
      }
      case "assignee": {
        const person = row.assignee ? (
          <>
            <span
              aria-hidden="true"
              className={cn(
                "flex size-6 shrink-0 items-center justify-center rounded-full text-avatar-md select-none",
                tablet && "size-7 text-[10.5px]",
                identityMarkClass(row.assignee.userId),
              )}
            >
              {row.assignee.initials}
            </span>
            <span
              className={cn(
                "min-w-0 truncate text-nav font-normal",
                tablet && "text-sm",
              )}
            >
              {row.assignee.name}
            </span>
          </>
        ) : (
          <>
            <span
              aria-hidden="true"
              className={cn(
                "flex size-6 shrink-0 items-center justify-center rounded-full text-avatar-md font-bold text-meta-foreground shadow-[inset_0_0_0_1.5px_var(--line-strong)]",
                tablet && "size-7 text-[10.5px]",
              )}
            >
              +
            </span>
            <span
              className={cn(
                "min-w-0 truncate text-nav font-normal",
                tablet && "text-sm",
              )}
            >
              {copy.row.unassigned}
            </span>
          </>
        );
        return (
          <Cell
            key="assignee"
            selected={selected}
            className="text-muted-foreground"
          >
            {canEdit && editors ? (
              <AssigneeEditor
                copy={copy}
                members={editors.members}
                currentId={row.assignee?.userId ?? null}
                label={fillTableCopy(copy.row.changeAssignee, {
                  key: keyLabel,
                })}
                onPick={(userId) => editors.onAssignee(row.id, userId)}
              >
                {person}
              </AssigneeEditor>
            ) : (
              person
            )}
          </Cell>
        );
      }
      case "due": {
        // The tablet cell stacks the date over what the desktop grid writes instead of it ("Quá hạn 2 ngày").
        const stacked = tablet && row.due.date !== undefined;
        const due = (
          <span
            data-due={row.due.kind}
            className={cn(
              "text-nav font-normal tabular-nums",
              tablet && "flex flex-col text-sm leading-[1.25]",
              DUE_CLASS[row.due.kind],
              tablet &&
                row.due.kind === "date" &&
                "font-medium text-foreground",
            )}
          >
            {stacked ? (
              <>
                <b className="font-semibold">{row.due.date}</b>
                <small className="text-xs font-semibold">{row.due.text}</small>
              </>
            ) : (
              row.due.text
            )}
          </span>
        );
        return (
          <Cell key="due" selected={selected}>
            {canEdit && editors ? (
              <DueEditor
                copy={copy}
                dueAt={row.dueAt}
                itemKey={row.key ?? undefined}
                startAt={row.startAt}
                label={fillTableCopy(copy.row.changeDue, { key: keyLabel })}
                onPick={(day, time) =>
                  time
                    ? editors.onDue(row.id, day, time)
                    : editors.onDue(row.id, day)
                }
              >
                {due}
              </DueEditor>
            ) : (
              due
            )}
          </Cell>
        );
      }
      case "priority": {
        const priority = row.priority;
        const shown = (
          <span
            data-priority={priority ?? undefined}
            className={cn(
              "inline-flex items-center gap-1 text-crumb",
              tablet && "gap-1.5 text-sm",
              priority
                ? PRIORITY_CLASS[priority]
                : "font-normal text-meta-foreground",
            )}
          >
            {priority ? (
              tablet ? (
                <Flag className="size-3.75" aria-hidden="true" />
              ) : (
                <PriorityGlyph level={priority} />
              )
            ) : null}
            {priority ? copy.row.priority[priority] : copy.row.noPriority}
          </span>
        );
        return (
          <Cell key="priority" selected={selected}>
            {canEdit && editors && editors.priorities.length > 0 ? (
              <PriorityEditor
                options={editors.priorities}
                currentLabel={
                  row.priority ? copy.row.priority[row.priority] : null
                }
                label={fillTableCopy(copy.row.changePriority, {
                  key: keyLabel,
                })}
                onPick={(optionId) => editors.onPriority(row.id, optionId)}
              >
                {shown}
              </PriorityEditor>
            ) : (
              shown
            )}
          </Cell>
        );
      }
      case "label":
        return (
          <Cell key="label" selected={selected}>
            {row.label ? (
              <span
                data-label-tone={row.label.tone}
                className={cn(
                  "inline-flex h-5.5 items-center truncate rounded-sm px-2 text-micro font-semibold",
                  ITEM_LABEL_TONE_CLASS[row.label.tone],
                )}
              >
                {row.label.name}
              </span>
            ) : null}
          </Cell>
        );
      case "progress":
        return (
          <Cell key="progress" selected={selected}>
            {row.progress ? (
              <span
                data-child-progress=""
                role="img"
                aria-label={fillTableCopy(copy.row.childProgress, {
                  done: row.progress.done,
                  total: row.progress.total,
                })}
                className={cn(
                  "flex w-27.5 items-center gap-2 text-metadata leading-[1.45] tabular-nums text-meta-foreground",
                  tablet && "w-full text-compact text-muted-foreground",
                )}
              >
                <span
                  aria-hidden="true"
                  className={cn(
                    "block h-1.25 flex-1 overflow-hidden rounded-[3px] bg-state-hover-strong",
                    tablet && "h-1.5 w-12 flex-none",
                  )}
                >
                  <span
                    data-progress-fill=""
                    className="block h-full rounded-[3px] bg-primary"
                    style={{ width: `${row.progress.percent}%` }}
                  />
                </span>
                <span aria-hidden="true">
                  {row.progress.done}/{row.progress.total}
                </span>
              </span>
            ) : tablet ? (
              <span aria-hidden="true" className="text-meta-foreground">
                —
              </span>
            ) : null}
          </Cell>
        );
      case "checklist":
        return (
          <Cell key="checklist" selected={selected}>
            {row.checklist ? (
              <span
                data-checklist-progress=""
                className="inline-flex items-center gap-1 text-nav tabular-nums text-muted-foreground"
              >
                <SquareCheck className="size-3.5" aria-hidden="true" />
                <span aria-hidden="true">
                  {row.checklist.done}/{row.checklist.total}
                </span>
                <span className="sr-only">
                  {fillTableCopy(copy.row.checklistProgress, row.checklist)}
                </span>
              </span>
            ) : null}
          </Cell>
        );
      case "comments":
        return (
          <Cell key="comments" selected={selected}>
            {row.commentCount > 0 ? (
              <span
                data-comment-count={row.commentCount}
                className="inline-flex items-center gap-1 text-nav tabular-nums text-muted-foreground"
              >
                <MessageCircle className="size-3.5" aria-hidden="true" />
                <span aria-hidden="true">{row.commentCount}</span>
                <span className="sr-only">
                  {fillTableCopy(copy.row.commentCount, {
                    count: row.commentCount,
                  })}
                </span>
              </span>
            ) : null}
          </Cell>
        );
      case "files":
        return (
          <Cell key="files" selected={selected}>
            {row.fileCount > 0 ? (
              <span
                data-file-count={row.fileCount}
                className="inline-flex items-center gap-1 text-nav tabular-nums text-muted-foreground"
              >
                <Paperclip className="size-3.5" aria-hidden="true" />
                <span aria-hidden="true">{row.fileCount}</span>
                <span className="sr-only">
                  {fillTableCopy(copy.row.fileCount, { count: row.fileCount })}
                </span>
              </span>
            ) : null}
          </Cell>
        );
      case "updated":
        return (
          <Cell key="updated" selected={selected}>
            <span
              className={cn(
                "text-crumb text-meta-foreground",
                tablet && "text-nav",
              )}
            >
              {row.updatedText}
            </span>
          </Cell>
        );
      case "start":
      case "created":
      case "completed": {
        const text =
          column.id === "start"
            ? row.startText
            : column.id === "created"
              ? row.createdText
              : row.completedText;
        return (
          <Cell key={column.id} selected={selected}>
            <span className="text-nav font-normal tabular-nums text-muted-foreground">
              {text}
            </span>
          </Cell>
        );
      }
      default: {
        const field = column.field;
        if (!field) return null;
        return (
          <Cell
            key={column.id}
            selected={selected}
            className="focus-within:outline-2 focus-within:-outline-offset-2 focus-within:outline-ring"
          >
            <TableFieldCell
              field={field}
              cell={row.fields[field.id] ?? { value: undefined }}
              rowKey={keyLabel}
              copy={copy}
              members={fieldEditing?.members ?? []}
              editable={Boolean(canEdit && fieldEditing?.canEdit)}
              write={fieldEditing?.writeOf(row.id, field.id)}
              onWrite={(value, stale) =>
                fieldEditing?.onWrite(row.id, field.id, value, stale)
              }
              onRetry={() => fieldEditing?.onRetry(row.id, field.id)}
              onKeepMine={() => fieldEditing?.onKeepMine(row.id, field.id)}
              onUseTheirs={() => fieldEditing?.onUseTheirs(row.id, field.id)}
            />
          </Cell>
        );
      }
    }
  };

  const rowElement = (
    <div
      role="row"
      aria-rowindex={rowIndex}
      tabIndex={tabStop ? 0 : -1}
      aria-selected={selected}
      aria-current={active ? "true" : undefined}
      data-row-id={row.id}
      onKeyDown={(event) => {
        if (
          event.key === "F2" &&
          event.target === event.currentTarget &&
          onRename
        ) {
          event.preventDefault();
          startRename();
          return;
        }
        onKeyDown(event);
      }}
      onClick={(event) => {
        // Controls inside the row (status pill, checkbox, the title button)
        // do their own thing; a status list in a portal also bubbles here.
        if (!event.currentTarget.contains(event.target as Node)) return;
        if (
          (event.target as HTMLElement).closest(
            "button, input, [role=checkbox]",
          )
        )
          return;
        onOpen();
      }}
      data-accent-active={active ? "true" : undefined}
      className={cn(
        "group/row relative grid min-w-full cursor-pointer focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring",
        editing && "z-5",
        // The open item's row wears the 2 px ring of the tablet drawing.
        tablet &&
          active &&
          "after:pointer-events-none after:absolute after:inset-0 after:bottom-px after:z-4 after:shadow-[inset_0_0_0_2px_var(--color-primary)] after:content-['']",
      )}
      style={
        {
          gridTemplateColumns: "var(--table-grid)",
          "--row-accent": accent,
        } as CSSProperties
      }
    >
      <span
        aria-hidden="true"
        data-row-accent=""
        className={cn(
          "pointer-events-none absolute inset-y-0 left-0 z-1 w-1.5 transition-colors duration-200 ease-out",
          "bg-[color-mix(in_oklch,var(--row-accent)_22%,transparent)]",
          "group-hover/row:bg-(--row-accent) group-focus-within/row:bg-(--row-accent)",
          active && "bg-(--row-accent)",
          tablet && "hidden",
        )}
      />
      <Cell
        selected={selected}
        pinned={0}
        className={tablet ? undefined : "pr-0 pl-3.5"}
      >
        <TableCheckbox
          checked={selected}
          label={fillTableCopy(copy.row.select, { key: keyLabel })}
          onChange={onSelectedChange}
        />
      </Cell>
      {columns.map(render)}
    </div>
  );
  if (!actions) return rowElement;
  return (
    <ContextMenu>
      <ContextMenuTrigger asChild>{rowElement}</ContextMenuTrigger>
      <ContextMenuContent className="w-52">
        <ContextMenuItem onSelect={onOpen}>
          {copy.row.menu.open}
        </ContextMenuItem>
        {actions.onDuplicate ? (
          <ContextMenuItem onSelect={actions.onDuplicate}>
            {copy.row.menu.duplicate}
          </ContextMenuItem>
        ) : null}
        <ContextMenuItem onSelect={actions.onCopyLink}>
          {copy.row.menu.copyLink}
        </ContextMenuItem>
        {actions.onMoveToBoard ? (
          <ContextMenuItem onSelect={actions.onMoveToBoard}>
            {copy.row.menu.moveToBoard}
          </ContextMenuItem>
        ) : null}
        {actions.onArchive ? (
          <ContextMenuItem onSelect={actions.onArchive}>
            {copy.row.menu.archive}
          </ContextMenuItem>
        ) : null}
        {actions.onDelete ? (
          <ContextMenuItem variant="destructive" onSelect={actions.onDelete}>
            {copy.row.menu.delete}
          </ContextMenuItem>
        ) : null}
      </ContextMenuContent>
    </ContextMenu>
  );
}
