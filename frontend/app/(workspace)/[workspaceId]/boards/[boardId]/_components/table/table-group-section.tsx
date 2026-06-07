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
    <section ref={setNodeRef} aria-label={`${group.title} group`} className={cn("mb-2 overflow-hidden rounded-sm bg-table-bg ring-1 ring-border/30", isOver && "ring-primary/40")}>
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
              groupColor={group.color}
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

  const accentColor = group.color || "var(--primary)"

  return (
    <div
      className="sticky left-0 z-20 flex min-h-[44px] items-center justify-between border-b border-border border-l-[6px] bg-table-group pl-3 pr-4"
      style={{ borderLeftColor: accentColor }}
    >
      <div className="flex min-w-0 items-center gap-2">
        <button
          type="button"
          className="rounded p-1 text-muted-foreground transition hover:bg-foreground/8 hover:text-foreground"
          aria-label={group.isCollapsed ? `Expand ${group.title}` : `Collapse ${group.title}`}
          onClick={() => onToggleGroup(group.id)}
        >
          <ChevronDown className={cn("size-4 transition-transform duration-200", group.isCollapsed && "-rotate-90")} />
        </button>
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
        <Badge
          variant="secondary"
          className="rounded-full px-2 py-0 text-[11px] font-medium bg-white/10 text-muted-foreground border-0"
        >
          {group.cards.length}
        </Badge>
      </div>
      <div className="flex items-center gap-1">
        <Button
          variant="ghost"
          size="sm"
          className="h-8 text-muted-foreground hover:text-foreground hover:bg-foreground/8"
          onClick={() => document.getElementById(inputId)?.focus()}
        >
          <Plus className="size-4" />
          Add
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon-sm" className="text-muted-foreground hover:bg-foreground/8" aria-label={`${group.title} group menu`}>
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
            <div className="p-2">
              <p className="mb-1.5 px-1 text-[11px] font-medium uppercase text-muted-foreground">Color</p>
              <div className="grid grid-cols-6 gap-1.5">
                {GROUP_COLORS.map((color) => (
                  <button
                    key={color}
                    type="button"
                    title={color}
                    aria-label={`Group color ${color}`}
                    className={cn(
                      "size-5 rounded-full border-2 transition-transform hover:scale-110",
                      group.color === color ? "border-foreground/70" : "border-transparent"
                    )}
                    style={{ backgroundColor: color }}
                    onClick={() => updateGroup.mutate({ groupId: group.id, color })}
                  />
                ))}
              </div>
              <label className="mt-2 flex cursor-pointer items-center gap-2 px-1 text-xs text-muted-foreground hover:text-foreground">
                Custom
                <input
                  type="color"
                  defaultValue={group.color ?? "#579bfc"}
                  className="size-5 cursor-pointer rounded border-0 bg-transparent p-0"
                  onChange={(e) => updateGroup.mutate({ groupId: group.id, color: e.target.value })}
                  onClick={(e) => e.stopPropagation()}
                />
              </label>
            </div>
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

const GROUP_COLORS = [
  "#579bfc", "#0086c0", "#66ccff", "#00c875",
  "#9cd326", "#cab641", "#fdab3d", "#ff7575",
  "#e2445c", "#ff5ac4", "#a25ddc", "#784bd1",
  "#7e3b8a", "#401694", "#68a1bd", "#808080",
  "#333333", "#ffffff",
]
