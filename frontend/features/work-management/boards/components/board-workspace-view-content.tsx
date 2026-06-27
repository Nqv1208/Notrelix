"use client"

import { AlertCircle, SquareKanban } from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import { useFullBoard } from "@/features/work-management/hooks"
import type { WorkspaceView } from "@/features/workspace"
import { MainTableView } from "@/features/work-management/boards/components/views/table/main-table-view"
import { BoardCalendarView } from "@/features/work-management/boards/components/views/calendar/board-calendar-view"
import { KanbanView } from "@/features/work-management/boards/components/views/kanban/kanban-view"
import { BoardTimelineView } from "@/features/work-management/boards/components/views/timeline/board-timeline-view"

export function BoardWorkspaceViewContent({
  workspaceId,
  boardId,
  view,
}: {
  workspaceId: string
  boardId: string
  view: WorkspaceView
}) {
  if (view.type === "table") return <MainTableView boardId={boardId} workspaceId={workspaceId} />
  if (view.type === "kanban") return <KanbanView boardId={boardId} workspaceId={workspaceId} />
  if (view.type === "calendar") return <BoardFullDataView workspaceId={workspaceId} boardId={boardId} mode="calendar" />
  if (view.type === "timeline") return <BoardFullDataView workspaceId={workspaceId} boardId={boardId} mode="timeline" />
  return <UnsupportedBoardView view={view} />
}

function BoardFullDataView({
  workspaceId,
  boardId,
  mode,
}: {
  workspaceId: string
  boardId: string
  mode: "kanban" | "calendar" | "timeline"
}) {
  const { board, groups, isLoading, error } = useFullBoard(boardId, workspaceId)

  if (isLoading) return <ViewSkeleton rows={mode === "kanban" ? 4 : 6} />
  if (error || !board) return <ViewError title="Board unavailable" />

  return (
    <div className="h-full overflow-auto p-4 sm:p-6">
      {mode === "kanban" ? <KanbanView boardId={board.id} workspaceId={workspaceId} /> : null}
      {mode === "calendar" ? <BoardCalendarView groups={groups} /> : null}
      {mode === "timeline" ? <BoardTimelineView groups={groups} /> : null}
    </div>
  )
}

function ViewSkeleton({ rows }: { rows: number }) {
  return (
    <div className="p-4 sm:p-6">
      <div className="rounded-2xl border border-border bg-card p-4">
        <Skeleton className="mb-4 h-10 rounded-xl" />
        {Array.from({ length: rows }).map((_, index) => (
          <Skeleton key={index} className="mb-2 h-12 rounded-xl last:mb-0" />
        ))}
      </div>
    </div>
  )
}

function ViewError({ title }: { title: string }) {
  return (
    <div className="p-4 sm:p-6">
      <div className="rounded-2xl border border-border bg-card p-8 text-center">
        <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
        <h2 className="text-lg font-semibold text-foreground">{title}</h2>
        <p className="mt-2 text-sm text-muted-foreground">The board may have been moved, archived, or you may not have access.</p>
      </div>
    </div>
  )
}

function UnsupportedBoardView({ view }: { view: WorkspaceView }) {
  return (
    <div className="p-4 sm:p-6">
      <div className="rounded-2xl border border-border bg-card p-8 text-center">
        <SquareKanban className="mx-auto mb-3 size-8 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-foreground">{view.name} is not a board view</h2>
        <p className="mt-2 text-sm text-muted-foreground">Use the workspace tabs to open board views or switch to Docs for documents.</p>
      </div>
    </div>
  )
}
