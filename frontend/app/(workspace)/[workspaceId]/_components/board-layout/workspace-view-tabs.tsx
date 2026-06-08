"use client"

import { useState, useEffect, useRef } from "react"
import Link from "next/link"
import { MoreHorizontal } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { WorkspaceView } from "@/features/workspace/types"
import { getViewHref } from "@/features/workspace/utils"
import { cn } from "@/lib/utils"
import { WorkspaceAddViewMenu } from "../view-management/workspace-add-view-menu"
import { useReorderWorkspaceViews } from "@/features/workspace/hooks"

// Dnd-kit imports
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragEndEvent,
} from "@dnd-kit/core"
import { restrictToHorizontalAxis } from "@dnd-kit/modifiers"
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  horizontalListSortingStrategy,
  useSortable,
} from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"

export function WorkspaceViewTabs({
  workspaceId,
  views,
  activeViewId,
  currentBoardId,
}: {
  workspaceId: string
  views: WorkspaceView[]
  activeViewId?: string
  currentBoardId?: string
}) {
  const [items, setItems] = useState<WorkspaceView[]>(views)
  const isDraggingRef = useRef(false)
  const cleanupClickRef = useRef<(() => void) | null>(null)
  const reorderMutation = useReorderWorkspaceViews(workspaceId)

  useEffect(() => {
    setItems(views)
  }, [views])

  useEffect(() => {
    return () => {
      if (cleanupClickRef.current) {
        cleanupClickRef.current()
      }
    }
  }, [])

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8, // Tránh click nhầm khi drag
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  )

  const handleDragStart = () => {
    isDraggingRef.current = true

    if (cleanupClickRef.current) {
      cleanupClickRef.current()
      cleanupClickRef.current = null
    }

    const preventClick = (e: MouseEvent) => {
      e.stopImmediatePropagation()
      e.preventDefault()
    }

    window.addEventListener("click", preventClick, true)
    cleanupClickRef.current = () => {
      window.removeEventListener("click", preventClick, true)
    }
  }

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event

    if (over && active.id !== over.id) {
      setItems((prevItems) => {
        const oldIndex = prevItems.findIndex((item) => item.id === active.id)
        const newIndex = prevItems.findIndex((item) => item.id === over.id)
        const updated = arrayMove(prevItems, oldIndex, newIndex)
        
        // Gọi API cập nhật position của các views
        reorderMutation.mutate(updated.map(v => v.id))
        
        return updated
      })
    }

    isDraggingRef.current = false

    setTimeout(() => {
      if (cleanupClickRef.current) {
        cleanupClickRef.current()
        cleanupClickRef.current = null
      }
    }, 100)
  }

  return (
    <div className="border-b border-border bg-card">
      <div className="flex min-w-0 items-center gap-2 px-4 sm:px-6">
        <div className="min-w-0 flex-1 overflow-x-auto whitespace-nowrap scrollbar-none">
          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragStart={handleDragStart}
            onDragEnd={handleDragEnd}
            modifiers={[restrictToHorizontalAxis]}
          >
            <SortableContext
              items={items.map((item) => item.id)}
              strategy={horizontalListSortingStrategy}
            >
              <div role="tablist" aria-label="Workspace views" className="flex h-12 items-center gap-1.5 py-1">
                {items.map((view) => (
                  <SortableTabItem
                    key={view.id}
                    view={view}
                    workspaceId={workspaceId}
                    active={view.id === activeViewId}
                    currentBoardId={currentBoardId}
                    isDraggingParentRef={isDraggingRef}
                  />
                ))}
              </div>
            </SortableContext>
          </DndContext>
        </div>
        <WorkspaceAddViewMenu workspaceId={workspaceId} />
        <Button variant="ghost" size="icon-sm" aria-label="More view actions">
          <MoreHorizontal className="size-4" />
        </Button>
      </div>
    </div>
  )
}

function SortableTabItem({
  view,
  workspaceId,
  active,
  currentBoardId,
  isDraggingParentRef,
}: {
  view: WorkspaceView
  workspaceId: string
  active: boolean
  currentBoardId?: string
  isDraggingParentRef: React.RefObject<boolean | null>
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: view.id })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.6 : 1,
    zIndex: isDragging ? 50 : 'auto',
  }

  const handleClick = (e: React.MouseEvent) => {
    if (isDraggingParentRef.current) {
      e.preventDefault()
      e.stopPropagation()
    }
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className={cn(
        "inline-flex cursor-grab active:cursor-grabbing touch-none select-none rounded-lg",
        isDragging && "shadow-md bg-accent/40"
      )}
    >
      <Link
        href={getViewHref(workspaceId, view, { currentBoardId }) as never}
        role="tab"
        aria-selected={active}
        onClick={handleClick}
        className={cn(
          "relative inline-flex h-9 items-center gap-1.5 rounded-lg px-3 text-sm font-medium text-muted-foreground transition hover:bg-muted/80 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
          active && "text-foreground bg-muted/40 font-semibold"
        )}
      >
        {view.name}
        {active ? <span className="absolute inset-x-2 -bottom-1 h-0.5 rounded-full bg-primary" /> : null}
      </Link>
    </div>
  )
}
