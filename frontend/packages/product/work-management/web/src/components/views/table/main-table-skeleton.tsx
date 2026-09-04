import { Skeleton } from "@notrelix/ui-web";

export function MainTableSkeleton() {
  return (
    <div className="h-full min-h-0 bg-card p-4" data-slot="main-table-loading">
      <div className="border border-border bg-card p-4">
        <Skeleton className="mb-4 h-10 rounded-lg" />
        <div className="flex flex-col gap-2">
          {Array.from({ length: 8 }).map((_, index) => (
            <Skeleton key={index} className="h-12 rounded-lg" />
          ))}
        </div>
      </div>
    </div>
  );
}
