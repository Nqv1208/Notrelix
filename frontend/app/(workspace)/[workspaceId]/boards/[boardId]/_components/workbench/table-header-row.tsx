import { GripVertical } from "lucide-react"
import { SortableContext, horizontalListSortingStrategy, useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import type { FieldDefinition } from "@/features/boards/types"

const fieldWidth: Record<string, string> = {
  text: "minmax(280px,1.5fr)",
  person: "160px",
  select: "150px",
  date: "140px",
  linked_page: "170px",
  progress: "160px",
}

export function TableHeaderRow({ fields }: { fields: FieldDefinition[] }) {
  const template = fields.map((field) => fieldWidth[field.fieldType] ?? "150px").join(" ")

  return (
    <SortableContext items={fields.map((field) => field.id)} strategy={horizontalListSortingStrategy}>
      <div
        role="row"
        className="sticky top-0 z-10 grid border-b border-border bg-muted px-4 py-3 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground"
        style={{ gridTemplateColumns: template }}
      >
        {fields.map((field) => (
          <SortableColumnHeader key={field.id} field={field} />
        ))}
      </div>
    </SortableContext>
  )
}

function SortableColumnHeader({ field }: { field: FieldDefinition }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: field.id,
    data: { type: "column", field },
  })

  return (
    <div
      ref={setNodeRef}
      role="columnheader"
      className="flex min-w-0 items-center gap-1.5 pr-3"
      style={{ transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.6 : 1 }}
    >
      <button type="button" className="cursor-grab rounded p-0.5 active:cursor-grabbing" aria-label={`Reorder ${field.name} column`} {...attributes} {...listeners}>
        <GripVertical className="size-3.5 text-muted-foreground/60" aria-hidden />
      </button>
      <span className="truncate">{field.name}</span>
    </div>
  )
}

export function getTableGridTemplate(fields: FieldDefinition[]) {
  return fields.map((field) => fieldWidth[field.fieldType] ?? "150px").join(" ")
}
