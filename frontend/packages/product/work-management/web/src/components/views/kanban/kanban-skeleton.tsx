import { Skeleton } from "@notrelix/ui-web"

export function KanbanSkeleton() {
  return (
    <div className="flex h-full flex-col gap-4 overflow-hidden bg-card p-4 sm:p-6" aria-busy="true" aria-label="Loading Kanban board">
      {/* Toolbar skeleton */}
      <div className="flex min-h-14 shrink-0 flex-wrap items-center gap-2 border-b border-border pb-4">
        <Skeleton className="h-9 w-64 rounded-full" />
        <Skeleton className="h-9 w-24 rounded-full" />
        <Skeleton className="h-9 w-24 rounded-full" />
      </div>

      {/* Board columns skeleton */}
      <div className="flex flex-1 gap-4 overflow-x-auto pb-4">
        {Array.from({ length: 4 }).map((_, colIndex) => (
          <div
            key={colIndex}
            className="flex w-[290px] shrink-0 flex-col gap-3 rounded-2xl border border-border bg-muted/20 p-3"
          >
            {/* Header skeleton */}
            <div className="flex items-center justify-between border-b border-border pb-2">
              <Skeleton className="h-5 w-32 rounded-lg" />
              <Skeleton className="size-6 rounded-full" />
            </div>

            {/* Cards skeleton */}
            <div className="flex flex-1 flex-col gap-3">
              {Array.from({ length: colIndex % 2 === 0 ? 3 : 2 }).map((_, cardIndex) => (
                <div key={cardIndex} className="rounded-xl border border-border bg-card p-3 shadow-xs space-y-3">
                  <Skeleton className="h-4 w-5/6 rounded-md" />
                  <Skeleton className="h-3 w-1/2 rounded-md" />
                  <div className="flex gap-2">
                    <Skeleton className="h-5 w-12 rounded-full" />
                    <Skeleton className="h-5 w-12 rounded-full" />
                  </div>
                  <div className="flex items-center justify-between pt-1">
                    <Skeleton className="h-4 w-16 rounded-md" />
                    <Skeleton className="size-6 rounded-full" />
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
