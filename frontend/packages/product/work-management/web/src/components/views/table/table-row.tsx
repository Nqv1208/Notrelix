import {
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
} from "react";
import {
  ChevronRight,
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
import { StatusDot, TableCheckbox } from "./table-parts";
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

function Cell({
  className,
  selected,
  children,
}: {
  className?: string;
  selected: boolean;
  children?: React.ReactNode;
}) {
  return (
    <div
      role="cell"
      className={cn(
        "relative box-border flex h-(--table-row-height) min-w-0 items-center gap-[7px] overflow-hidden border-b border-border px-2.5 whitespace-nowrap",
        selected
          ? "bg-row-selected"
          : cn(tablePanelClass, "group-hover/row:bg-state-hover"),
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
  copy,
  columns,
  accent,
  statusOptions,
  selected,
  active,
  tabStop,
  canEdit,
  editors,
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
          <Cell key="title" selected={selected}>
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
                className="grid size-5 shrink-0 cursor-pointer place-items-center rounded-sm border-0 bg-transparent p-0 text-meta-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-ring"
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
                className="min-w-0 truncate text-left text-nav font-medium text-foreground focus-visible:outline-2 focus-visible:outline-ring"
              >
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
              "inline-flex h-6 items-center gap-1.5 rounded-full border-0 px-[9px] text-crumb font-semibold text-foreground",
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
                identityMarkClass(row.assignee.userId),
              )}
            >
              {row.assignee.initials}
            </span>
            <span className="min-w-0 truncate text-nav font-normal">
              {row.assignee.name}
            </span>
          </>
        ) : (
          <>
            <span
              aria-hidden="true"
              className="flex size-6 shrink-0 items-center justify-center rounded-full text-avatar-md font-bold text-meta-foreground shadow-[inset_0_0_0_1.5px_var(--line-strong)]"
            >
              +
            </span>
            <span className="min-w-0 truncate text-nav font-normal">
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
        const due = (
          <span
            data-due={row.due.kind}
            className={cn(
              "text-nav font-normal tabular-nums",
              DUE_CLASS[row.due.kind],
            )}
          >
            {row.due.text}
          </span>
        );
        return (
          <Cell key="due" selected={selected}>
            {canEdit && editors ? (
              <DueEditor
                copy={copy}
                dueAt={row.dueAt}
                label={fillTableCopy(copy.row.changeDue, { key: keyLabel })}
                onPick={(day) => editors.onDue(row.id, day)}
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
              priority
                ? PRIORITY_CLASS[priority]
                : "font-normal text-meta-foreground",
            )}
          >
            {priority ? <PriorityGlyph level={priority} /> : null}
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
                className="flex w-27.5 items-center gap-2 text-metadata leading-[1.45] tabular-nums text-meta-foreground"
              >
                <span
                  aria-hidden="true"
                  className="block h-1.25 flex-1 overflow-hidden rounded-[3px] bg-state-hover-strong"
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
            <span className="text-crumb text-meta-foreground">
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
    }
  };

  const rowElement = (
    <div
      role="row"
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
        )}
      />
      <Cell selected={selected} className="pr-0 pl-3.5">
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
