"use client"

import { useState } from "react"
import { Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useCreateCard } from "@/features/boards/hooks"
import type { BoardGroup, BoardTableColumn } from "@/features/boards/types"
import { generatePosition } from "@/features/boards/utils/fractional-index"

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
      className="grid min-h-11 items-center border-b border-border bg-card text-sm text-muted-foreground transition focus-within:bg-muted/40 hover:bg-muted/30"
      style={{ gridTemplateColumns: gridTemplate }}
    >
      <div className="sticky left-0 z-10 flex h-full items-center justify-center border-r border-border bg-inherit px-3">
        <Plus className="size-4" />
      </div>
      {columns.map((column, index) => (
        <div key={column.id} role="gridcell" className="min-w-0 border-r border-border px-3 py-1.5">
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
      <div className="border-r border-border" />
    </div>
  )
}
