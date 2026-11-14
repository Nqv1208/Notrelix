import { Filter, Plus, SquareCheckBig, TriangleAlert } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import type { MainTableCopy } from "./main-table.copy";
import { fillTableCopy } from "./main-table.copy";
import { useTableLayout } from "./table-layout";
import { AddGroupPopover } from "../../status-option/add-group-popover";
import {
  BOARD_BAND_BUTTON,
  BoardBand,
  BoardStateBlock,
} from "../../board-layout/board-state-block";
import { kanbanPrimaryButtonClass } from "../kanban/kanban-chrome";
import type { TableAddGroupProps } from "./table-add-group";

/** The line under the table: counts, "Hiện việc đã xong" and the key hints. */
export function TableFooter({
  copy,
  shown,
  hidden,
  archived = 0,
  canShowDone,
  onShowDone,
  tablet = false,
}: {
  copy: MainTableCopy;
  shown: number;
  hidden: number;
  /** Items archived a moment ago and still waiting for "Hoàn tác". */
  archived?: number;
  /** Finished items are hidden: offer to show them. */
  canShowDone: boolean;
  onShowDone: () => void;
  /** The 56 px footer inside the tablet box (`Final-T-Table`). */
  tablet?: boolean;
}) {
  const f = copy.footer;
  if (tablet) {
    return (
      <div
        data-slot="tablet-table-footer"
        className="flex h-14 flex-none items-center gap-1.5 border-t border-line pr-1.5 pl-4 text-[14px] text-muted-foreground"
      >
        {hidden > 0 ? (
          <span>
            {fillTableCopy(canShowDone ? f.tabletHiddenDone : f.tabletHidden, {
              hidden,
            })}
            {archived > 0
              ? ` · ${fillTableCopy(f.archived, { count: archived })}`
              : ""}
          </span>
        ) : null}
        {canShowDone ? (
          <button
            type="button"
            onClick={onShowDone}
            className="h-11 cursor-pointer rounded-[10px] border-0 bg-transparent px-3 text-[14px] font-semibold text-state-selected-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {f.showDone}
          </button>
        ) : null}
        <span className="flex-1" />
        <span className="pr-2.5 text-[13.5px] whitespace-nowrap text-meta-foreground">
          {fillTableCopy(f.tabletShown, { shown })}
        </span>
      </div>
    );
  }
  return (
    <div className="flex flex-wrap items-center gap-4 pt-2.5 pb-3.5 text-crumb text-meta-foreground">
      <span>
        {fillTableCopy(f.summary, { shown, hidden })}
        {archived > 0
          ? ` · ${fillTableCopy(f.archived, { count: archived })}`
          : ""}
      </span>
      {canShowDone ? (
        <button
          type="button"
          onClick={onShowDone}
          className="cursor-pointer border-0 bg-transparent p-0 text-crumb font-semibold text-state-selected-foreground hover:underline focus-visible:outline-2 focus-visible:outline-ring"
        >
          {f.showDone}
        </button>
      ) : null}
      <span className="flex-1" />
    </div>
  );
}

/** One note in the middle of the grid, as a row so the table keeps valid children. */
function TableNote({ children }: { children: React.ReactNode }) {
  // The phone list is not a table: the note stands alone there.
  const layout = useTableLayout();
  const phone = layout === "phone";
  const tablet = layout === "tablet";
  const note = (
    <div
      role="status"
      className="box-border flex flex-col items-center gap-2 px-6 py-14 text-center text-nav leading-[1.45] font-normal text-muted-foreground"
    >
      {children}
    </div>
  );
  if (phone) return note;
  return (
    <div
      role="row"
      className={cn(
        "box-border w-[min(100%,1100px)]",
        // The tablet grid scrolls sideways under a viewport narrower than the note: it stays in view.
        tablet && "sticky left-0 w-[100cqw]",
      )}
    >
      <div role="cell">{note}</div>
    </div>
  );
}

/** Nothing matches the filter: a centred note and "Bỏ lọc". */
export function TableNoResults({
  copy,
  assigneeName,
  hidesDone,
  onClear,
}: {
  copy: MainTableCopy;
  /** The one person the filter narrows to; the note names them. */
  assigneeName?: string;
  /** Finished items are hidden too, so the note says "unfinished". */
  hidesDone?: boolean;
  onClear: () => void;
}) {
  const n = copy.noResults;
  const tablet = useTableLayout() !== "desktop";
  const description = assigneeName
    ? fillTableCopy(hidesDone ? n.assigneeOpen : n.assignee, {
        name: assigneeName,
      })
    : n.description;
  return (
    <TableNote>
      {tablet ? (
        <span
          aria-hidden="true"
          className="mb-1.5 grid size-14 place-items-center rounded-[16px] bg-state-hover text-meta-foreground"
        >
          <Filter className="size-6" />
        </span>
      ) : null}
      <b
        className={cn(
          "text-[0.9375rem] font-semibold text-foreground",
          tablet && "text-[17px] font-[650]",
        )}
      >
        {n.title}
      </b>
      <span>{description}</span>
      <Button
        variant="outline"
        size="sm"
        className={cn(
          "h-8 gap-1.5 rounded-[7px] border-line-strong bg-popover px-3 text-compact font-medium text-foreground shadow-none hover:bg-state-hover-strong dark:bg-card",
          tablet && "mt-1 h-11 rounded-[10px] text-[14.5px]",
        )}
        onClick={onClear}
      >
        {n.clear}
      </Button>
    </TableNote>
  );
}

/** The board has no group to put an item in: a band above the grid with "Thêm nhóm" (BC-Shell NG). */
export function TableNoGroup({
  copy,
  addGroup,
}: {
  copy: MainTableCopy;
  addGroup?: Omit<TableAddGroupProps, "copy">;
}) {
  const t = copy.noGroup;
  return (
    <div className="px-4 pb-3 sm:px-6" data-slot="table-no-group">
      <BoardBand
        tone="warning"
        icon={<TriangleAlert />}
        title={t.title}
        description={t.description}
        action={
          addGroup?.boardGroup ? (
            <AddGroupPopover
              copy={addGroup.formCopy}
              label={t.add}
              existingTitles={addGroup.boardGroup.existingTitles}
              pending={addGroup.boardGroup.pending}
              onSubmit={addGroup.boardGroup.onSubmit}
              triggerClassName={BOARD_BAND_BUTTON}
            />
          ) : undefined
        }
      />
    </div>
  );
}

/** The board holds no item at all: the note and, for someone who may add, "Thêm công việc" (BC-Shell E, Ev). */
export function TableBoardEmpty({
  copy,
  onAdd,
}: {
  copy: MainTableCopy;
  /** Absent for a viewer: the note says they can only look. */
  onAdd?: () => void;
}) {
  const t = copy.empty;
  return (
    <BoardStateBlock
      icon={<SquareCheckBig />}
      title={t.title}
      description={onAdd ? t.description : t.viewer}
      action={
        onAdd ? (
          <Button
            size="sm"
            className={cn(
              kanbanPrimaryButtonClass,
              "max-sm:h-11 max-sm:px-3.5 max-sm:text-[14px]",
            )}
            onClick={onAdd}
          >
            <Plus className="size-4" aria-hidden="true" />
            {t.add}
          </Button>
        ) : undefined
      }
    />
  );
}
