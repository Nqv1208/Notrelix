"use client"

import type { DraggableAttributes } from "@dnd-kit/core"
import type { ButtonHTMLAttributes } from "react"
import { GripVertical, Plus } from "lucide-react"
import { Button } from "@/components/ui/button"

export function BlockDragHandle({
  label,
  attributes,
  listeners,
  onAdd,
}: {
  label: string
  attributes: DraggableAttributes
  listeners?: ButtonHTMLAttributes<HTMLButtonElement>
  onAdd: () => void
}) {
  return (
    <div className="flex w-12 shrink-0 items-start justify-end gap-0.5 pt-1 opacity-0 transition group-hover/block:opacity-100 group-focus-within/block:opacity-100">
      <Button
        variant="ghost"
        size="icon-xs"
        className="cursor-grab active:cursor-grabbing"
        aria-label={`Move ${label}`}
        {...attributes}
        {...listeners}
      >
        <GripVertical className="size-3.5 text-muted-foreground" />
      </Button>
      <Button variant="ghost" size="icon-xs" aria-label="Add block below" onClick={onAdd}>
        <Plus className="size-3.5" />
      </Button>
    </div>
  )
}
