import { useEffect, useRef, useState } from "react";
import { ChevronDown, Plus, TriangleAlert } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import { ITEM_TITLE_MAX_LENGTH } from "@notrelix/work-management-core";
import {
  fillTableCopy,
  pluralTableCopy,
  type MainTableCopy,
} from "./main-table.copy";
import { pageButtonClass, tablePanelClass } from "./table-chrome";
import { TableGroupMenu, type TableGroupMenuModel } from "./table-group-menu";
import { formatTableCount, type TableGroupModel } from "./table-model";
import { useTableLayout } from "./table-layout";
import { StatusDot } from "./table-parts";

/** The group header: toggle, status dot, name, count and the overdue note. */
export function TableGroupHeader({
  copy,
  group,
  collapsed,
  onToggle,
  menu,
  boardNote,
  onAdd,
}: {
  copy: MainTableCopy;
  group: TableGroupModel;
  collapsed: boolean;
  onToggle: () => void;
  /** The tablet header's "+": opens the group's add field; absent when the viewer cannot add. */
  onAdd?: () => void;
  /** Move and delete of the group's option; absent = the viewer cannot, or the group is not an option. */
  menu?: TableGroupMenuModel;
  /** What the first group says of the whole board ("Board có 5.000 việc"); absent on the others. */
  boardNote?: string;
}) {
  const tablet = useTableLayout() === "tablet";
  // A group read in pages counts the rows that pass the filter, loaded or not; a filtered one says of how many.
  const count =
    group.filteredTotal === undefined
      ? String(group.rows.length)
      : group.filteredTotal < group.total
        ? fillTableCopy(copy.group.filteredOf, {
            count: formatTableCount(group.filteredTotal),
            total: formatTableCount(group.total),
          })
        : formatTableCount(group.filteredTotal);
  const overdue =
    group.overdueCount > 0
      ? pluralTableCopy(
          { one: copy.group.overdue_one, other: copy.group.overdue_other },
          group.overdueCount,
        )
      : null;
  if (tablet)
    // `Final-T-Table`: one 44 px toggle holds the chevron, dot, name and count; "+" adds to the group.
    return (
      <div
        role="row"
        className="flex h-11 items-center border-b border-line-strong bg-popover dark:bg-card"
      >
        <span
          role="cell"
          className="sticky left-0 inline-flex h-10.75 items-center gap-0.5 pl-0.5"
        >
          <button
            type="button"
            aria-expanded={!collapsed}
            aria-label={fillTableCopy(
              collapsed ? copy.group.expand : copy.group.collapse,
              { name: group.name },
            )}
            onClick={onToggle}
            className="inline-flex h-10.75 cursor-pointer items-center gap-2 rounded-[10px] border-0 bg-transparent pr-3 pl-2.5 text-[15px] font-semibold text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
          >
            <ChevronDown
              className={cn(
                "size-4 text-meta-foreground",
                collapsed && "-rotate-90",
              )}
              aria-hidden="true"
            />
            <StatusDot category={group.category} />
            {group.name}
            <span className="inline-grid h-6 min-w-6 place-items-center rounded-full bg-state-hover-strong px-1.75 text-crumb font-semibold text-muted-foreground">
              {count}
            </span>
            {overdue ? (
              <span className="text-compact font-semibold text-danger-text">
                · {overdue}
              </span>
            ) : null}
          </button>
          {onAdd ? (
            <button
              type="button"
              aria-label={fillTableCopy(copy.group.addTo, { name: group.name })}
              onClick={onAdd}
              className="grid size-11 cursor-pointer place-items-center rounded-[10px] border-0 bg-transparent p-0 text-meta-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
            >
              <Plus className="size-4" aria-hidden="true" />
            </button>
          ) : null}
          {menu ? (
            <TableGroupMenu copy={copy} name={group.name} menu={menu} />
          ) : null}
        </span>
        {boardNote ? (
          <span
            role="cell"
            className="ml-auto pr-3 text-compact font-medium text-meta-foreground"
          >
            {boardNote}
          </span>
        ) : null}
      </div>
    );
  return (
    <div
      role="row"
      className="flex h-9.75 items-center border-b border-line-strong bg-table-group"
    >
      <span
        role="cell"
        className="inline-flex items-center gap-2 pl-2.5 text-nav font-semibold text-foreground"
      >
        <button
          type="button"
          aria-expanded={!collapsed}
          aria-label={fillTableCopy(
            collapsed ? copy.group.expand : copy.group.collapse,
            { name: group.name },
          )}
          onClick={onToggle}
          className="grid size-6 cursor-pointer place-items-center rounded-[5px] border-0 bg-transparent p-0 text-meta-foreground focus-visible:outline-2 focus-visible:outline-ring"
        >
          <ChevronDown
            className={cn("size-3.5", collapsed && "-rotate-90")}
            aria-hidden="true"
          />
        </button>
        <StatusDot category={group.category} />
        {group.name}
        <span className="font-medium text-meta-foreground">{count}</span>
        {group.rows.length === 0 && !group.hasMore ? (
          <span
            data-slot="table-group-empty"
            className="text-[13px] leading-[1.45] font-normal text-meta-foreground"
          >
            {group.total > 0 ? copy.group.emptyFiltered : copy.group.emptyGroup}
          </span>
        ) : null}
        {group.overdueCount > 0 ? (
          <span className="text-crumb font-semibold text-danger-text">
            ·{" "}
            {pluralTableCopy(
              {
                one: copy.group.overdue_one,
                other: copy.group.overdue_other,
              },
              group.overdueCount,
            )}
          </span>
        ) : null}
        {menu ? (
          <TableGroupMenu copy={copy} name={group.name} menu={menu} />
        ) : null}
      </span>
      {boardNote ? (
        <span
          role="cell"
          className="ml-auto pr-3 text-[13px] font-medium text-meta-foreground"
        >
          {boardNote}
        </span>
      ) : null}
    </div>
  );
}

/** How many rows a group shows before "Show more", and how many each press adds. */
export const TABLE_GROUP_WINDOW = 50;

/**
 * The foot of a group that shows only its first rows: how many are shown of the group's total, and the
 * button that adds the next page. The rows stay in memory; only the ones shown are drawn, so a group of
 * thousands keeps the page light.
 */
export function TableShowMoreRow({
  copy,
  shown,
  total,
  loading = false,
  onShowMore,
}: {
  copy: MainTableCopy;
  shown: number;
  total: number;
  /** The next page of the group is being read from the server. */
  loading?: boolean;
  onShowMore: () => void;
}) {
  const next = Math.min(TABLE_GROUP_WINDOW, total - shown);
  return (
    <div
      role="row"
      data-slot="table-show-more"
      className="flex flex-wrap items-center justify-center gap-3 px-6 py-2.5 text-crumb text-muted-foreground"
    >
      <span role="cell">
        {fillTableCopy(copy.group.shownOf, { shown, total })}
      </span>
      <span role="cell">
        <Button
          type="button"
          variant="outline"
          size="sm"
          className={pageButtonClass}
          disabled={loading}
          aria-busy={loading || undefined}
          onClick={onShowMore}
        >
          {loading
            ? copy.group.loadingMore
            : pluralTableCopy(
                {
                  one: copy.group.showMore_one,
                  other: copy.group.showMore_other,
                },
                next,
              )}
        </Button>
      </span>
    </div>
  );
}

/**
 * The next page of a group did not come: the rows already shown stay, and the row says so where the page would
 * have started, with a retry.
 */
export function TableLoadMoreError({
  copy,
  next,
  onRetry,
}: {
  copy: MainTableCopy;
  next: number;
  onRetry: () => void;
}) {
  return (
    <div role="row" data-slot="table-load-more-error" className="px-6 py-2.5">
      <div role="cell">
        <div
          role="alert"
          className="flex items-start gap-2.5 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-[13px] leading-[1.4] text-danger-text"
        >
          <TriangleAlert
            aria-hidden="true"
            className="mt-0.5 size-4 shrink-0"
          />
          <span className="flex min-w-0 flex-1 flex-col gap-px">
            <b className="font-semibold text-foreground">
              {copy.group.loadMoreFailed}
            </b>
            <span>
              {fillTableCopy(copy.group.loadMoreFailedBody, {
                count: formatTableCount(next),
              })}
            </span>
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className={pageButtonClass}
            onClick={onRetry}
          >
            {copy.group.loadMoreRetry}
          </Button>
        </div>
      </div>
    </div>
  );
}

/**
 * "Thêm vào {group}": a row under the group that turns into a title field.
 * Enter adds the item and keeps the field for the next one; Escape or
 * leaving an empty field closes it.
 */
export function TableAddRow({
  copy,
  group,
  openRequest = 0,
  onCreate,
}: {
  copy: MainTableCopy;
  group: TableGroupModel;
  /** Changing this number opens the field (the toolbar's "Thêm công việc"). */
  openRequest?: number;
  onCreate: (title: string) => void;
}) {
  const [open, setOpen] = useState(false);
  useEffect(() => {
    if (openRequest > 0) setOpen(true);
  }, [openRequest]);
  const [title, setTitle] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  const submit = () => {
    const trimmed = title.trim();
    if (!trimmed) return;
    onCreate(trimmed);
    setTitle("");
  };
  // The tablet drawing has no "Thêm vào" row: the header's "+" opens the field.
  const tablet = useTableLayout() === "tablet";
  if (tablet && !open) return null;

  return (
    <div
      role="row"
      className={cn(
        "flex h-8.75 items-center border-b border-border text-compact text-meta-foreground",
        tablePanelClass,
      )}
    >
      <span
        role="cell"
        className="inline-flex items-center gap-1.5 pl-13.5 leading-[1.45]"
      >
        {open ? (
          <form
            className="inline-flex items-center gap-1.5"
            onSubmit={(event) => {
              event.preventDefault();
              submit();
            }}
          >
            <Plus className="size-3.5" aria-hidden="true" />
            <input
              ref={inputRef}
              value={title}
              maxLength={ITEM_TITLE_MAX_LENGTH}
              onChange={(event) => setTitle(event.target.value)}
              aria-label={fillTableCopy(copy.group.newItemLabel, {
                name: group.name,
              })}
              placeholder={copy.group.newItemPlaceholder}
              onBlur={() => {
                if (title.trim() === "") setOpen(false);
              }}
              onKeyDown={(event) => {
                if (event.key === "Escape") {
                  event.preventDefault();
                  setTitle("");
                  setOpen(false);
                }
              }}
              className="h-6.5 w-72 rounded-[7px] border border-line-strong bg-popover px-2 text-compact text-foreground outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card"
            />
          </form>
        ) : (
          <button
            type="button"
            onClick={() => setOpen(true)}
            className="inline-flex cursor-pointer items-center gap-1.5 border-0 bg-transparent p-0 text-compact leading-[1.45] text-meta-foreground hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
          >
            <Plus className="size-3.5" aria-hidden="true" />
            {fillTableCopy(copy.group.addTo, { name: group.name })}
          </button>
        )}
      </span>
    </div>
  );
}
