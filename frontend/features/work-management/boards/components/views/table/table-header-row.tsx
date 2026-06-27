"use client"

import type { PointerEvent } from "react"
import { GripVertical, MoreHorizontal } from "lucide-react"
import { Checkbox } from "@/components/ui/checkbox"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { useDeleteColumn, useUpdateColumn } from "@/features/work-management/hooks"
import type { BoardTableColumn } from "@/features/work-management/types"

export function TableHeaderRow({
  boardId,
  workspaceId,
  columns,
  gridTemplate,
  isAllSelected,
  onToggleAll,
  onResizeColumn,
  onHideColumn,
}: {
  boardId: string
  workspaceId: string
  columns: BoardTableColumn[]
  gridTemplate: string
  isAllSelected: boolean
  onToggleAll: () => void
  onResizeColumn: (columnId: string, width: number) => void
  onHideColumn: (columnId: string) => void
}) {
  const updateColumn = useUpdateColumn(boardId, workspaceId)
  const deleteColumn = useDeleteColumn(boardId, workspaceId)

  return (
    <div
      role="row"
      className="grid min-h-[44px] border-b border-border bg-table-header text-[11px] font-semibold tracking-wider text-muted-foreground uppercase"
      style={{ gridTemplateColumns: gridTemplate }}
    >
      <div role="columnheader" className="sticky left-0 z-30 flex items-center gap-2 border-r border-border bg-table-header px-3">
        <Checkbox checked={isAllSelected} onCheckedChange={onToggleAll} aria-label="Select all tasks" />
        <GripVertical className="size-3.5 text-muted-foreground/50" />
      </div>
      {columns.map((column) => (
        <div key={column.id} role="columnheader" className="group/header relative flex min-w-0 items-center justify-between border-r border-border px-3">
          <span className="truncate">{column.field.name}</span>
          <div className="flex items-center">
            {!column.field.id.endsWith("field-title") ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <button
                    type="button"
                    className="rounded p-1 text-muted-foreground opacity-0 transition hover:bg-background/30 hover:text-foreground group-hover/header:opacity-100"
                    aria-label={`${column.field.name} column menu`}
                  >
                    <MoreHorizontal className="size-3.5" />
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-40">
                  <DropdownMenuItem
                    onClick={() => {
                      const name = window.prompt("Column name", column.field.name)
                      if (name?.trim()) updateColumn.mutate({ columnId: column.id, name: name.trim() })
                    }}
                  >
                    Rename column
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => onHideColumn(column.id)}>Hide column</DropdownMenuItem>
                  <DropdownMenuItem className="text-destructive" onClick={() => deleteColumn.mutate(column.id)}>
                    Delete column
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            ) : null}
            <ResizeHandle column={column} onResizeColumn={onResizeColumn} />
          </div>
        </div>
      ))}
      <div role="columnheader" aria-label="Actions column spacing" className="border-r border-border" />
    </div>
  )
}

function ResizeHandle({
  column,
  onResizeColumn,
}: {
  column: BoardTableColumn
  onResizeColumn: (columnId: string, width: number) => void
}) {
  function handlePointerDown(event: PointerEvent<HTMLButtonElement>) {
    event.preventDefault()
    event.stopPropagation()
    const startX = event.clientX
    const startWidth = column.width

    function handlePointerMove(moveEvent: globalThis.PointerEvent) {
      const nextWidth = Math.max(column.minWidth, startWidth + moveEvent.clientX - startX)
      onResizeColumn(column.id, nextWidth)
    }

    function handlePointerUp() {
      window.removeEventListener("pointermove", handlePointerMove)
      window.removeEventListener("pointerup", handlePointerUp)
    }

    window.addEventListener("pointermove", handlePointerMove)
    window.addEventListener("pointerup", handlePointerUp)
  }

  return (
    <button
      type="button"
      aria-label={`Resize ${column.field.name}`}
      className="absolute right-0 top-0 h-full w-2 cursor-col-resize touch-none border-r border-transparent transition hover:border-primary"
      onPointerDown={handlePointerDown}
    />
  )
}
