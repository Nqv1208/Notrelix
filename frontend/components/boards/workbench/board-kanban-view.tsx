"use client"

import {
  closestCorners,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core"
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable"
import type { Board, BoardGroup, Card } from "@/features/boards/types"
import { useMoveCard } from "@/features/boards/hooks"
import { generatePosition } from "@/features/boards/utils/fractional-index"
import { KanbanColumn } from "./kanban-column"

export function BoardKanbanView({ board, groups }: { board: Board; groups: BoardGroup[] }) {
  const moveCard = useMoveCard(board.id)
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  )

  function handleDragEnd(event: DragEndEvent) {
    if (!event.over) return
    const activeType = event.active.data.current?.type
    const overType = event.over.data.current?.type
    if (activeType !== "kanban-card") return

    const activeCard = event.active.data.current?.card as Card | undefined
    const targetGroupId =
      overType === "kanban-card"
        ? (event.over.data.current?.card as Card | undefined)?.listId
        : overType === "kanban-column"
          ? String(event.over.id)
          : undefined

    if (!activeCard || !targetGroupId) return
    const targetGroup = groups.find((group) => group.id === targetGroupId)
    if (!targetGroup) return

    const overCard = overType === "kanban-card" ? (event.over.data.current?.card as Card | undefined) : undefined
    const orderedCards = targetGroup.cards.filter((card) => card.id !== activeCard.id).sort((a, b) => a.position - b.position)
    const overIndex = overCard ? orderedCards.findIndex((card) => card.id === overCard.id) : orderedCards.length
    const before = orderedCards[overIndex - 1]?.position
    const after = orderedCards[overIndex]?.position

    moveCard.mutate({
      cardId: activeCard.id,
      listId: targetGroupId,
      position: generatePosition(before, after),
    })
  }

  return (
    <section className="rounded-2xl border border-border bg-card p-4 shadow-sm" aria-label={`${board.title} kanban view`}>
      <DndContext sensors={sensors} collisionDetection={closestCorners} onDragEnd={handleDragEnd}>
        <div className="flex min-h-[620px] gap-4 overflow-x-auto pb-2">
          {groups.map((group) => (
            <KanbanColumn key={group.id} board={board} group={group} />
          ))}
        </div>
      </DndContext>
    </section>
  )
}
