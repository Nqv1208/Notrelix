"use client"

import { Plus, SquareKanban } from "lucide-react"
import { Button } from "@/components/ui/button"

export function KanbanEmptyState({ onCreateColumn }: { onCreateColumn: () => void }) {
  return (
    <div className="flex h-[450px] w-full flex-col items-center justify-center rounded-2xl border border-dashed border-border bg-card p-8 text-center shadow-xs">
      <div className="flex size-14 items-center justify-center rounded-2xl bg-primary/10 text-primary">
        <SquareKanban className="size-7" />
      </div>
      <h3 className="mt-4 text-base font-semibold text-foreground">No columns on this board</h3>
      <p className="mt-2 max-w-sm text-sm text-muted-foreground">
        Create columns (lists) to start organizing tasks, dragging-and-dropping them to track progress.
      </p>
      <Button size="sm" className="mt-6" onClick={onCreateColumn}>
        <Plus className="size-4" />
        Add first column
      </Button>
    </div>
  )
}
