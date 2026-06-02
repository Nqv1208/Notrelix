import type { Card, FieldDefinition } from "@/features/boards/types"
import { CellDate } from "./cells/cell-date"
import { CellLinkedPage } from "./cells/cell-linked-page"
import { CellPerson } from "./cells/cell-person"
import { CellProgress } from "./cells/cell-progress"
import { CellStatus } from "./cells/cell-status"
import { CellText } from "./cells/cell-text"

export function TableCell({ card, field }: { card: Card; field: FieldDefinition }) {
  if (field.id.endsWith("field-title")) return <CellText card={card} field={field} />
  if (field.fieldType === "person") return <CellPerson members={card.members} />
  if (field.fieldType === "date") return <CellDate card={card} field={field} value={card.dueDate} />
  if (field.fieldType === "linked_page") return <CellLinkedPage pageId={card.linkedPageId} />
  if (field.fieldType === "progress") return <CellProgress card={card} />

  if (field.fieldType === "select") {
    const value = field.id.endsWith("field-priority") ? card.priority : card.status
    return <CellStatus card={card} field={field} value={value} options={field.options} />
  }

  return <span className="truncate text-muted-foreground">{String(card.fieldValues[field.id] ?? "")}</span>
}
