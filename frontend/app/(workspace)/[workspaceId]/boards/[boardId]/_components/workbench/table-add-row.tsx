import { Plus } from "lucide-react"
import type { FieldDefinition } from "@/features/boards/types"
import { getTableGridTemplate } from "./table-header-row"

export function TableAddRow({ fields, groupTitle }: { fields: FieldDefinition[]; groupTitle: string }) {
  return (
    <button
      type="button"
      className="grid w-full border-b border-border px-4 py-2 text-left text-sm text-muted-foreground transition hover:bg-muted/50 hover:text-foreground"
      style={{ gridTemplateColumns: getTableGridTemplate(fields) }}
      aria-label={`Add task to ${groupTitle}`}
    >
      <span className="flex items-center gap-2">
        <Plus className="size-4" />
        Add task
      </span>
    </button>
  )
}
