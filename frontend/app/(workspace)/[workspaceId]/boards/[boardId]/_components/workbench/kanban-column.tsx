"use client"

import { useState } from "react"
import { useDroppable } from "@dnd-kit/core"
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable"
import { MoreHorizontal, Plus } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { ScrollArea } from "@/components/ui/scroll-area"
import { cn } from "@/lib/utils"
import type { Board, BoardGroup } from "@/features/boards/types"
import { KanbanAddCard } from "./kanban-add-card"
import { KanbanCard } from "./kanban-card"

export function KanbanColumn({
  board,
  group,
  workspaceId,
}: {
  board: Board
  group: BoardGroup
  workspaceId: string
}) {
  const [isAdding, setIsAdding] = useState(false)
  const { setNodeRef, isOver } = useDroppable({
    id: group.id,
    data: { type: "kanban-column", group },
  })

  return (
    <section
      ref={setNodeRef}
      className={cn(
        "flex w-[320px] shrink-0 flex-col overflow-hidden rounded-2xl border border-border bg-muted/55 transition",
        isOver && "border-primary bg-accent/45"
      )}
      aria-label={`${group.title} kanban column`}
    >
      <div className="flex shrink-0 items-center justify-between gap-3 border-b border-border bg-card/70 px-3 py-3">
        <div className="flex min-w-0 items-center gap-2">
          <span className="size-2.5 rounded-full" style={{ backgroundColor: group.color ?? "var(--primary)" }} />
          <h2 className="truncate text-sm font-semibold text-foreground">{group.title}</h2>
          <Badge variant="secondary" className="rounded-full">{group.cards.length}</Badge>
        </div>
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={`Add card to ${group.title}`}
            onClick={() => setIsAdding(true)}
          >
            <Plus className="size-4" />
          </Button>
          <Button variant="ghost" size="icon-sm" aria-label={`${group.title} actions`}>
            <MoreHorizontal className="size-4" />
          </Button>
        </div>
      </div>

      <ScrollArea className="min-h-0 flex-1">
        <SortableContext items={group.cards.map((card) => card.id)} strategy={verticalListSortingStrategy}>
          <div className="space-y-3 p-3">
            {group.cards.map((card) => (
              <KanbanCard key={card.id} board={board} card={card} />
            ))}
            <KanbanAddCard
              boardId={board.id}
              workspaceId={workspaceId}
              group={group}
              isAdding={isAdding}
              onToggleAdding={setIsAdding}
            />
          </div>
        </SortableContext>
      </ScrollArea>
    </section>
  )
}
