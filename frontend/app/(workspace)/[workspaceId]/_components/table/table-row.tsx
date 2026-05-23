"use client"

import { MoreHorizontal, GripVertical } from "lucide-react"
import { useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import { Checkbox } from "@/components/ui/checkbox"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { useDeleteCard, useDuplicateCard } from "@/features/boards/hooks"
import type { Board, BoardGroup, BoardTableColumn, Card } from "@/features/boards/types"
import { cn } from "@/lib/utils"
import { TableCell } from "./table-cell"

export function TableRow({
  board,
  group,
  card,
  columns,
  gridTemplate,
  isChecked,
  isDetailSelected,
  onSelect,
  onOpenDetail,
}: {
  board: Board
  group: BoardGroup
  card: Card
  columns: BoardTableColumn[]
  gridTemplate: string
  isChecked: boolean
  isDetailSelected: boolean
  onSelect: (selected: boolean) => void
  onOpenDetail: () => void
}) {
  const deleteCard = useDeleteCard(board.id, board.workspaceId)
  const duplicateCard = useDuplicateCard(board.id, board.workspaceId)
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: card.id,
    data: { type: "card", card, group },
  })
  const rowTransition = [transition, "background-color 200ms ease", "box-shadow 200ms ease"].filter(Boolean).join(", ")

  return (
    <div
      ref={setNodeRef}
      role="row"
      aria-label={`${card.title} in ${group.title}`}
      aria-selected={isDetailSelected}
      aria-grabbed={isDragging}
      className={cn(
        "group/row grid min-h-12 cursor-pointer items-center border-b border-border bg-card text-sm transition-[background-color,box-shadow] duration-200 ease-out hover:bg-muted/50",
        isChecked && "bg-accent/30",
        isDetailSelected && "bg-accent/50 ring-1 ring-inset ring-primary/20",
        isDragging && "relative z-10 shadow-lg"
      )}
      style={{
        gridTemplateColumns: gridTemplate,
        transform: CSS.Transform.toString(transform),
        transition: rowTransition,
        opacity: isDragging ? 0.62 : 1,
      }}
      onClick={onOpenDetail}
    >
      <div role="gridcell" className="sticky left-0 z-10 flex h-full items-center gap-2 border-r border-border bg-inherit px-3">
        <Checkbox
          checked={isChecked}
          onCheckedChange={(checked) => onSelect(Boolean(checked))}
          onClick={(event) => event.stopPropagation()}
          aria-label={`Select ${card.title}`}
        />
        <button
          type="button"
          className="cursor-grab rounded p-0.5 text-muted-foreground/70 active:cursor-grabbing"
          aria-label={`Move ${card.title}`}
          onClick={(event) => event.stopPropagation()}
          {...attributes}
          {...listeners}
        >
          <GripVertical className="size-3.5" aria-hidden />
        </button>
      </div>
      {columns.map((column) => (
        <div key={column.id} role="gridcell" className="flex min-w-0 items-center border-r border-border px-3 py-2">
          <TableCell board={board} card={card} field={column.field} onOpenDetail={onOpenDetail} />
        </div>
      ))}
      <div role="gridcell" className="flex h-full items-center justify-center border-r border-border px-2">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              type="button"
              className="rounded p-1 text-muted-foreground opacity-0 transition hover:bg-muted hover:text-foreground group-hover/row:opacity-100"
              aria-label={`${card.title} row menu`}
              onClick={(event) => event.stopPropagation()}
            >
              <MoreHorizontal className="size-4" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" onClick={(event) => event.stopPropagation()}>
            <DropdownMenuItem onClick={onOpenDetail}>Open details</DropdownMenuItem>
            <DropdownMenuItem onClick={() => duplicateCard.mutate(card.id)}>Duplicate task</DropdownMenuItem>
            <DropdownMenuItem className="text-destructive" onClick={() => deleteCard.mutate(card.id)}>Delete task</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  )
}
