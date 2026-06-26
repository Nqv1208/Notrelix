"use client"

import { ArrowUpDown } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

export type KanbanSortOption = "position" | "title" | "priority" | "dueDate"

export function KanbanSortMenu({
  activeSort,
  onSortChange,
}: {
  activeSort: KanbanSortOption
  onSortChange: (option: KanbanSortOption) => void
}) {
  const labels: Record<KanbanSortOption, string> = {
    position: "Default",
    title: "Title (A-Z)",
    priority: "Priority",
    dueDate: "Due Date",
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" size="sm" className="bg-card">
          <ArrowUpDown className="size-4" />
          Sort: {labels[activeSort]}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-44">
        <DropdownMenuLabel>Sort Cards By</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={() => onSortChange("position")}>
          Default Position
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => onSortChange("title")}>
          Title (A-Z)
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => onSortChange("priority")}>
          Priority
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => onSortChange("dueDate")}>
          Due Date
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
