"use client"

import { useMemo } from "react"
import Link from "next/link"
import { useParams } from "next/navigation"
import { useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import { CalendarDays, CheckCircle2, FileText, GripVertical, MessageSquareText, Paperclip } from "lucide-react"
import { format } from "date-fns"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Progress } from "@/components/ui/progress"
import { cn } from "@/lib/utils"
import type { Board, Card } from "@/features/boards/types"

export function KanbanCard({ board, card }: { board: Board; card: Card }) {
  const params = useParams<{ workspaceSlug: string; boardId: string }>()
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: card.id,
    data: { type: "kanban-card", card },
  })

  const statusField = board.fieldDefinitions.find((field) => field.id.endsWith("field-status"))
  const priorityField = board.fieldDefinitions.find((field) => field.id.endsWith("field-priority"))
  const status = statusField?.options.find((option) => option.id === card.status)
  const priority = priorityField?.options.find((option) => option.id === card.priority)
  const checklistProgress = useMemo(() => {
    const total = card.checklists.reduce((count, checklist) => count + checklist.items.length, 0)
    const done = card.checklists.reduce((count, checklist) => count + checklist.items.filter((item) => item.isDone).length, 0)
    return total === 0 ? 0 : Math.round((done / total) * 100)
  }, [card.checklists])

  return (
    <article
      ref={setNodeRef}
      className={cn(
        "rounded-xl border border-border bg-card p-3 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md",
        isDragging && "opacity-60"
      )}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      aria-label={card.title}
      aria-grabbed={isDragging}
    >
      <div className="mb-3 flex items-start gap-2">
        <button type="button" className="mt-0.5 cursor-grab rounded p-0.5 active:cursor-grabbing" aria-label={`Move ${card.title}`} {...attributes} {...listeners}>
          <GripVertical className="size-3.5 text-muted-foreground/60" />
        </button>
        <div className="min-w-0 flex-1">
          <Link href={`/${params.workspaceSlug}/boards/${params.boardId}/card/${card.id}` as never} className="line-clamp-2 text-sm font-semibold leading-5 text-foreground hover:text-primary">
            {card.title}
          </Link>
          {card.linkedPageId ? (
            <div className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
              <FileText className="size-3.5 text-primary" />
              <span className="truncate">{card.linkedPageId}</span>
            </div>
          ) : null}
        </div>
      </div>

      <div className="mb-3 flex flex-wrap gap-1.5">
        {status ? (
          <Badge className="rounded-full border" variant="secondary" style={{ backgroundColor: `${status.color}24`, borderColor: `${status.color}55`, color: status.color }}>
            {status.label}
          </Badge>
        ) : null}
        {priority ? (
          <Badge className="rounded-full border" variant="secondary" style={{ backgroundColor: `${priority.color}24`, borderColor: `${priority.color}55`, color: priority.color }}>
            {priority.label}
          </Badge>
        ) : null}
      </div>

      <div className="mb-3 space-y-2">
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span className="inline-flex items-center gap-1">
            <CheckCircle2 className="size-3.5" />
            Checklist
          </span>
          <span>{checklistProgress}%</span>
        </div>
        <Progress value={checklistProgress} className="h-2" />
      </div>

      <div className="flex items-center justify-between gap-3 text-xs text-muted-foreground">
        <div className="flex items-center gap-2">
          {card.dueDate ? (
            <span className="inline-flex items-center gap-1">
              <CalendarDays className="size-3.5" />
              {format(new Date(card.dueDate), "MMM d")}
            </span>
          ) : null}
          {card._count.comments > 0 ? (
            <span className="inline-flex items-center gap-1">
              <MessageSquareText className="size-3.5" />
              {card._count.comments}
            </span>
          ) : null}
          {card._count.attachments > 0 ? (
            <span className="inline-flex items-center gap-1">
              <Paperclip className="size-3.5" />
              {card._count.attachments}
            </span>
          ) : null}
        </div>

        <div className="-space-x-2">
          {card.members.slice(0, 2).map((member) => (
            <Avatar key={member.id} className="inline-flex size-7 border-2 border-card">
              <AvatarFallback className="text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>
                {member.initials}
              </AvatarFallback>
            </Avatar>
          ))}
        </div>
      </div>
    </article>
  )
}
