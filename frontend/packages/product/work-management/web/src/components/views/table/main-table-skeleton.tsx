import { Skeleton, cn } from "@notrelix/ui-web";
import { MainTableToolbarPlaceholder } from "./main-table-error";
import { mainTableCopy, type MainTableCopy } from "./main-table.copy";

/** Row 36 px; columns select, key, title, status, due (BC-Shell L). */
const ROW =
  "grid h-9 grid-cols-[40px_84px_minmax(0,1fr)_140px_120px] items-center border-b";
/** The drawn bars grow and shrink from row to row. */
const TITLE_BARS = [226, 300, 374, 249, 323];

/**
 * The table while it loads (BC-Shell L): the toolbar switched off, the grid
 * header, then 36 px rows of 10 px bars. Announced once as busy.
 */
export function MainTableSkeleton({
  copy = mainTableCopy("vi"),
}: {
  copy?: MainTableCopy;
}) {
  const c = copy.columns;
  return (
    <div
      role="status"
      aria-busy="true"
      aria-label={copy.loading}
      data-slot="main-table-loading"
      className="flex h-full min-h-0 flex-col bg-background"
    >
      <MainTableToolbarPlaceholder copy={copy} phoneBar />
      <div
        data-slot="main-table-loading-cards"
        className="flex flex-col gap-2.5 px-3 pt-2 sm:hidden"
      >
        {[161, 161, 161].map((bar, index) => (
          <div
            key={index}
            className="flex h-21 flex-col gap-2 rounded-[10px] border border-border bg-popover p-3 dark:bg-card"
          >
            <Skeleton className="h-2.5" style={{ width: bar }} />
            <Skeleton className="h-3 w-60.25" />
            <span className="flex gap-2">
              <Skeleton className="h-5 w-16 rounded-full" />
              <Skeleton className="h-5 w-12 rounded-full" />
            </span>
          </div>
        ))}
      </div>
      <div className="mx-4 min-h-0 flex-1 overflow-hidden rounded-[12px] border border-border bg-popover max-sm:hidden sm:mx-6 dark:bg-card">
        <div
          className={cn(
            ROW,
            "border-line-strong bg-table-group text-compact font-semibold text-meta-foreground",
          )}
        >
          <span />
          {[c.key, c.title, c.status, c.due].map((label) => (
            <span key={label} className="px-2.5">
              {label}
            </span>
          ))}
        </div>
        {TITLE_BARS.map((title, index) => (
          <div key={index} className={ROW}>
            <span />
            <span className="px-2.5">
              <Skeleton className="h-2.5 w-12.5" />
            </span>
            <span className="px-2.5">
              <Skeleton className="h-2.5" style={{ width: title }} />
            </span>
            <span className="px-2.5">
              <Skeleton className="h-2.5 w-17.5" />
            </span>
            <span className="px-2.5">
              <Skeleton className="h-2.5 w-15" />
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
