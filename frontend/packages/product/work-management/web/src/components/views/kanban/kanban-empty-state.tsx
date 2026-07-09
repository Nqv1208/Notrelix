"use client"

import { Inbox } from "lucide-react"

export function KanbanEmptyState() {
  return (
    <div className="flex flex-col items-center justify-center p-8 text-center">
      <Inbox className="h-12 w-12 text-muted-foreground/50" />
      <p className="mt-2 text-sm text-muted-foreground">No cards yet</p>
    </div>
  )
}
