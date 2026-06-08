"use client"

import { useState, useEffect } from "react"
import { ChevronDown, MoreHorizontal, Plus, Check, Copy, Eye, EyeOff, Trash2 } from "lucide-react"
import { useDroppable } from "@dnd-kit/core"
import { SortableContext, verticalListSortingStrategy } from "@dnd-kit/sortable"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useDeleteGroup, useDuplicateGroup, useUpdateGroup } from "@/features/boards/hooks"
import type { Board, BoardGroup, BoardTableColumn } from "@/features/boards/types"
import { cn } from "@/lib/utils"
import { TableAddTaskRow } from "./table-add-task-row"
import { TableRow } from "./table-row"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

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
  const [settingsDialogOpen, setSettingsDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [customColor, setCustomColor] = useState(group.color || "")
  const updateGroup = useUpdateGroup(board.id, board.workspaceId)
  const deleteGroup = useDeleteGroup(board.id, board.workspaceId)
  const duplicateGroup = useDuplicateGroup(board.id, board.workspaceId)

  useEffect(() => {
    setTitle(group.title)
  }, [group.title])

  useEffect(() => {
    setCustomColor(group.color || "")
  }, [group.color])

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
    <>
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
        <Button
          variant="ghost"
          size="icon-sm"
          className="text-muted-foreground hover:bg-foreground/8"
          aria-label={`${group.title} group settings`}
          onClick={() => setSettingsDialogOpen(true)}
        >
          <MoreHorizontal className="size-4" />
        </Button>
      </div>
    </div>

    <Dialog open={settingsDialogOpen} onOpenChange={setSettingsDialogOpen}>
      <DialogContent className="sm:max-w-[420px] p-5 gap-4">
        <DialogHeader className="pb-2 border-b border-border/40">
          <DialogTitle className="text-base font-bold flex items-center gap-2">
            <span className="size-3 rounded-full" style={{ backgroundColor: group.color || "var(--primary)" }} />
            Cấu hình nhóm: {group.title}
          </DialogTitle>
          <DialogDescription className="text-xs">
            Tùy chỉnh tiêu đề, màu sắc hiển thị và quản lý nhóm công việc.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-1">
          {/* Naming Section */}
          <div className="space-y-1.5">
            <label className="text-[11px] font-bold text-muted-foreground uppercase tracking-wider">Tên nhóm</label>
            <Input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              onBlur={() => {
                const next = title.trim()
                if (next && next !== group.title) {
                  updateGroup.mutate({ groupId: group.id, title: next })
                }
              }}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  const next = title.trim()
                  if (next && next !== group.title) {
                    updateGroup.mutate({ groupId: group.id, title: next })
                    e.currentTarget.blur()
                  }
                }
              }}
              placeholder="Nhập tên nhóm..."
              className="h-9 bg-background border-border text-sm focus-visible:ring-1"
            />
          </div>

          {/* Color Swatches Grid */}
          <div className="space-y-1.5">
            <label className="text-[11px] font-bold text-muted-foreground uppercase tracking-wider">Màu sắc mặc định</label>
            <div className="grid grid-cols-6 gap-2 border border-border/50 bg-muted/20 rounded-lg p-2.5">
              {GROUP_COLORS.map((color) => {
                const isActive = group.color === color
                return (
                  <button
                    key={color}
                    type="button"
                    title={color}
                    aria-label={`Group color ${color}`}
                    className={cn(
                      "size-8 rounded-full border border-black/10 transition-all duration-200 hover:scale-110 flex items-center justify-center relative shadow-xs cursor-pointer",
                      isActive ? "ring-2 ring-primary ring-offset-2 ring-offset-background scale-105" : "hover:border-muted-foreground/40"
                    )}
                    style={{ backgroundColor: color }}
                    onClick={() => updateGroup.mutate({ groupId: group.id, color })}
                  >
                    {isActive && (
                      <Check className={cn("size-4", color.toLowerCase() === "#ffffff" ? "text-neutral-900" : "text-white")} />
                    )}
                  </button>
                )
              })}
            </div>
          </div>

          {/* Custom Color Picker */}
          <div className="space-y-1.5">
            <label className="text-[11px] font-bold text-muted-foreground uppercase tracking-wider">Màu tự chọn</label>
            <div className="flex items-center gap-3 rounded-lg border border-border bg-muted/20 p-2">
              <div className="relative size-9 overflow-hidden rounded-md border border-border shadow-xs flex items-center justify-center bg-muted hover:bg-muted/50 transition-colors">
                <input
                  type="color"
                  value={group.color || "#579bfc"}
                  className="absolute inset-0 size-full cursor-pointer opacity-0"
                  onChange={(e) => {
                    updateGroup.mutate({ groupId: group.id, color: e.target.value })
                    setCustomColor(e.target.value)
                  }}
                />
                <div 
                  className="size-5 rounded-sm shadow-inner border border-black/10" 
                  style={{ backgroundColor: group.color || "#579bfc" }} 
                />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-xs font-semibold text-foreground">Chọn mã màu</p>
                <p className="text-[10px] text-muted-foreground truncate">{group.color || "#579bfc"}</p>
              </div>
              <Input
                type="text"
                value={customColor}
                placeholder="#579bfc"
                className="h-8 w-24 text-xs font-mono uppercase bg-background border-border text-center"
                onChange={(e) => {
                  let val = e.target.value
                  if (val && !val.startsWith("#")) {
                    val = "#" + val
                  }
                  setCustomColor(val)
                  if (/^#[0-9A-F]{6}$/i.test(val)) {
                    updateGroup.mutate({ groupId: group.id, color: val })
                  }
                }}
                onBlur={() => {
                  if (!/^#[0-9A-F]{6}$/i.test(customColor)) {
                    setCustomColor(group.color || "")
                  }
                }}
              />
            </div>
          </div>

          {/* Quick Actions */}
          <div className="space-y-1.5">
            <label className="text-[11px] font-bold text-muted-foreground uppercase tracking-wider">Thao tác nhanh</label>
            <div className="grid grid-cols-2 gap-2">
              <Button
                variant="outline"
                size="sm"
                className="flex items-center justify-start gap-2 h-9 text-xs border-border bg-background hover:bg-muted/40"
                onClick={() => {
                  duplicateGroup.mutate(group.id)
                  setSettingsDialogOpen(false)
                }}
              >
                <Copy className="size-3.5 text-muted-foreground" />
                Nhân bản nhóm
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="flex items-center justify-start gap-2 h-9 text-xs border-border bg-background hover:bg-muted/40"
                onClick={() => {
                  onToggleGroup(group.id)
                }}
              >
                {group.isCollapsed ? (
                  <>
                    <Eye className="size-3.5 text-muted-foreground" />
                    Mở rộng nhóm
                  </>
                ) : (
                  <>
                    <EyeOff className="size-3.5 text-muted-foreground" />
                    Thu gọn nhóm
                  </>
                )}
              </Button>
            </div>
          </div>

          {/* Danger Zone */}
          <div className="border-t border-border/60 pt-3 flex justify-between items-center">
            <div className="flex flex-col gap-0.5">
              <p className="text-[11px] font-bold text-destructive uppercase tracking-wider">Vùng nguy hiểm</p>
              <p className="text-[10px] text-muted-foreground">Xóa nhóm này cùng toàn bộ các thẻ bên trong.</p>
            </div>
            <Button
              variant="destructive"
              size="sm"
              className="h-8 px-3 text-xs flex items-center gap-1.5"
              onClick={() => {
                setSettingsDialogOpen(false)
                setDeleteDialogOpen(true)
              }}
            >
              <Trash2 className="size-3.5" />
              Xóa nhóm
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>

    <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Xóa nhóm công việc</AlertDialogTitle>
          <AlertDialogDescription>
            Bạn có chắc chắn muốn xóa nhóm &ldquo;{group.title}&rdquo;? Tất cả các nhiệm vụ trong nhóm này cũng sẽ bị xóa vễn viễn khỏi bảng. Hành động này không thể hoàn tác.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Hủy</AlertDialogCancel>
          <AlertDialogAction
            variant="destructive"
            onClick={() => {
              deleteGroup.mutate(group.id)
              setDeleteDialogOpen(false)
            }}
          >
            Xóa nhóm
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
    </>
  )
}

const GROUP_COLORS = [
  "#579bfc", "#0086c0", "#66ccff", "#00c875",
  "#9cd326", "#cab641", "#fdab3d", "#ff7575",
  "#e2445c", "#ff5ac4", "#a25ddc", "#784bd1",
  "#7e3b8a", "#401694", "#68a1bd", "#808080",
  "#333333", "#ffffff",
]
