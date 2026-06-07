"use client"

import { useState } from "react"
import { Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useCreateCard } from "@/features/boards/hooks"
import type { BoardGroup, FieldDefinition } from "@/features/boards/types"
import { generatePosition } from "@/features/boards/utils/fractional-index"
import { getTableGridTemplate } from "./table-header-row"

export function TableAddRow({
  boardId,
  workspaceId,
  group,
  fields,
}: {
  boardId: string
  workspaceId: string
  group: BoardGroup
  fields: FieldDefinition[]
}) {
  const [title, setTitle] = useState("")
  const createCard = useCreateCard(boardId, workspaceId)

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
      className="grid w-full border-b border-border px-4 py-1.5 text-sm text-muted-foreground transition hover:bg-muted/50 hover:text-foreground focus-within:bg-muted/50"
      style={{ gridTemplateColumns: getTableGridTemplate(fields) }}
    >
      <form
        className="flex min-w-0 items-center gap-2"
        onSubmit={(event) => {
          event.preventDefault()
          submit()
        }}
      >
        <Plus className="size-4 shrink-0 text-muted-foreground/50" />
        <Input
          value={title}
          onClick={(event) => event.stopPropagation()}
          onChange={(event) => setTitle(event.target.value)}
          placeholder={`Add task to ${group.title}`}
          className="h-8 border-0 bg-transparent px-0 shadow-none focus-visible:bg-background focus-visible:px-2 focus-visible:ring-1"
          aria-label={`Add task to ${group.title}`}
        />
        {title.trim() ? (
          <Button type="submit" size="sm" className="h-7 shrink-0">
            Add
          </Button>
        ) : null}
      </form>
    </div>
  )
}
