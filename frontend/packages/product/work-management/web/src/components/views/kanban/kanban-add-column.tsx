"use client"

import { useState } from "react"
import { Plus } from "lucide-react"
import { Button } from "@notrelix/ui-web"
import { Input } from "@notrelix/ui-web"

interface KanbanAddColumnProps {
  onAdd: (title: string) => void
}

export function KanbanAddColumn({ onAdd }: KanbanAddColumnProps) {
  const [isAdding, setIsAdding] = useState(false)
  const [title, setTitle] = useState("")

  if (!isAdding) {
    return (
      <Button variant="ghost" onClick={() => setIsAdding(true)} className="h-10 w-full">
        <Plus className="mr-2 size-4" />
        Add Column
      </Button>
    )
  }

  return (
    <div className="flex flex-col gap-2 p-2">
      <Input
        placeholder="Column title"
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        autoFocus
      />
      <div className="flex gap-2">
        <Button size="sm" onClick={() => { onAdd(title); setTitle(""); setIsAdding(false) }}>
          Add
        </Button>
        <Button size="sm" variant="ghost" onClick={() => { setIsAdding(false); setTitle("") }}>
          Cancel
        </Button>
      </div>
    </div>
  )
}
