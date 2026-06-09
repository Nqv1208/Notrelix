"use client"

import { useEffect, useRef, useState } from "react"
import { Plus, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

export function KanbanAddColumn({
  onCreateColumn,
}: {
  onCreateColumn: (title: string) => void
}) {
  const [isOpen, setIsOpen] = useState(false)
  const [title, setTitle] = useState("")
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (isOpen) requestAnimationFrame(() => inputRef.current?.focus())
  }, [isOpen])

  function submit() {
    const nextTitle = title.trim()
    if (!nextTitle) return
    onCreateColumn(nextTitle)
    setTitle("")
    setIsOpen(false)
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
        className="flex w-[290px] shrink-0 items-center justify-center gap-2 rounded-2xl border border-dashed border-border bg-muted/10 p-4 text-sm font-semibold text-muted-foreground transition hover:border-primary hover:bg-muted/20 hover:text-foreground"
        aria-label="Add new column"
      >
        <Plus className="size-4" />
        Add column
      </button>
    )
  }

  return (
    <form
      className="flex w-[290px] shrink-0 flex-col gap-2 rounded-2xl border border-border bg-card p-3 shadow-sm"
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
        placeholder="Enter column title..."
        className="h-9 text-sm"
        aria-label="New column title"
      />
      <div className="flex items-center gap-2">
        <Button type="submit" size="sm" className="h-7" disabled={!title.trim()}>
          Add column
        </Button>
        <Button type="button" variant="ghost" size="icon-sm" onClick={cancel} aria-label="Cancel">
          <X className="size-4" />
        </Button>
      </div>
    </form>
  )
}
