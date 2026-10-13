import { Skeleton } from "@notrelix/ui-web";
import { mainTableCopy, type MainTableCopy } from "./main-table.copy";

/**
 * The table while it loads: the toolbar and the grid header, then rows with
 * the mockup's proportions. Announced once as busy.
 */
export function MainTableSkeleton({
  copy = mainTableCopy("vi"),
}: {
  copy?: MainTableCopy;
}) {
  return (
    <div
      role="status"
      aria-busy="true"
      aria-label={copy.loading}
      data-slot="main-table-loading"
      className="flex h-full min-h-0 flex-col bg-background"
    >
      <div className="flex items-center gap-1.5 px-4 py-2.5 sm:px-6">
        <Skeleton className="h-8 w-50 rounded-[7px]" />
        <Skeleton className="h-8 w-16.5 rounded-[7px]" />
        <Skeleton className="h-8 w-35 rounded-[7px]" />
        <Skeleton className="h-8 w-40 rounded-[7px]" />
        <span className="flex-1" />
        <Skeleton className="h-8 w-37.5 rounded-[7px]" />
      </div>
      <div className="mx-4 min-h-0 flex-1 overflow-hidden rounded-[12px] border border-border bg-popover sm:mx-6 dark:bg-card">
        <div className="h-9 border-b border-line-strong bg-table-group" />
        {Array.from({ length: 8 }).map((_, index) => (
          <div
            key={index}
            className="flex h-10 items-center gap-4 border-b border-border px-3.5"
          >
            <Skeleton className="size-3.75 rounded-[4px]" />
            <Skeleton className="h-3 w-14" />
            <Skeleton className="h-3.5 w-64" />
            <Skeleton className="h-6 w-20 rounded-full" />
            <Skeleton className="h-3.5 w-32" />
          </div>
        ))}
      </div>
    </div>
  );
}
