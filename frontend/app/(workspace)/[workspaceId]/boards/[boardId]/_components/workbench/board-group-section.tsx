import { useDroppable } from "@dnd-kit/core"
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable"
import type { BoardGroup, FieldDefinition } from "@/features/boards/types"
import { BoardGroupHeader } from "./board-group-header"
import { TableAddRow } from "./table-add-row"
import { TableDataRow } from "./table-data-row"

export function BoardGroupSection({
  boardId,
  workspaceId,
  group,
  fields,
}: {
  boardId: string
  workspaceId: string
  group: BoardGroup
  fields: FieldDefinition[]
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: group.id,
    data: { type: "group", group },
  })

  return (
    <section ref={setNodeRef} aria-label={`${group.title} group`} className={isOver ? "bg-accent/30" : undefined}>
      <BoardGroupHeader group={group} />
      {!group.isCollapsed ? (
        <SortableContext items={group.cards.map((card) => card.id)} strategy={verticalListSortingStrategy}>
          {group.cards.map((card) => (
            <TableDataRow key={card.id} group={group} card={card} fields={fields} />
          ))}
          <TableAddRow boardId={boardId} workspaceId={workspaceId} group={group} fields={fields} />
        </SortableContext>
      ) : null}
    </section>
  )
}
