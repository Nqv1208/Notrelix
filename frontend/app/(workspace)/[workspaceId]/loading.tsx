export default function WorkspaceLoading() {
  return (
    <div className="min-h-screen bg-card">
      <div className="mx-auto max-w-[1360px] px-6 lg:px-8 py-6 space-y-8 animate-pulse">
        {/* Header Skeleton */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3.5">
            <div className="size-11 rounded-2xl bg-muted" />
            <div className="space-y-2">
              <div className="h-5 w-36 rounded-lg bg-muted" />
              <div className="h-3 w-28 rounded-md bg-muted" />
            </div>
          </div>
          <div className="flex items-center gap-2">
            <div className="size-9 rounded-xl bg-muted" />
            <div className="size-9 rounded-xl bg-muted" />
            <div className="h-8 w-20 rounded-xl bg-muted hidden sm:block" />
          </div>
        </div>

        {/* Overview Skeleton */}
        <div className="space-y-5">
          <div className="space-y-2">
            <div className="h-5 w-44 rounded-lg bg-muted" />
            <div className="h-3.5 w-64 rounded-md bg-muted" />
          </div>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="rounded-2xl border border-border/50 bg-card p-4 flex items-center gap-3">
                <div className="size-10 rounded-xl bg-muted" />
                <div className="space-y-2">
                  <div className="h-6 w-10 rounded bg-muted" />
                  <div className="h-3 w-16 rounded bg-muted" />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Quick Actions Skeleton */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="rounded-xl border border-border/30 bg-card px-4 py-3 flex items-center gap-2.5">
              <div className="size-8 rounded-lg bg-muted" />
              <div className="h-4 w-16 rounded bg-muted" />
            </div>
          ))}
        </div>

        {/* Content Grid Skeleton */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          <div className="lg:col-span-8 space-y-8">
            {/* Pinned Docs */}
            <div className="space-y-3">
              <div className="h-4 w-36 rounded bg-muted" />
              {[...Array(4)].map((_, i) => (
                <div key={i} className="rounded-xl bg-card px-4 py-3 flex items-center gap-3">
                  <div className="size-6 rounded bg-muted" />
                  <div className="flex-1 space-y-1.5">
                    <div className="h-3.5 w-2/3 rounded bg-muted" />
                    <div className="h-2.5 w-1/3 rounded bg-muted" />
                  </div>
                  <div className="h-5 w-16 rounded-full bg-muted" />
                </div>
              ))}
            </div>
            {/* Active Boards */}
            <div className="space-y-3">
              <div className="h-4 w-28 rounded bg-muted" />
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {[...Array(4)].map((_, i) => (
                  <div key={i} className="rounded-2xl border border-border/50 bg-card p-4 space-y-3">
                    <div className="h-4 w-3/4 rounded bg-muted" />
                    <div className="h-1.5 rounded-full bg-muted" />
                    <div className="flex gap-1">
                      {[...Array(3)].map((_, j) => (
                        <div key={j} className="size-6 rounded-full bg-muted" />
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
          <div className="lg:col-span-4 space-y-6">
            {/* Deadlines */}
            <div className="rounded-2xl border border-border/50 bg-card p-5 space-y-3">
              <div className="h-4 w-24 rounded bg-muted" />
              {[...Array(4)].map((_, i) => (
                <div key={i} className="flex items-center gap-2.5 py-1.5">
                  <div className="size-2 rounded-full bg-muted" />
                  <div className="flex-1 space-y-1">
                    <div className="h-3.5 w-full rounded bg-muted" />
                    <div className="h-2.5 w-1/2 rounded bg-muted" />
                  </div>
                  <div className="size-6 rounded-full bg-muted" />
                </div>
              ))}
            </div>
            {/* Activity */}
            <div className="rounded-2xl border border-border/50 bg-card p-5 space-y-3">
              <div className="h-4 w-20 rounded bg-muted" />
              {[...Array(5)].map((_, i) => (
                <div key={i} className="flex items-center gap-3 py-1.5">
                  <div className="size-7 rounded-full bg-muted" />
                  <div className="flex-1 h-3.5 rounded bg-muted" />
                  <div className="h-2.5 w-10 rounded bg-muted" />
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}