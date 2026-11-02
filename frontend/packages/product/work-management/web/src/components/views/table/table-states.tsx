import { Filter } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import type { MainTableCopy } from "./main-table.copy";
import { fillTableCopy } from "./main-table.copy";
import { useTableLayout } from "./table-layout";

/** The line under the table: counts, "Hiện việc đã xong" and the key hints. */
export function TableFooter({
  copy,
  shown,
  hidden,
  archived = 0,
  canShowDone,
  onShowDone,
}: {
  copy: MainTableCopy;
  shown: number;
  hidden: number;
  /** Items archived a moment ago and still waiting for "Hoàn tác". */
  archived?: number;
  /** Finished items are hidden: offer to show them. */
  canShowDone: boolean;
  onShowDone: () => void;
}) {
  const f = copy.footer;
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

/** The board has no group to show yet. */
export function TableEmpty({ copy }: { copy: MainTableCopy }) {
  return (
    <TableNote>
      <b className="text-[0.9375rem] font-semibold text-foreground">
        {copy.empty.title}
      </b>
      <span>{copy.empty.description}</span>
    </TableNote>
  );
}
