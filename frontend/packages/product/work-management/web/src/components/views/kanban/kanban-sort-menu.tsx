import type { KanbanSortOption } from "@notrelix/work-management-core";
import { ArrowUpDown } from "lucide-react";
import { Button } from "@notrelix/ui-web";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";

export function KanbanSortMenu({
  activeSort,
  onSortChange,
}: {
  activeSort: KanbanSortOption;
  onSortChange: (option: KanbanSortOption) => void;
}) {
  const labels: Record<KanbanSortOption, string> = {
    position: "Default",
    title: "Title (A-Z)",
    priority: "Priority",
    dueDate: "Due Date",
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className="h-8"
          aria-label={`Sort cards by ${labels[activeSort]}`}
        >
          <ArrowUpDown className="mr-2 size-4" aria-hidden="true" />
          {labels[activeSort]}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuLabel>Sort by</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {(Object.keys(labels) as KanbanSortOption[]).map((option) => (
          <DropdownMenuItem key={option} onClick={() => onSortChange(option)}>
            {labels[option]}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
