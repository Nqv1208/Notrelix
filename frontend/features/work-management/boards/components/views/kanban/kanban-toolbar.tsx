"use client"

import { Plus, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { KanbanFilterMenu } from "@/features/work-management/boards/components/views/kanban/kanban-filter-menu"
import { KanbanSortMenu, type KanbanSortOption } from "@/features/work-management/boards/components/views/kanban/kanban-sort-menu"
import type { KanbanFiltersState } from "@/features/work-management/types"

export function KanbanToolbar({
  searchQuery,
  onSearchChange,
  filters,
  onFilterChange,
  onClearFilters,
  activeSort,
  onSortChange,
  onCreateCard,
  onCreateColumn,
}: {
  searchQuery: string
  onSearchChange: (value: string) => void
  filters: KanbanFiltersState
  onFilterChange: (key: keyof KanbanFiltersState, values: string[]) => void
  onClearFilters: () => void
  activeSort: KanbanSortOption
  onSortChange: (option: KanbanSortOption) => void
  onCreateCard: () => void
  onCreateColumn: () => void
}) {
  return (
    <div className="flex min-h-14 shrink-0 flex-wrap items-center justify-between gap-3 bg-card pb-4 border-b border-border">
      {/* Search & Filters */}
      <div className="flex flex-wrap items-center gap-2 flex-1 min-w-0 max-w-xl">
        <div className="relative w-full max-w-xs">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            className="h-9 pl-9 rounded-full bg-muted/30 focus-visible:ring-1 focus-visible:ring-offset-0"
            placeholder="Search cards..."
            aria-label="Search cards"
          />
        </div>

        <KanbanFilterMenu
          filters={filters}
          onFilterChange={onFilterChange}
          onClear={onClearFilters}
        />

        <KanbanSortMenu
          activeSort={activeSort}
          onSortChange={onSortChange}
        />
      </div>

      {/* Action CTA Buttons */}
      <div className="flex items-center gap-2">
        <Button
          size="sm"
          onClick={onCreateCard}
          className="rounded-full bg-brand-violet hover:bg-brand-violet/90 text-white font-medium shadow-xs"
        >
          <Plus className="size-4" />
          New card
        </Button>
        <Button
          size="sm"
          variant="outline"
          onClick={onCreateColumn}
          className="rounded-lg bg-card"
        >
          <Plus className="size-4" />
          Add column
        </Button>
      </div>
    </div>
  )
}
