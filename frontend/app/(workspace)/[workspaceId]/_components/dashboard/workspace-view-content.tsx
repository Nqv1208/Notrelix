"use client"

import Link from "next/link"
import { useMemo } from "react"
import { AlertCircle, Clock3, FileText, Gauge, MessageSquareText, SquareKanban, Users } from "lucide-react"
import { BoardCalendarView } from "@/app/(workspace)/[workspaceId]/boards/[boardId]/_components/views/board-calendar-view"
import { KanbanView } from "../kanban/kanban-view"
import { BoardTimelineView } from "@/app/(workspace)/[workspaceId]/boards/[boardId]/_components/views/board-timeline-view"
import { MondayDocEditor } from "@/app/(workspace)/[workspaceId]/docs/[pageId]/_components/editor/monday-doc-editor"
import { Button } from "@/components/ui/button"
import { Progress } from "@/components/ui/progress"
import { Skeleton } from "@/components/ui/skeleton"
import { useFullBoard, useResolvedWorkspaceBoard, useWorkspaceBoards } from "@/features/boards/hooks"
import type { Board } from "@/features/boards/types"
import { usePageList } from "@/features/docs/hooks/use-page-tree"
import type { WorkspaceSnapshot, WorkspaceView } from "@/features/workspace/types"
import { getWorkspaceBoardHref, getWorkspaceBoardsHref } from "@/features/workspace/utils/workspace-routes"
import { MainTableView } from "@/app/(workspace)/[workspaceId]/boards/[boardId]/_components/table/main-table-view"

export function WorkspaceViewContent({
  workspaceId: routeWorkspaceId,
  view,
  snapshot,
}: {
  workspaceId: string
  view: WorkspaceView
  snapshot: WorkspaceSnapshot
}) {
  const workspaceId = snapshot.workspace.id

  if (view.type === "kanban") return <WorkspaceBoardView mode="kanban" view={view} workspaceId={workspaceId} />
  if (view.type === "doc") return <WorkspaceDocView pageId={view.target.pageId ?? "docs-mvp-spec"} workspaceId={routeWorkspaceId} />
  if (view.type === "calendar") return <WorkspaceCalendarView view={view} workspaceId={workspaceId} />
  if (view.type === "timeline") return <WorkspaceTimelineView view={view} workspaceId={workspaceId} />
  if (view.type === "dashboard") return <WorkspaceDashboardView workspaceId={routeWorkspaceId} snapshot={snapshot} />
  if (view.type === "table") return <WorkspaceBoardView mode="table" view={view} workspaceId={workspaceId} />
  return <UnsupportedView view={view} />
}

function WorkspaceBoardView({
  mode,
  view,
  workspaceId,
}: {
  mode: "table" | "kanban"
  view: WorkspaceView
  workspaceId: string
}) {
  const resolvedBoard = useResolvedWorkspaceBoard({ workspaceId, requestedBoardId: view.target.boardId })

  if (resolvedBoard.isLoading) return <ViewSkeleton rows={mode === "table" ? 8 : 4} />
  if (resolvedBoard.error) return <ViewError title="Boards unavailable" />
  if (resolvedBoard.isEmpty || !resolvedBoard.boardId) return <ViewEmptyBoard />
  if (mode === "table") return <MainTableView boardId={resolvedBoard.boardId} workspaceId={workspaceId} />

  return <KanbanView boardId={resolvedBoard.boardId} workspaceId={workspaceId} />
}

function WorkspaceDocView({ pageId, workspaceId }: { pageId: string; workspaceId: string }) {
  return (
    <div className="h-full min-h-[720px] bg-card">
      <MondayDocEditor pageId={pageId} workspaceId={workspaceId} embedded showToolbar={false} showOpenFullDoc />
    </div>
  )
}

function WorkspaceCalendarView({
  view,
  workspaceId,
}: {
  view: WorkspaceView
  workspaceId: string
}) {
  const resolvedBoard = useResolvedWorkspaceBoard({ workspaceId, requestedBoardId: view.target.boardId })
  const { groups, isLoading: isBoardLoading, error: boardError } = useFullBoard(resolvedBoard.boardId, workspaceId)

  if (resolvedBoard.isLoading || isBoardLoading) return <ViewSkeleton rows={6} />
  if (resolvedBoard.error || boardError) return <ViewError title="Calendar unavailable" />
  if (resolvedBoard.isEmpty || !resolvedBoard.boardId) return <ViewEmptyBoard />

  return (
    <div className="p-4 sm:p-6">
      <BoardCalendarView groups={groups} />
    </div>
  )
}

function WorkspaceTimelineView({
  view,
  workspaceId,
}: {
  view: WorkspaceView
  workspaceId: string
}) {
  const resolvedBoard = useResolvedWorkspaceBoard({ workspaceId, requestedBoardId: view.target.boardId })
  const { groups, isLoading: isBoardLoading, error: boardError } = useFullBoard(resolvedBoard.boardId, workspaceId)

  if (resolvedBoard.isLoading || isBoardLoading) return <ViewSkeleton rows={7} />
  if (resolvedBoard.error || boardError) return <ViewError title="Timeline unavailable" />
  if (resolvedBoard.isEmpty || !resolvedBoard.boardId) return <ViewEmptyBoard />

  return (
    <div className="p-4 sm:p-6">
      <BoardTimelineView groups={groups} />
    </div>
  )
}

function WorkspaceDashboardView({ workspaceId, snapshot }: { workspaceId: string; snapshot: WorkspaceSnapshot }) {
  const boardsQuery = useWorkspaceBoards(workspaceId)
  const pagesQuery = usePageList(workspaceId)

  const boardsCount = boardsQuery.data?.length ?? 0
  const docsCount = pagesQuery.data?.length ?? 0
  const primaryBoardId = boardsQuery.data?.[0]?.id
  const openTableHref = primaryBoardId ? getWorkspaceBoardHref(workspaceId, primaryBoardId) : getWorkspaceBoardsHref(workspaceId)

  const metrics = useMemo(() => [
    { label: "Views", value: snapshot.views.length.toString(), detail: "Workspace tabs", icon: Gauge },
    { label: "Members", value: snapshot.members.length.toString(), detail: "Across roles", icon: Users },
    { label: "Boards", value: boardsCount.toString(), detail: "Active boards", icon: SquareKanban },
    { label: "Docs", value: docsCount.toString(), detail: "Workspace pages", icon: FileText },
  ], [snapshot.views.length, snapshot.members.length, boardsCount, docsCount])

  return (
    <div className="space-y-5 p-4 sm:p-6">
      <div className="grid gap-3 md:grid-cols-4">
        {metrics.map((metric) => (
          <div key={metric.label} className="rounded-2xl border border-border bg-card p-4 shadow-sm">
            <div className="mb-3 flex items-center gap-2 text-xs font-medium text-muted-foreground">
              <metric.icon className="size-4 text-primary" />
              {metric.label}
            </div>
            <p className="text-2xl font-semibold tracking-[-0.015em] text-foreground">{metric.value}</p>
            <p className="mt-1 text-xs text-muted-foreground">{metric.detail}</p>
          </div>
        ))}
      </div>

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
        <section className="rounded-2xl border border-border bg-card p-5">
          <div className="mb-4 flex items-center justify-between">
            <div>
              <h2 className="text-sm font-semibold text-foreground">Workspace pulse</h2>
              <p className="text-xs text-muted-foreground">Current activity across views and project resources.</p>
            </div>
            <Button asChild variant="outline" size="sm" className="bg-card">
              <Link href={openTableHref as never}>Open table</Link>
            </Button>
          </div>
          <div className="grid gap-3 md:grid-cols-3">
            {(boardsQuery.data ?? []).slice(0, 3).map((board: Board, index: number) => (
              <div key={board.id} className="rounded-xl border border-border bg-muted/45 p-4">
                <h3 className="mb-3 text-sm font-semibold text-foreground">{board.title}</h3>
                <Progress value={[68, 43, 81][index % 3]} />
                <p className="mt-3 text-xs text-muted-foreground">Active work stream</p>
              </div>
            ))}
            {boardsCount === 0 && (
              <div className="col-span-3 text-center py-6 text-sm text-muted-foreground">
                No active boards found.
              </div>
            )}
          </div>
        </section>

        <aside className="space-y-5">
          <section className="rounded-2xl border border-border bg-card p-5">
            <div className="mb-4 flex items-center gap-2">
              <MessageSquareText className="size-4 text-primary" />
              <h2 className="text-sm font-semibold text-foreground">Recent team activity</h2>
            </div>
            <div className="space-y-3">
              {snapshot.activity.map((item) => (
                <div key={item.id} className="flex gap-3 rounded-xl border border-border bg-muted p-3">
                  <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-card text-primary">
                    <Clock3 className="size-4" />
                  </div>
                  <div className="min-w-0">
                    <p className="text-sm text-foreground"><span className="font-medium">{item.actor}</span> {item.action} {item.target}</p>
                    <p className="mt-1 text-xs text-muted-foreground">{item.createdAt}</p>
                  </div>
                </div>
              ))}
            </div>
          </section>
        </aside>
      </div>
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
        <p className="mt-2 text-sm text-muted-foreground">The view may have been moved, archived, or is waiting for API integration.</p>
      </div>
    </div>
  )
}

function ViewEmptyBoard() {
  return (
    <div className="p-4 sm:p-6">
      <div className="rounded-2xl border border-border bg-card p-8 text-center">
        <SquareKanban className="mx-auto mb-3 size-8 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-foreground">No boards yet</h2>
        <p className="mt-2 text-sm text-muted-foreground">Create a board in this workspace to populate the table, kanban, calendar, and timeline views.</p>
      </div>
    </div>
  )
}

function UnsupportedView({ view }: { view: WorkspaceView }) {
  return (
    <div className="p-4 sm:p-6">
      <div className="rounded-2xl border border-border bg-card p-8 text-center">
        <div className="mx-auto mb-3 flex size-12 items-center justify-center rounded-2xl bg-muted text-lg">{view.icon}</div>
        <h2 className="text-lg font-semibold text-foreground">{view.name} is ready for setup</h2>
        <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-muted-foreground">
          This view type is part of the workspace view model and will connect to its API-backed renderer in a later phase.
        </p>
      </div>
    </div>
  )
}
