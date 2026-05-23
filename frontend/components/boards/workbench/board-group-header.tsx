import { ChevronDown, Plus } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type { BoardGroup } from "@/features/boards/types"

export function BoardGroupHeader({ group }: { group: BoardGroup }) {
  return (
    <div className="flex items-center justify-between border-b border-border bg-card px-4 py-3">
      <div className="flex min-w-0 items-center gap-2">
        <ChevronDown className="size-4 text-muted-foreground" />
        <span className="size-2.5 rounded-full" style={{ backgroundColor: group.color ?? "var(--primary)" }} />
        <h2 className="truncate text-sm font-semibold text-foreground">{group.title}</h2>
        <Badge variant="secondary" className="rounded-full">{group.cards.length}</Badge>
      </div>
      <Button variant="ghost" size="sm" className="h-8 rounded-full">
        <Plus className="size-4" />
        Add task
      </Button>
    </div>
  )
}
