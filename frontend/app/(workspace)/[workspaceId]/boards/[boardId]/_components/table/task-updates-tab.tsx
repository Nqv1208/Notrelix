"use client"

import { useState } from "react"
import { Check, MessageSquareText, MoreHorizontal, Pencil, Trash2, X } from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Textarea } from "@/components/ui/textarea"
import { Skeleton } from "@/components/ui/skeleton"
import { useCardComments, useDeleteCardUpdate, useUpdateCardUpdate } from "@/features/boards/hooks"
import type { CardDetail, CardUpdate } from "@/features/boards/types"
import { TaskDetailEmptyState } from "./task-detail-empty-state"
import { UpdateComposer } from "./update-composer"

export function TaskUpdatesTab({ card }: { card: CardDetail }) {
  const { data = [], isLoading } = useCardComments(card.id)
  const updateMutation = useUpdateCardUpdate(card.id)
  const deleteMutation = useDeleteCardUpdate(card.id)

  return (
    <div className="flex flex-col gap-4 p-4">
      <UpdateComposer cardId={card.id} members={card.members.length > 0 ? card.members : card.watchers} />

      {isLoading ? (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-20 rounded-lg" />
          <Skeleton className="h-20 rounded-lg" />
        </div>
      ) : data.length === 0 ? (
        <TaskDetailEmptyState
          icon={MessageSquareText}
          title="No updates yet"
          description="Share decisions, blockers, and context so everyone can follow the task."
        />
      ) : (
        <div className="flex flex-col gap-3">
          {data.map((update) => (
            <UpdateItem
              key={update.id}
              update={update}
              onSave={(body) => updateMutation.mutate({ updateId: update.id, body })}
              onDelete={() => deleteMutation.mutate(update.id)}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function UpdateItem({
  update,
  onSave,
  onDelete,
}: {
  update: CardUpdate
  onSave: (body: string) => void
  onDelete: () => void
}) {
  const [editing, setEditing] = useState(false)
  const [body, setBody] = useState(update.body)

  return (
    <article className="rounded-lg border border-border bg-card p-3">
      <div className="flex items-center gap-2">
        <Avatar className="size-8">
          <AvatarFallback className="text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: update.author.color }}>
            {update.author.initials}
          </AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-foreground">{update.author.name}</p>
          <p className="text-xs text-muted-foreground">{new Date(update.createdAt).toLocaleString()}</p>
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon-sm" aria-label="Update actions">
              <MoreHorizontal className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => setEditing(true)}>
              <Pencil className="size-4" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem className="text-destructive" onClick={onDelete}>
              <Trash2 className="size-4" />
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      {editing ? (
        <div className="mt-3 space-y-2">
          <Textarea value={body} onChange={(event) => setBody(event.target.value)} className="min-h-24 bg-background" />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" size="sm" onClick={() => {
              setBody(update.body)
              setEditing(false)
            }}>
              <X className="size-4" />
              Cancel
            </Button>
            <Button size="sm" onClick={() => {
              const next = body.trim()
              if (next) onSave(next)
              setEditing(false)
            }}>
              <Check className="size-4" />
              Save
            </Button>
          </div>
        </div>
      ) : (
        <p className="mt-3 text-sm leading-6 text-muted-foreground">{update.body}</p>
      )}
    </article>
  )
}
