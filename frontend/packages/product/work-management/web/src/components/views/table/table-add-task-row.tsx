"use client"

import { useState } from "react"
import { Plus } from "lucide-react"
import { Button } from "@notrelix/ui-web"
import { Input } from "@notrelix/ui-web"
import { useCreateCard } from "@notrelix/work-management-state"
import type { BoardGroup, BoardTableColumn } from "@notrelix/work-management-core"
import { generatePosition } from "@notrelix/work-management-core"

export function TableAddTaskRow({
  boardId,
  workspaceId,
  group,
  columns,
  gridTemplate,
  inputId,
}: {
  boardId: string
  workspaceId: string
  group: BoardGroup
  columns: BoardTableColumn[]
  gridTemplate: string
  inputId: string
}) {
  const [title, setTitle] = useState("")
  const createCard = useCreateCard(boardId, workspaceId)
  const firstColumn = columns[0]

  function submit() {
    const nextTitle = title.trim()
    if (!nextTitle) return
    const lastPosition = group.cards.at(-1)?.position
    createCard.mutate({
      listId: group.id,
      title: nextTitle,
      position: generatePosition(lastPosition, undefined),
    })
    setTitle("")
  }

  return (
    <div
      role="row"
      className="grid min-h-[44px] items-center border-b border-l-[3px] border-border/70 border-l-transparent bg-table-row text-sm text-muted-foreground transition-colors duration-150 focus-within:bg-table-row-hover hover:bg-table-row-hover/60"
      style={{ gridTemplateColumns: gridTemplate }}
    >
      <div className="sticky left-0 z-10 flex h-full items-center justify-center border-r border-border/70 bg-inherit px-3">
        <Plus className="size-4 text-muted-foreground/50" />
      </div>
      {columns.map((column, index) => (
        <div key={column.id} role="gridcell" className="min-w-0 border-r border-border/70 px-3 py-1.5">
          {index === 0 ? (
            <form
              className="flex min-w-0 items-center gap-2"
              onSubmit={(event) => {
                event.preventDefault()
                submit()
              }}
            >
              <Input
                id={inputId}
                value={title}
                onClick={(event) => event.stopPropagation()}
                onChange={(event) => setTitle(event.target.value)}
                placeholder={`Add task to ${group.title}`}
                className="h-8 border-0 bg-transparent px-0 shadow-none focus-visible:bg-background focus-visible:px-2 focus-visible:ring-1"
                aria-label={`Add task to ${group.title}`}
              />
              {title.trim() ? (
                <Button type="submit" size="sm" className="h-7">
                  Add
                </Button>
              ) : null}
            </form>
          ) : firstColumn ? (
            <span className="text-xs text-muted-foreground/70">{column.field.name}</span>
          ) : null}
        </div>
      ))}
      <div className="border-r border-border/70" />
    </div>
  )
}
