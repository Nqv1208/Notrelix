import { Progress } from "@/components/ui/progress"
import type { Card } from "@/features/boards/types"

export function CellProgress({ card }: { card: Card }) {
  const total = card.checklists.reduce((count, checklist) => count + checklist.items.length, 0)
  const done = card.checklists.reduce((count, checklist) => count + checklist.items.filter((item) => item.isDone).length, 0)
  const value = total === 0 ? 0 : Math.round((done / total) * 100)

  return (
    <div className="flex min-w-0 items-center gap-2">
      <Progress value={value} className="h-2" />
      <span className="w-9 text-xs text-muted-foreground">{value}%</span>
    </div>
  )
}
