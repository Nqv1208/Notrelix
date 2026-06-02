"use client"

import { memo } from "react"
import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core"
import { arrayMove, SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy } from "@dnd-kit/sortable"
import { Button } from "@/components/ui/button"
import { useCreateBlock } from "@/features/docs/hooks/use-create-block"
import { useReorderBlocks } from "@/features/docs/hooks/use-reorder-blocks"
import type { Block } from "@/features/docs/types"
import { EditableBlock } from "./editable-block"

interface DocBlockRendererProps {
  blocks: Block[]
  pageId: string
}

export const DocBlockRenderer = memo(function DocBlockRenderer({ blocks, pageId }: DocBlockRendererProps) {
  const createBlock = useCreateBlock(pageId)
  const reorderBlocks = useReorderBlocks(pageId)
  const orderedBlocks = [...blocks].sort((a, b) => a.position - b.position)
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  )

  function handleDragEnd(event: DragEndEvent) {
    if (!event.over || event.active.id === event.over.id) return
    const oldIndex = orderedBlocks.findIndex((block) => block.id === event.active.id)
    const newIndex = orderedBlocks.findIndex((block) => block.id === event.over?.id)
    if (oldIndex === -1 || newIndex === -1) return
    const next = arrayMove(orderedBlocks, oldIndex, newIndex)
    reorderBlocks.mutate({ pageId, orderedBlockIds: next.map((block) => block.id) })
  }

  if (!orderedBlocks.length) {
    return (
      <div className="rounded-2xl border border-dashed border-border p-10 text-center">
        <p className="text-sm font-medium text-foreground">Start writing</p>
        <p className="mt-1 text-sm text-muted-foreground">Add the first block or use slash commands.</p>
        <Button className="mt-4 rounded-full" onClick={() => createBlock.mutate({ type: "paragraph", properties: { text: "" } })}>
          Add block
        </Button>
      </div>
    )
  }

  return (
    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
      <SortableContext items={orderedBlocks.map((block) => block.id)} strategy={verticalListSortingStrategy}>
        <section aria-label="Page blocks" className="space-y-1">
          {orderedBlocks.map((block) => (
            <EditableBlock key={block.id} block={block} pageId={pageId} />
          ))}
        </section>
      </SortableContext>
    </DndContext>
  )
})
