"use client"

import { format } from "date-fns"
import { Input } from "@/components/ui/input"
import { useUpdateCard } from "@/features/boards/hooks"
import type { Card, FieldDefinition } from "@/features/boards/types"

export function CellDate({ card, field, value }: { card: Card; field: FieldDefinition; value?: string }) {
  const updateCard = useUpdateCard(card.boardId)
  const inputValue = value ? format(new Date(value), "yyyy-MM-dd") : ""

  return (
    <Input
      type="date"
      aria-label={`Edit ${field.name}`}
      className="h-8 border-0 bg-transparent px-0 text-sm text-muted-foreground shadow-none focus-visible:bg-muted focus-visible:px-2 focus-visible:ring-1"
      value={inputValue}
      onChange={(event) => {
        const next = event.target.value
        updateCard.mutate({
          cardId: card.id,
          patch: { dueDate: next ? new Date(`${next}T09:00:00.000Z`).toISOString() : null },
        })
      }}
    />
  )
}
