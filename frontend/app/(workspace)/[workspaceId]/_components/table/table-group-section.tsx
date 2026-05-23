"use client"

import { useState } from "react"
import { ChevronDown, MoreHorizontal, Plus } from "lucide-react"
import { useDroppable } from "@dnd-kit/core"
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { useDeleteGroup, useDuplicateGroup, useUpdateGroup } from "@/features/boards/hooks"
import type { Board, BoardGroup, BoardTableColumn } from "@/features/boards/types"
import { cn } from "@/lib/utils"
import { TableAddTaskRow } from "./table-add-task-row"
import { TableRow } from "./table-row"
import { getGroupToneClass } from "./table-utils"

export function TableGroupSection({
  board,
  group,
  columns,
  gridTemplate,
  selectedCardIdSet,
  activeDetailCardId,
  onSetCardSelected,
  onOpenDetail,
  onToggleGroup,
}: {
  board: Board
  group: BoardGroup
  columns: BoardTableColumn[]
  gridTemplate: string
  selectedCardIdSet: Set<string>
  activeDetailCardId?: string | null
  onSetCardSelected: (cardId: string, selected: boolean) => void
  onOpenDetail: (cardId: string) => void
  onToggleGroup: (groupId: string) => void
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: group.id,
    data: { type: "group", group },
  })
  const inputId = `add-card-${group.id}`

  return (
    <section ref={setNodeRef} aria-label={`${group.title} group`} className={cn("bg-card", isOver && "bg-accent/30")}>
      <GroupHeader board={board} group={group} inputId={inputId} onToggleGroup={onToggleGroup} />
      {!group.isCollapsed ? (
        <SortableContext items={group.cards.map((card) => card.id)} strategy={verticalListSortingStrategy}>
          {group.cards.map((card) => (
            <TableRow
              key={card.id}
              board={board}
              group={group}
              card={card}
              columns={columns}
              gridTemplate={gridTemplate}
              isChecked={selectedCardIdSet.has(card.id)}
              isDetailSelected={activeDetailCardId === card.id}
              onSelect={(selected) => onSetCardSelected(card.id, selected)}
              onOpenDetail={() => onOpenDetail(card.id)}
            />
          ))}
          <TableAddTaskRow boardId={board.id} workspaceId={board.workspaceId} group={group} columns={columns} gridTemplate={gridTemplate} inputId={inputId} />
        </SortableContext>
      ) : null}
    </section>
  )
}

function GroupHeader({
  board,
  group,
  inputId,
  onToggleGroup,
}: {
  board: Board
  group: BoardGroup
  inputId: string
  onToggleGroup: (groupId: string) => void
}) {
  const [editing, setEditing] = useState(false)
  const [title, setTitle] = useState(group.title)
  const updateGroup = useUpdateGroup(board.id, board.workspaceId)
  const deleteGroup = useDeleteGroup(board.id, board.workspaceId)
  const duplicateGroup = useDuplicateGroup(board.id, board.workspaceId)

  function commitTitle() {
    const next = title.trim()
    setEditing(false)
    if (!next || next === group.title) {
      setTitle(group.title)
      return
    }
    updateGroup.mutate({ groupId: group.id, title: next })
  }

  return (
    <div className="sticky left-0 z-10 flex min-h-12 items-center justify-between border-b border-border bg-card px-4">
      <div className="flex min-w-0 items-center gap-2">
        <button
          type="button"
          className="rounded p-1 text-muted-foreground transition hover:bg-muted hover:text-foreground"
          aria-label={group.isCollapsed ? `Expand ${group.title}` : `Collapse ${group.title}`}
          onClick={() => onToggleGroup(group.id)}
        >
          <ChevronDown className={cn("size-4 transition", group.isCollapsed && "-rotate-90")} />
        </button>
        <span className={cn("size-2.5 rounded-full", !group.color && getGroupToneClass(group.title))} style={group.color ? { backgroundColor: group.color } : undefined} />
        {editing ? (
          <Input
            autoFocus
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            onBlur={commitTitle}
            onKeyDown={(event) => {
              if (event.key === "Enter") commitTitle()
              if (event.key === "Escape") {
                setTitle(group.title)
                setEditing(false)
              }
            }}
            className="h-8 w-56 border-0 bg-muted px-2 text-sm font-semibold shadow-none focus-visible:ring-1"
            aria-label={`Rename ${group.title}`}
          />
        ) : (
          <button type="button" className="min-w-0 text-left" onDoubleClick={() => setEditing(true)}>
            <h3 className="truncate text-sm font-semibold text-foreground">{group.title}</h3>
          </button>
        )}
        <Badge variant="secondary" className="rounded-full">
          {group.cards.length}
        </Badge>
      </div>
      <div className="flex items-center gap-1">
        <Button
          variant="ghost"
          size="sm"
          className="h-8"
          onClick={() => document.getElementById(inputId)?.focus()}
        >
          <Plus className="size-4" />
          Add
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon-sm" aria-label={`${group.title} group menu`}>
              <MoreHorizontal className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => setEditing(true)}>Rename group</DropdownMenuItem>
            <DropdownMenuItem onClick={() => duplicateGroup.mutate(group.id)}>Duplicate group</DropdownMenuItem>
            <DropdownMenuItem onClick={() => onToggleGroup(group.id)}>
              {group.isCollapsed ? "Expand group" : "Collapse group"}
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            {GROUP_COLORS.map((color) => (
              <DropdownMenuItem key={color} onClick={() => updateGroup.mutate({ groupId: group.id, color })}>
                <span className="size-2.5 rounded-full" style={{ backgroundColor: color }} />
                {color}
              </DropdownMenuItem>
            ))}
            <DropdownMenuSeparator />
            <DropdownMenuItem className="text-destructive" onClick={() => deleteGroup.mutate(group.id)}>
              Delete group
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  )
}

const GROUP_COLORS = ["#579bfc", "#00c875", "#fdab3d", "#e2445c", "#a25ddc", "#66ccff"]
