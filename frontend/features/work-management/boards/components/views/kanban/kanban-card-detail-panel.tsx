"use client"

import { useEffect, useRef, useState } from "react"
import { AnimatePresence, motion } from "framer-motion"
import { Activity, AlertCircle, Files, FileText, ListChecks, MessageSquareText, Plus, Trash2, X } from "lucide-react"
import { format } from "date-fns"
import { useCardDetail, useCardChecklists } from "@/features/work-management/hooks"
import type { Board, CardDetail, Checklist } from "@/features/work-management/types"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Progress } from "@/components/ui/progress"
import { TaskDetailHeader } from "@/features/work-management/items/components/card-detail/task-detail-header"
import { TaskUpdatesTab } from "@/features/work-management/items/components/card-detail/task-updates-tab"
import { TaskFilesTab } from "@/features/work-management/items/components/card-detail/task-files-tab"
import { TaskActivityTab } from "@/features/work-management/items/components/card-detail/task-activity-tab"
import { TaskDetailEmptyState } from "@/features/work-management/items/components/card-detail/task-detail-empty-state"

export function KanbanCardDetailPanel({
  board,
  cardId,
  open,
  onClose,
  onExitComplete,
}: {
  board: Board
  cardId: string | null
  open: boolean
  onClose: () => void
  onExitComplete?: () => void
}) {
  const content = cardId ? (
    <KanbanCardDetailContent
      board={board}
      cardId={cardId}
      onClose={onClose}
    />
  ) : null

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent 
        className="max-w-4xl w-[92vw] h-[85vh] p-0 gap-0 overflow-hidden flex flex-col rounded-2xl border border-border/80 shadow-2xl bg-popover duration-200"
        showCloseButton={false}
      >
        <DialogTitle className="sr-only">Card details</DialogTitle>
        {content}
      </DialogContent>
    </Dialog>
  )
}

function KanbanCardDetailContent({
  board,
  cardId,
  onClose,
}: {
  board: Board
  cardId: string
  onClose: () => void
}) {
  const { card, isLoading, error } = useCardDetail(cardId, board.id, board.workspaceId)

  if (isLoading) return <CardDetailSkeleton />
  if (error || !card) return <CardDetailError onClose={onClose} />

  return (
    <div className="flex h-full min-h-0 w-full min-w-0 flex-col overflow-hidden bg-popover font-body">
      <TaskDetailHeader key={card.id} board={board} card={card as CardDetail} onClose={onClose} />
      <KanbanCardTabs board={board} card={card as CardDetail} />
    </div>
  )
}

function KanbanCardTabs({ board, card }: { board: Board; card: CardDetail }) {
  const checklists = card.checklists || []

  return (
    <Tabs defaultValue="updates" className="flex min-h-0 flex-1 flex-col gap-0">
      <div className="sticky top-0 z-10 border-b border-border bg-popover px-4 py-2">
        <TabsList className="max-w-full overflow-x-auto flex justify-start gap-1">
          <TabsTrigger value="updates">
            <MessageSquareText className="size-4" />
            Updates
          </TabsTrigger>
          <TabsTrigger value="checklist">
            <ListChecks className="size-4" />
            Checklist
          </TabsTrigger>
          <TabsTrigger value="files">
            <Files className="size-4" />
            Files
          </TabsTrigger>
          <TabsTrigger value="activity">
            <Activity className="size-4" />
            Activity
          </TabsTrigger>
          <TabsTrigger value="linked-docs">
            <FileText className="size-4" />
            Docs
          </TabsTrigger>
        </TabsList>
      </div>

      <TabsContent value="updates" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <TaskUpdatesTab card={card} />
        </ScrollArea>
      </TabsContent>

      <TabsContent value="checklist" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <CardChecklistTab card={card} board={board} />
        </ScrollArea>
      </TabsContent>

      <TabsContent value="files" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <TaskFilesTab card={card} />
        </ScrollArea>
      </TabsContent>

      <TabsContent value="activity" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <TaskActivityTab card={card} />
        </ScrollArea>
      </TabsContent>

      <TabsContent value="linked-docs" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <div className="p-4">
            <TaskDetailEmptyState
              icon={FileText}
              title={card.linkedPageId ? "Linked doc connected" : "No linked docs"}
              description={card.linkedPageId ? card.linkedPageId : "Link a workspace doc from the table to keep specs and task execution together."}
            />
          </div>
        </ScrollArea>
      </TabsContent>
    </Tabs>
  )
}

function CardChecklistTab({ card, board }: { card: CardDetail; board: Board }) {
  const {
    createChecklist,
    updateChecklist,
    deleteChecklist,
    createChecklistItem,
    updateChecklistItem,
    deleteChecklistItem,
  } = useCardChecklists(card.id, board.id, board.workspaceId)

  const [newChecklistTitle, setNewChecklistTitle] = useState("")

  const handleAddChecklist = (e: React.FormEvent) => {
    e.preventDefault()
    if (!newChecklistTitle.trim()) return
    createChecklist(newChecklistTitle.trim())
    setNewChecklistTitle("")
  }

  return (
    <div className="p-4 space-y-6">
      {/* List of Checklists */}
      {card.checklists && card.checklists.length > 0 ? (
        card.checklists.map((checklist) => (
          <ChecklistSection
            key={checklist.id}
            checklist={checklist}
            onRename={(title) => updateChecklist({ checklistId: checklist.id, title })}
            onDelete={() => deleteChecklist(checklist.id)}
            onAddItem={(title) => createChecklistItem({ checklistId: checklist.id, title })}
            onToggleItem={(itemId, isChecked) => updateChecklistItem({ itemId, isChecked })}
            onRenameItem={(itemId, title) => updateChecklistItem({ itemId, title })}
            onDeleteItem={(itemId) => deleteChecklistItem(itemId)}
          />
        ))
      ) : (
        <div className="py-8">
          <TaskDetailEmptyState
            icon={ListChecks}
            title="No checklists yet"
            description="Add a checklist to break down this card into smaller action items."
          />
        </div>
      )}

      {/* Add Checklist Form */}
      <form onSubmit={handleAddChecklist} className="flex gap-2 border-t border-border pt-4">
        <Input
          value={newChecklistTitle}
          onChange={(e) => setNewChecklistTitle(e.target.value)}
          placeholder="New checklist title..."
          className="h-9 text-sm"
        />
        <Button type="submit" size="sm" className="bg-brand-violet hover:bg-brand-violet/90 text-white shrink-0">
          <Plus className="size-4 mr-1" />
          Add checklist
        </Button>
      </form>
    </div>
  )
}

function ChecklistSection({
  checklist,
  onRename,
  onDelete,
  onAddItem,
  onToggleItem,
  onRenameItem,
  onDeleteItem,
}: {
  checklist: Checklist
  onRename: (title: string) => void
  onDelete: () => void
  onAddItem: (title: string) => void
  onToggleItem: (itemId: string, isChecked: boolean) => void
  onRenameItem: (itemId: string, title: string) => void
  onDeleteItem: (itemId: string) => void
}) {
  const [isEditingTitle, setIsEditingTitle] = useState(false)
  const [titleInput, setTitleInput] = useState(checklist.title)
  const [newItemTitle, setNewItemTitle] = useState("")

  const total = checklist.items?.length || 0
  const checked = checklist.items?.filter((i) => i.isDone)?.length || 0
  const progress = total === 0 ? 0 : Math.round((checked / total) * 100)

  const handleTitleSubmit = () => {
    const next = titleInput.trim()
    if (next && next !== checklist.title) {
      onRename(next)
    }
    setIsEditingTitle(false)
  }

  const handleAddItemSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!newItemTitle.trim()) return
    onAddItem(newItemTitle.trim())
    setNewItemTitle("")
  }

  return (
    <div className="space-y-3 rounded-xl border border-border bg-card p-4 shadow-xs">
      {/* Checklist Header */}
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2 flex-1 min-w-0">
          <ListChecks className="size-4.5 text-muted-foreground/80 shrink-0" />
          {isEditingTitle ? (
            <Input
              value={titleInput}
              onChange={(e) => setTitleInput(e.target.value)}
              onBlur={handleTitleSubmit}
              onKeyDown={(e) => e.key === "Enter" && handleTitleSubmit()}
              className="h-7 text-sm font-semibold px-1 py-0.5 focus-visible:ring-1 focus-visible:ring-offset-0"
              autoFocus
            />
          ) : (
            <h4
              onDoubleClick={() => setIsEditingTitle(true)}
              className="text-sm font-semibold text-foreground truncate cursor-pointer hover:text-primary transition-colors font-display"
            >
              {checklist.title}
            </h4>
          )}
        </div>
        <Button variant="ghost" size="icon-xs" onClick={onDelete} className="text-muted-foreground hover:text-destructive shrink-0">
          <Trash2 className="size-4" />
        </Button>
      </div>

      {/* Progress Bar */}
      <div className="space-y-1.5">
        <div className="flex justify-between text-xs text-muted-foreground">
          <span>Progress</span>
          <span className="font-semibold">{progress}%</span>
        </div>
        <Progress value={progress} className="h-1.5" />
      </div>

      {/* Checklist Items list */}
      <div className="space-y-1.5 pt-1">
        {checklist.items?.map((item) => (
          <ChecklistItemRow
            key={item.id}
            item={item}
            onToggle={(checked) => onToggleItem(item.id, checked)}
            onRename={(title) => onRenameItem(item.id, title)}
            onDelete={() => onDeleteItem(item.id)}
          />
        ))}
      </div>

      {/* Add Checklist Item input */}
      <form onSubmit={handleAddItemSubmit} className="flex gap-2 pt-2">
        <Input
          value={newItemTitle}
          onChange={(e) => setNewItemTitle(e.target.value)}
          placeholder="Add an item..."
          className="h-8 text-xs bg-muted/20"
        />
        <Button type="submit" size="xs" disabled={!newItemTitle.trim()} className="h-8 px-3 shrink-0">
          Add
        </Button>
      </form>
    </div>
  )
}

function ChecklistItemRow({
  item,
  onToggle,
  onRename,
  onDelete,
}: {
  item: { id: string; title: string; isDone: boolean }
  onToggle: (checked: boolean) => void
  onRename: (title: string) => void
  onDelete: () => void
}) {
  const [isEditing, setIsEditing] = useState(false)
  const [titleInput, setTitleInput] = useState(item.title)

  const handleRenameSubmit = () => {
    const next = titleInput.trim()
    if (next && next !== item.title) {
      onRename(next)
    }
    setIsEditing(false)
  }

  return (
    <div className="flex items-center justify-between gap-2 py-1 px-1.5 rounded-lg hover:bg-muted/30 group/item transition-colors">
      <div className="flex items-center gap-2.5 flex-1 min-w-0">
        <Checkbox
          checked={item.isDone}
          onCheckedChange={(checked) => onToggle(Boolean(checked))}
        />
        {isEditing ? (
          <Input
            value={titleInput}
            onChange={(e) => setTitleInput(e.target.value)}
            onBlur={handleRenameSubmit}
            onKeyDown={(e) => e.key === "Enter" && handleRenameSubmit()}
            className="h-6 text-xs px-1 py-0 focus-visible:ring-1 focus-visible:ring-offset-0 flex-1"
            autoFocus
          />
        ) : (
          <span
            onDoubleClick={() => setIsEditing(true)}
            className={cn(
              "text-xs leading-5 text-foreground truncate cursor-pointer select-none",
              item.isDone && "line-through text-muted-foreground/75"
            )}
          >
            {item.title}
          </span>
        )}
      </div>
      <Button
        variant="ghost"
        size="icon-xs"
        onClick={onDelete}
        className="opacity-0 group-hover/item:opacity-100 transition-opacity hover:text-destructive shrink-0 size-6"
      >
        <X className="size-3.5" />
      </Button>
    </div>
  )
}

function CardDetailSkeleton() {
  return (
    <div className="flex h-full w-full flex-col gap-4 bg-popover p-4">
      <Skeleton className="h-10 rounded-lg" />
      <Skeleton className="h-20 rounded-lg" />
      <Skeleton className="h-9 rounded-lg" />
      <Skeleton className="h-40 rounded-lg" />
    </div>
  )
}

function CardDetailError({ onClose }: { onClose: () => void }) {
  return (
    <div className="flex h-full w-full flex-col items-center justify-center bg-popover p-6 text-center">
      <AlertCircle className="mb-3 size-8 text-destructive" />
      <h2 className="text-sm font-semibold text-foreground font-display">Card unavailable</h2>
      <p className="mt-2 max-w-xs text-sm text-muted-foreground">This card could not be loaded or no longer exists.</p>
      <Button type="button" variant="outline" size="sm" className="mt-4 bg-card" onClick={onClose}>
        Close panel
      </Button>
    </div>
  )
}
