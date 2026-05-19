"use client"

import { Activity } from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import { useCardActivity } from "@/features/boards/hooks"

export function CardActivity({ cardId }: { cardId: string }) {
  const { data = [], isLoading } = useCardActivity(cardId)

  return (
    <section className="rounded-2xl border border-border bg-card p-5">
      <div className="mb-4 flex items-center gap-2">
        <Activity className="size-4 text-primary" />
        <h2 className="text-sm font-semibold text-foreground">Activity</h2>
      </div>
      {isLoading ? (
        <div className="space-y-2">
          <Skeleton className="h-12 rounded-xl" />
          <Skeleton className="h-12 rounded-xl" />
        </div>
      ) : (
        <div className="space-y-2">
          {data.map((item) => (
            <div key={item.id} className="rounded-xl border border-border bg-muted/40 p-3">
              <p className="text-sm text-foreground"><span className="font-medium">{item.actor}</span> {item.action}</p>
              <p className="mt-1 text-xs text-muted-foreground">{new Date(item.createdAt).toLocaleString()}</p>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}
