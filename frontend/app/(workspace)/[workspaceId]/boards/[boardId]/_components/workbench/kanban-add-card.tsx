"use client"

import { useEffect, useRef, useState } from "react"
import { Plus, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useCreateCard } from "@/features/boards/hooks"
import type { BoardGroup } from "@/features/boards/types"
import { generatePosition } from "@/features/boards/utils/fractional-index"

export function KanbanAddCard({
  boardId,
  workspaceId,
  group,
  isAdding,
  onToggleAdding,
}: {
  boardId: string
  workspaceId: string
  group: BoardGroup
  isAdding?: boolean
  onToggleAdding?: (adding: boolean) => void
}) {
  const [internalOpen, setInternalOpen] = useState(false)
  const [title, setTitle] = useState("")
  const inputRef = useRef<HTMLInputElement>(null)
  const createCard = useCreateCard(boardId, workspaceId)

  const isOpen = isAdding ?? internalOpen
  const setIsOpen = onToggleAdding ?? setInternalOpen

  useEffect(() => {
    if (isOpen) {
      requestAnimationFrame(() => inputRef.current?.focus())
    }
  }, [isOpen])

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
    requestAnimationFrame(() => inputRef.current?.focus())
  }

  function cancel() {
    setTitle("")
    setIsOpen(false)
  }

  if (!isOpen) {
    return (
      <button
        type="button"
        onClick={() => setIsOpen(true)}
        className="flex h-10 w-full items-center gap-2 rounded-xl border border-dashed border-border px-3 text-sm text-muted-foreground transition hover:border-primary hover:bg-card hover:text-foreground"
        aria-label={`Add card to ${group.title}`}
      >
        <Plus className="size-4" />
        Add card
      </button>
    )
  }

  return (
    <form
      className="rounded-xl border border-primary/50 bg-card p-3 shadow-sm"
      onSubmit={(event) => {
        event.preventDefault()
        submit()
      }}
    >
      <Input
        ref={inputRef}
        value={title}
        onChange={(event) => setTitle(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "Escape") cancel()
        }}
        placeholder="Enter card title..."
        className="mb-2 h-9 text-sm"
        aria-label={`New card title for ${group.title}`}
      />
      <div className="flex items-center gap-2">
        <Button type="submit" size="sm" className="h-7" disabled={!title.trim()}>
          Add card
        </Button>
        <Button type="button" variant="ghost" size="icon-sm" onClick={cancel} aria-label="Cancel">
          <X className="size-4" />
        </Button>
      </div>
    </form>
  )
}
