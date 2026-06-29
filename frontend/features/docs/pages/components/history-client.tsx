"use client"

import { useState } from "react"
import { Clock3, RotateCcw } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { usePageHistory } from "../hooks/queries/use-page-history"
import { mockDocsWorkspace } from "../../shared/mock/mock-data"
import { isMockModeEnabled } from "@/lib/config/mock-mode"

export function HistoryClient({ pageId }: { pageId: string }) {
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const { data: history = [], isLoading } = usePageHistory(pageId)
  const users = isMockModeEnabled("docs")
    ? new Map(mockDocsWorkspace.users.map((user) => [user.id, user]))
    : new Map()

  if (isLoading) {
    return (
      <div className="grid gap-5 lg:grid-cols-[340px_minmax(0,1fr)]">
        <Skeleton className="h-96 rounded-2xl" />
        <Skeleton className="h-96 rounded-2xl" />
      </div>
    )
  }

  return (
    <div className="grid gap-5 lg:grid-cols-[340px_minmax(0,1fr)]">
      <aside className="space-y-2 rounded-2xl border border-border bg-card p-3">
        {history.map((item) => {
          const user = users.get(item.actorId)
          return (
            <button
              key={item.id}
              type="button"
              onClick={() => setSelectedId(item.id)}
              className="w-full rounded-xl border border-transparent p-3 text-left transition hover:bg-muted data-[active=true]:border-[#6161ff] data-[active=true]:bg-accent"
              data-active={selectedId === item.id}
            >
              <div className="mb-1 flex items-center gap-2 text-sm font-medium text-foreground">
                <Clock3 className="size-4 text-primary" />
                {item.action} {item.targetLabel}
              </div>
              <p className="text-xs text-muted-foreground">{user?.name ?? "Unknown"} · {new Date(item.createdAt).toLocaleString()}</p>
            </button>
          )
        })}
      </aside>
      <section className="min-h-96 rounded-2xl border border-border bg-card p-5">
        {selectedId ? (
          <div>
            <div className="mb-5 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-foreground">Snapshot preview</h2>
              <Button size="sm" className="rounded-full">
                <RotateCcw className="size-4" />
                Restore
              </Button>
            </div>
            <div className="space-y-3">
              <Skeleton className="h-8 w-2/3" />
              <Skeleton className="h-5 w-full" />
              <Skeleton className="h-5 w-10/12" />
              <Skeleton className="h-32 w-full rounded-xl" />
            </div>
          </div>
        ) : (
          <div className="flex h-80 items-center justify-center text-sm text-muted-foreground">Select a version to preview</div>
        )}
      </section>
    </div>
  )
}
