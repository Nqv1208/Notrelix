import { Button } from "@notrelix/ui-web";
import type { MainTableCopy } from "./main-table.copy";
import { fillTableCopy } from "./main-table.copy";

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
  return (
    <div role="row" className="box-border w-[min(100%,1100px)]">
      <div role="cell">
        <div
          role="status"
          className="box-border flex flex-col items-center gap-2 px-6 py-14 text-center text-nav leading-[1.45] font-normal text-muted-foreground"
        >
          {children}
        </div>
      </div>
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
  const description = assigneeName
    ? fillTableCopy(hidesDone ? n.assigneeOpen : n.assignee, {
        name: assigneeName,
      })
    : n.description;
  return (
    <TableNote>
      <b className="text-[0.9375rem] font-semibold text-foreground">
        {n.title}
      </b>
      <span>{description}</span>
      <Button
        variant="outline"
        size="sm"
        className="h-8 gap-1.5 rounded-[7px] border-line-strong bg-popover px-3 text-compact font-medium text-foreground shadow-none hover:bg-state-hover-strong dark:bg-card"
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
