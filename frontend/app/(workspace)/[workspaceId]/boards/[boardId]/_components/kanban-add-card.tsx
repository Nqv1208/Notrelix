import { Plus } from "lucide-react"

export function KanbanAddCard({ groupTitle }: { groupTitle: string }) {
  return (
    <button
      type="button"
      className="flex h-10 w-full items-center gap-2 rounded-xl border border-dashed border-border px-3 text-sm text-muted-foreground transition hover:border-primary hover:bg-card hover:text-foreground"
      aria-label={`Add card to ${groupTitle}`}
    >
      <Plus className="size-4" />
      Add card
    </button>
  )
}
