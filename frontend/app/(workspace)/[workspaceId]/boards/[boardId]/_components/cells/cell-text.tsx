"use client"

import { useState } from "react"
import Link from "next/link"
import { ArrowUpRight, MessageSquareText, Paperclip } from "lucide-react"
import { useParams } from "next/navigation"
import { Input } from "@/components/ui/input"
import { useUpdateCard } from "@/features/boards/hooks"
import type { Card, FieldDefinition } from "@/features/boards/types"

export function CellText({ card, field }: { card: Card; field: FieldDefinition }) {
  const [editing, setEditing] = useState(false)
  const [value, setValue] = useState(card.title)
  const updateCard = useUpdateCard(card.boardId)
  const params = useParams<{ workspaceId: string; boardId: string }>()

  function commit() {
    const next = value.trim()
    setEditing(false)
    if (!next || next === card.title) {
      setValue(card.title)
      return
    }
    updateCard.mutate({ cardId: card.id, patch: { title: next } })
  }

  if (editing) {
    return (
      <Input
        autoFocus
        className="h-8 border-0 bg-muted px-2 shadow-none focus-visible:ring-1"
        value={value}
        onChange={(event) => setValue(event.target.value)}
        onBlur={commit}
        onKeyDown={(event) => {
          if (event.key === "Enter") commit()
          if (event.key === "Escape") {
            setValue(card.title)
            setEditing(false)
          }
        }}
        aria-label={`Edit ${field.name}`}
      />
    )
  }

  return (
    <div className="min-w-0">
      <div className="flex min-w-0 items-center gap-2">
        <button type="button" className="min-w-0 text-left" onDoubleClick={() => setEditing(true)} onClick={() => setEditing(true)}>
          <p className="truncate font-medium text-foreground">{card.title}</p>
        </button>
        <Link href={`/${params.workspaceId}/boards/${params.boardId}/card/${card.id}` as never} className="shrink-0 rounded p-1 text-muted-foreground opacity-0 transition hover:bg-muted hover:text-foreground group-hover:opacity-100" aria-label={`Open ${card.title}`}>
          <ArrowUpRight className="size-3.5" />
        </Link>
      </div>
      <div className="mt-0.5 flex items-center gap-2 text-xs text-muted-foreground">
        {card._count.comments > 0 ? (
          <span className="inline-flex items-center gap-1">
            <MessageSquareText className="size-3" />
            {card._count.comments}
          </span>
        ) : null}
        {card._count.attachments > 0 ? (
          <span className="inline-flex items-center gap-1">
            <Paperclip className="size-3" />
            {card._count.attachments}
          </span>
        ) : null}
      </div>
    </div>
  )
}
