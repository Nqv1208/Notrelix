import { Plus } from "lucide-react"
import { Button } from "@notrelix/ui-web"
import { Input } from "@notrelix/ui-web"
import { KanbanFilterMenu } from "./kanban-filter-menu"
import { KanbanSortMenu } from "./kanban-sort-menu"
import type { KanbanSortOption } from "@notrelix/work-management-core"

interface KanbanToolbarProps {
  searchQuery: string
  onSearchChange: (value: string) => void
  onClearFilters: () => void
  activeSort: KanbanSortOption
  onSortChange: (option: KanbanSortOption) => void
  onCreateCard: () => void
  onAddColumn: () => void
}

export function KanbanToolbar({
  searchQuery,
  onSearchChange,
  onClearFilters,
  activeSort,
  onSortChange,
  onCreateCard,
  onAddColumn,
}: KanbanToolbarProps) {
  return (
    <div className="flex items-center gap-2 border-b px-4 py-2">
      <Input
        placeholder="Search cards..."
        value={searchQuery}
        onChange={(e) => onSearchChange(e.target.value)}
        className="h-8 w-64"
      />
      <KanbanSortMenu activeSort={activeSort} onSortChange={onSortChange} />
      <div className="flex-1" />
      <Button variant="outline" size="sm" onClick={onAddColumn}>
        <Plus className="mr-1 size-4" />
        Column
      </Button>
      <Button size="sm" onClick={onCreateCard}>
        <Plus className="mr-1 size-4" />
        Card
      </Button>
    </div>
  )
}
