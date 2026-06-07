"use client"

import { useMemo } from "react"
import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core"
import { arrayMove, sortableKeyboardCoordinates } from "@dnd-kit/sortable"
import type { Board, BoardGroup, Card, FieldDefinition } from "@/features/boards/types"
import { useBoardView, useMoveCard } from "@/features/boards/hooks"
import { generatePosition } from "@/features/boards/utils/fractional-index"
import { BoardGroupSection } from "./board-group-section"
import { TableHeaderRow } from "./table-header-row"

export function BoardTableView({
  board,
  groups,
  fieldDefinitions,
}: {
  board: Board
  groups: BoardGroup[]
  fieldDefinitions: FieldDefinition[]
}) {
  const { viewConfig, updateViewConfig } = useBoardView(board.id)
  const moveCard = useMoveCard(board.id)
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  )

  const visibleFields = useMemo(() => {
    const fields = fieldDefinitions
      .filter((field) => !field.isHidden && !viewConfig.hiddenFields.includes(field.id))
      .sort((a, b) => a.position - b.position)
    if (viewConfig.columnOrder.length === 0) return fields
    return [...fields].sort((a, b) => {
      const aIndex = viewConfig.columnOrder.indexOf(a.id)
      const bIndex = viewConfig.columnOrder.indexOf(b.id)
      return (aIndex === -1 ? Number.MAX_SAFE_INTEGER : aIndex) - (bIndex === -1 ? Number.MAX_SAFE_INTEGER : bIndex)
    })
  }, [fieldDefinitions, viewConfig.columnOrder, viewConfig.hiddenFields])

  function handleDragEnd(event: DragEndEvent) {
    const activeType = event.active.data.current?.type
    const overType = event.over?.data.current?.type
    if (!event.over) return

    if (activeType === "column" && overType === "column") {
      const oldIndex = visibleFields.findIndex((field) => field.id === event.active.id)
      const newIndex = visibleFields.findIndex((field) => field.id === event.over?.id)
      if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return
      updateViewConfig({ columnOrder: arrayMove(visibleFields, oldIndex, newIndex).map((field) => field.id) })
      return
    }

    if (activeType === "card") {
      const activeCard = event.active.data.current?.card as Card | undefined
      const targetGroupId =
        overType === "card"
          ? (event.over.data.current?.card as Card | undefined)?.listId
          : overType === "group"
            ? String(event.over.id)
            : undefined
      if (!activeCard || !targetGroupId) return
      const targetGroup = groups.find((group) => group.id === targetGroupId)
      if (!targetGroup) return
      const overCard = overType === "card" ? (event.over.data.current?.card as Card | undefined) : undefined
      const orderedCards = targetGroup.cards.filter((card) => card.id !== activeCard.id).sort((a, b) => a.position - b.position)
      const overIndex = overCard ? orderedCards.findIndex((card) => card.id === overCard.id) : orderedCards.length
      const before = orderedCards[overIndex - 1]?.position
      const after = orderedCards[overIndex]?.position
      moveCard.mutate({ cardId: activeCard.id, listId: targetGroupId, position: generatePosition(before, after) })
    }
  }

  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-sm" aria-label={`${board.title} main table`}>
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <div role="grid" aria-rowcount={groups.reduce((count, group) => count + group.cards.length, 0)} aria-colcount={visibleFields.length}>
          <TableHeaderRow fields={visibleFields} />
          {groups.map((group) => (
            <BoardGroupSection key={group.id} boardId={board.id} workspaceId={board.workspaceId} group={group} fields={visibleFields} />
          ))}
        </div>
      </DndContext>
    </section>
  )
}
