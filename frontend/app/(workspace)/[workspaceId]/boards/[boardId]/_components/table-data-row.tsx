import { GripVertical } from "lucide-react"
import { useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import type { BoardGroup, Card, FieldDefinition } from "@/features/boards/types"
import { TableCell } from "./table-cell"
import { getTableGridTemplate } from "./table-header-row"

export function TableDataRow({
  group,
  card,
  fields,
}: {
  group: BoardGroup
  card: Card
  fields: FieldDefinition[]
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: card.id,
    data: { type: "card", card, group },
  })

  return (
    <div
      ref={setNodeRef}
      role="row"
      aria-label={`${card.title} in ${group.title}`}
      aria-grabbed={isDragging}
      className="group grid min-h-12 items-center border-b border-border bg-card px-4 py-2 text-sm transition hover:bg-muted/50"
      style={{ gridTemplateColumns: getTableGridTemplate(fields), transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.55 : 1 }}
    >
      {fields.map((field, index) => (
        <div key={field.id} role="gridcell" className="min-w-0 pr-3">
          <div className="flex min-w-0 items-center gap-2">
            {index === 0 ? (
              <button type="button" className="cursor-grab rounded p-0.5 active:cursor-grabbing" aria-label={`Move ${card.title}`} {...attributes} {...listeners}>
                <GripVertical className="size-3.5 shrink-0 text-muted-foreground/60" aria-hidden />
              </button>
            ) : null}
            <TableCell card={card} field={field} />
          </div>
        </div>
      ))}
    </div>
  )
}
