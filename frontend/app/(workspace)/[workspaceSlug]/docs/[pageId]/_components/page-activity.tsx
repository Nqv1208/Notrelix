"use client"

import { Activity } from "lucide-react"
import { ScrollArea } from "@/components/ui/scroll-area"
import { usePageHistory } from "@/features/docs/hooks/use-page-history"
import type { DocsUser } from "@/features/docs/types"

export function PageActivity({ pageId, users }: { pageId: string; users: DocsUser[] }) {
  const { data: activity = [], isLoading } = usePageHistory(pageId)
  const byId = new Map(users.map((user) => [user.id, user]))

  return (
    <ScrollArea className="h-full pr-2">
      {isLoading ? (
        <p className="p-4 text-sm text-muted-foreground">Loading activity...</p>
      ) : (
        <div className="space-y-3">
          {activity.map((item) => {
            const user = byId.get(item.actorId)
            return (
              <div key={item.id} className="flex gap-3 rounded-xl border border-border bg-card p-3">
                <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-accent text-primary">
                  <Activity className="size-4" />
                </div>
                <div className="min-w-0">
                  <p className="text-sm text-foreground">
                    <span className="font-medium">{user?.name ?? "Someone"}</span> {item.action} {item.targetLabel}
                  </p>
                  <p className="mt-1 text-xs text-muted-foreground">{new Date(item.createdAt).toLocaleString()}</p>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </ScrollArea>
  )
}
