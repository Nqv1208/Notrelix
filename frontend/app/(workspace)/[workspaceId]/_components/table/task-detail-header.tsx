"use client"

import { useState } from "react"
import { Bell, BellOff, CalendarDays, MoreHorizontal, X } from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { useDeleteCard, useDuplicateCard, useUpdateCard } from "@/features/boards/hooks"
import type { Board, CardDetail } from "@/features/boards/types"
import { cn } from "@/lib/utils"
import { formatDate, getOptionToneClass } from "./table-utils"

export function TaskDetailHeader({
  board,
  card,
  onClose,
}: {
  board: Board
  card: CardDetail
  onClose: () => void
}) {
  const [title, setTitle] = useState(card.title)
  const [isWatched, setIsWatched] = useState(card.isWatched)
  const updateCard = useUpdateCard(card.boardId, card.workspaceId)
  const deleteCard = useDeleteCard(card.boardId, card.workspaceId)
  const duplicateCard = useDuplicateCard(card.boardId, card.workspaceId)
  const assignee = card.members[0]
  const statusField = board.fieldDefinitions.find((field) => field.id.endsWith("field-status"))
  const status = statusField?.options.find((option) => option.id === card.status)

  function commitTitle() {
    const nextTitle = title.trim()
    if (!nextTitle || nextTitle === card.title) {
      setTitle(card.title)
      return
    }
    updateCard.mutate({ cardId: card.id, patch: { title: nextTitle } })
  }

  return (
    <header className="sticky top-0 z-20 border-b border-border bg-popover">
      <div className="flex items-start gap-3 px-4 py-3">
        <Button variant="ghost" size="icon-sm" aria-label="Close task details" onClick={onClose}>
          <X className="size-4" />
        </Button>

        <div className="min-w-0 flex-1">
          <p className="mb-1 text-xs text-muted-foreground">{board.title}</p>
          <Input
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            onBlur={commitTitle}
            onKeyDown={(event) => {
              if (event.key === "Enter") event.currentTarget.blur()
              if (event.key === "Escape") {
                setTitle(card.title)
                event.currentTarget.blur()
              }
            }}
            className="h-auto border-0 bg-transparent px-0 py-0 text-lg font-semibold shadow-none focus-visible:ring-0"
            aria-label="Edit task title"
          />
          <div className="mt-3 flex flex-wrap items-center gap-2">
            {assignee ? (
              <Badge variant="secondary" className="gap-2 rounded-full border border-border bg-card">
                <Avatar className="size-5">
                  <AvatarFallback className="text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: assignee.color }}>
                    {assignee.initials}
                  </AvatarFallback>
                </Avatar>
                {assignee.name}
              </Badge>
            ) : (
              <Badge variant="secondary" className="rounded-full border border-border bg-card text-muted-foreground">
                Unassigned
              </Badge>
            )}
            {status ? (
              <Badge variant="secondary" className={cn("rounded-full border", getOptionToneClass(status.id))}>
                {status.label}
              </Badge>
            ) : null}
            {card.dueDate ? (
              <Badge variant="secondary" className="rounded-full border border-border bg-card">
                <CalendarDays className="size-3.5" />
                {formatDate(card.dueDate)}
              </Badge>
            ) : null}
          </div>
        </div>

        <div className="flex items-center gap-1">
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label={isWatched ? "Unfollow task" : "Follow task"}
                onClick={() => setIsWatched((current) => !current)}
              >
                {isWatched ? <Bell className="size-4" /> : <BellOff className="size-4" />}
              </Button>
            </TooltipTrigger>
            <TooltipContent>{isWatched ? "Following" : "Follow"}</TooltipContent>
          </Tooltip>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon-sm" aria-label="Task menu">
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem>Copy task link</DropdownMenuItem>
              <DropdownMenuItem>Move to group</DropdownMenuItem>
              <DropdownMenuItem onClick={() => duplicateCard.mutate(card.id)}>Duplicate task</DropdownMenuItem>
              <DropdownMenuItem className="text-destructive" onClick={() => {
                deleteCard.mutate(card.id)
                onClose()
              }}>Archive task</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
      <Separator />
    </header>
  )
}
