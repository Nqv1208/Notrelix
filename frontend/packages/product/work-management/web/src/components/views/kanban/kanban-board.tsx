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
import type { Board, BoardGroup } from "@notrelix/work-management-core"
import { generatePosition } from "@notrelix/work-management-core"
import { KanbanAddColumn } from "./kanban-add-column"
import { KanbanColumn } from "./kanban-column"
export function KanbanBoard({
  board,
  columns,
  workspaceId,
  onOpenDetails,
  onMoveCard,
  onReorderColumns,
  onAdd,
  onRenameColumn,
  onColorChangeColumn,
  onDeleteColumn,
  onDuplicateCard,
  onDeleteCard,
}: {
  board: Board
  columns: BoardGroup[]
  workspaceId: string
  onOpenDetails: (cardId: string) => void
  onMoveCard: (cardId: string, listId: string, position: number) => void
  onReorderColumns: (updated: BoardGroup[]) => void
  onAdd: (title: string) => void
  onRenameColumn: (listId: string, title: string) => void
  onColorChangeColumn: (listId: string, color: string) => void
  onDeleteColumn: (listId: string) => void
  onDuplicateCard: (cardId: string) => void
  onDeleteCard: (cardId: string) => void
}) {
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  )

  function handleDragEnd(event: DragEndEvent) {
    if (!event.over) return
    const activeType = event.active.data.current?.type
    const overType = event.over.data.current?.type

    // Card Drag End
    if (activeType === "kanban-card") {
      const activeCard = event.active.data.current?.card
      const targetGroupId =
        overType === "kanban-card"
          ? event.over.data.current?.card?.listId
          : overType === "kanban-column"
            ? String(event.over.id)
            : undefined

      if (!activeCard || !targetGroupId) return
      const targetGroup = columns.find((group) => group.id === targetGroupId)
      if (!targetGroup) return

      const overCard = overType === "kanban-card" ? event.over.data.current?.card : undefined
      const orderedCards = targetGroup.cards.filter((card) => card.id !== activeCard.id).sort((a, b) => a.position - b.position)
      const overIndex = overCard ? orderedCards.findIndex((card) => card.id === overCard.id) : orderedCards.length
      const before = orderedCards[overIndex - 1]?.position
      const after = orderedCards[overIndex]?.position

      onMoveCard(activeCard.id, targetGroupId, generatePosition(before, after))
    }
  }

  if (columns.length === 0) {
    return <KanbanAddColumn onAdd={() => onAdd("New column")} />
  }

  return (
    <DndContext sensors={sensors} collisionDetection={closestCorners} onDragEnd={handleDragEnd}>
      <div className="flex flex-1 gap-4 overflow-x-auto pb-4 select-none">
        {columns.map((group) => (
          <KanbanColumn
            key={group.id}
            board={board}
            group={group}
            workspaceId={workspaceId}
            onOpenDetails={onOpenDetails}
            onRename={(title: any) => onRenameColumn(group.id, title)}
            onColorChange={(color: any) => onColorChangeColumn(group.id, color)}
            onDelete={() => onDeleteColumn(group.id)}
            onDuplicateCard={onDuplicateCard}
            onDeleteCard={onDeleteCard}
          />
        ))}

        <div className="w-[290px] shrink-0">
          <KanbanAddColumn
            onAdd={onAdd}
          />
        </div>
      </div>
    </DndContext>
  )
}
