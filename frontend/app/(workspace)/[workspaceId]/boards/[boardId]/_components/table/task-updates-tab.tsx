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
    <div className="flex flex-col gap-3 p-3.5">
      <UpdateComposer cardId={card.id} members={card.members.length > 0 ? card.members : card.watchers} />

      {isLoading ? (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-14 rounded-lg" />
          <Skeleton className="h-14 rounded-lg" />
        </div>
      ) : data.length === 0 ? (
        <TaskDetailEmptyState
          icon={MessageSquareText}
          title="No updates yet"
          description="Share decisions, blockers, and context so everyone can follow the task."
        />
      ) : (
        <div className="flex flex-col gap-1.5 mt-1">
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
    <article className="group/comment flex items-start gap-2.5 rounded-lg px-2 py-2 transition-colors hover:bg-muted/30">
      <Avatar className="size-7 shrink-0 mt-0.5">
        <AvatarFallback className="text-[9px] font-bold text-primary-foreground" style={{ backgroundColor: update.author.color }}>
          {update.author.initials}
        </AvatarFallback>
      </Avatar>
      <div className="min-w-0 flex-1">
        <div className="flex items-baseline justify-between gap-2">
          <div className="flex items-baseline gap-2">
            <span className="text-xs font-semibold text-foreground">{update.author.name}</span>
            <span className="text-[10px] text-muted-foreground/70">
              {new Date(update.createdAt).toLocaleDateString(undefined, {
                month: "short",
                day: "numeric",
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
          </div>
          <div className="opacity-0 transition-opacity shrink-0 group-hover/comment:opacity-100">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon-sm" className="size-6 p-0 hover:bg-muted" aria-label="Update actions">
                  <MoreHorizontal className="size-3.5 text-muted-foreground" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-24">
                <DropdownMenuItem onClick={() => setEditing(true)} className="text-xs py-1">
                  <Pencil className="mr-1.5 size-3.5" />
                  Edit
                </DropdownMenuItem>
                <DropdownMenuItem className="text-destructive text-xs py-1" onClick={onDelete}>
                  <Trash2 className="mr-1.5 size-3.5" />
                  Delete
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
        {editing ? (
          <div className="mt-2 space-y-2">
            <Textarea
              value={body}
              onChange={(event) => setBody(event.target.value)}
              className="min-h-16 text-xs resize-none bg-background p-2"
            />
            <div className="flex justify-end gap-1.5">
              <Button
                variant="ghost"
                size="sm"
                className="h-7 px-2 text-[10px] gap-1"
                onClick={() => {
                  setBody(update.body)
                  setEditing(false)
                }}
              >
                <X className="size-3" />
                Cancel
              </Button>
              <Button
                size="sm"
                className="h-7 px-2 text-[10px] gap-1"
                onClick={() => {
                  const next = body.trim()
                  if (next) onSave(next)
                  setEditing(false)
                }}
              >
                <Check className="size-3" />
                Save
              </Button>
            </div>
          </div>
        ) : (
          <p className="mt-1 text-xs leading-relaxed text-muted-foreground whitespace-pre-wrap">{update.body}</p>
        )}
      </div>
    </article>
  )
}
