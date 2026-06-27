"use client"

import { Filter, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { Separator } from "@/components/ui/separator"
import type { KanbanFiltersState } from "@/features/work-management/types"

const STATUS_OPTIONS = [
  { id: "status-not-started", label: "Not Started" },
  { id: "status-working", label: "Working on it" },
  { id: "status-stuck", label: "Stuck" },
  { id: "status-done", label: "Done" },
  { id: "status-completed", label: "Completed" },
]

const PRIORITY_OPTIONS = [
  { id: "urgent", label: "Urgent" },
  { id: "high", label: "High" },
  { id: "medium", label: "Medium" },
  { id: "low", label: "Low" },
]

export function KanbanFilterMenu({
  filters,
  onFilterChange,
  onClear,
}: {
  filters: KanbanFiltersState
  onFilterChange: (key: keyof KanbanFiltersState, values: string[]) => void
  onClear: () => void
}) {
  const hasActiveFilters = filters.status.length > 0 || filters.priority.length > 0
  const activeCount = filters.status.length + filters.priority.length

  const handleToggle = (key: "status" | "priority", optionId: string) => {
    const current = filters[key]
    const next = current.includes(optionId)
      ? current.filter((id) => id !== optionId)
      : [...current, optionId]
    onFilterChange(key, next)
  }

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="outline" size="sm" className="bg-card">
          <Filter className="size-4" />
          Filter
          {activeCount > 0 ? (
            <span className="ml-1.5 flex size-4.5 items-center justify-center rounded-full bg-primary text-[10px] font-bold text-primary-foreground">
              {activeCount}
            </span>
          ) : null}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-56 p-3 space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold text-foreground uppercase tracking-wider">Filters</span>
          {hasActiveFilters ? (
            <Button variant="ghost" size="icon-xs" onClick={onClear} aria-label="Clear filters">
              <X className="size-3.5" />
            </Button>
          ) : null}
        </div>

        <Separator />

        <div className="space-y-2">
          <span className="text-xs font-medium text-muted-foreground">Status</span>
          <div className="space-y-1.5">
            {STATUS_OPTIONS.map((status) => (
              <label key={status.id} className="flex items-center gap-2 text-sm text-foreground cursor-pointer">
                <Checkbox
                  checked={filters.status.includes(status.id)}
                  onCheckedChange={() => handleToggle("status", status.id)}
                />
                {status.label}
              </label>
            ))}
          </div>
        </div>

        <Separator />

        <div className="space-y-2">
          <span className="text-xs font-medium text-muted-foreground">Priority</span>
          <div className="space-y-1.5">
            {PRIORITY_OPTIONS.map((priority) => (
              <label key={priority.id} className="flex items-center gap-2 text-sm text-foreground cursor-pointer">
                <Checkbox
                  checked={filters.priority.includes(priority.id)}
                  onCheckedChange={() => handleToggle("priority", priority.id)}
                />
                {priority.label}
              </label>
            ))}
          </div>
        </div>
      </PopoverContent>
    </Popover>
  )
}
