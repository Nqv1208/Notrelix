"use client"

import Link from "next/link"
import { AlertCircle, CalendarDays, Clock3, FileText, Gauge, MessageSquareText, SquareKanban, Users } from "lucide-react"
import { BoardKanbanView } from "@/components/boards/workbench"
import { MondayDocEditor } from "@/components/docs/editor"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Progress } from "@/components/ui/progress"
import { Skeleton } from "@/components/ui/skeleton"
import { useFullBoard, useResolvedWorkspaceBoard } from "@/features/boards/hooks"
import type { Card } from "@/features/boards/types"
import type { WorkspaceSnapshot, WorkspaceView } from "@/features/workspace/types"
import { MainTableView } from "./table/main-table-view"

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

  return <WorkspaceKanbanBoardView boardId={resolvedBoard.boardId} workspaceId={workspaceId} />
}

function WorkspaceKanbanBoardView({ boardId, workspaceId }: { boardId: string; workspaceId: string }) {
  const { board, groups, isLoading, error } = useFullBoard(boardId, workspaceId)

  if (isLoading) return <ViewSkeleton rows={4} />
  if (error || !board) return <ViewError title="Board unavailable" />

  return (
    <div className="p-4 sm:p-6">
      <BoardKanbanView board={board} groups={groups} />
    </div>
  )
}

function WorkspaceDocView({ pageId, workspaceId }: { pageId: string; workspaceId: string }) {
  return (
    <div className="h-full min-h-[720px] bg-background">
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

  const cards = groups.flatMap((group) => group.cards.map((card) => ({ ...card, groupTitle: group.title })))
  const days = ["Mon 18", "Tue 19", "Wed 20", "Thu 21", "Fri 22"]

  return (
    <div className="p-4 sm:p-6">
      <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
        <div className="flex items-center justify-between border-b border-border px-4 py-3">
          <div>
            <h2 className="text-sm font-semibold text-foreground">Workspace calendar</h2>
            <p className="text-xs text-muted-foreground">Unified deadlines from board cards and linked docs.</p>
          </div>
          <Badge variant="secondary" className="rounded-full">{cards.length} scheduled</Badge>
        </div>
        <div className="grid min-w-[760px] grid-cols-5">
          {days.map((day, index) => (
            <div key={day} className="min-h-[520px] border-r border-border p-3 last:border-r-0">
              <div className="mb-3 flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
                <CalendarDays className="size-3.5" />
                {day}
              </div>
              <div className="space-y-2">
                {cards.slice(index * 3, index * 3 + 3).map((card) => (
                  <CalendarCard key={card.id} card={card} groupTitle={card.groupTitle} />
                ))}
              </div>
            </div>
          ))}
        </div>
      </section>
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

  const cards = groups.flatMap((group) => group.cards.map((card) => ({ ...card, groupTitle: group.title }))).slice(0, 10)

  return (
    <div className="p-4 sm:p-6">
      <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
        <div className="grid min-w-[900px] grid-cols-[260px_repeat(6,minmax(96px,1fr))] border-b border-border bg-muted px-4 py-3 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
          <div>Item</div>
          {["Week 1", "Week 2", "Week 3", "Week 4", "Week 5", "Week 6"].map((week) => <div key={week}>{week}</div>)}
        </div>
        <div className="min-w-[900px]">
          {cards.map((card, index) => (
            <div key={card.id} className="grid min-h-14 grid-cols-[260px_repeat(6,minmax(96px,1fr))] items-center border-b border-border px-4 last:border-b-0">
              <div className="min-w-0 pr-4">
                <p className="truncate text-sm font-medium text-foreground">{card.title}</p>
                <p className="text-xs text-muted-foreground">{card.groupTitle}</p>
              </div>
              <div className="col-span-6 h-3 rounded-full bg-muted">
                <div
                  className="h-3 rounded-full bg-primary"
                  style={{ width: `${38 + (index % 5) * 11}%`, marginLeft: `${(index % 3) * 8}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}

function WorkspaceDashboardView({ workspaceId, snapshot }: { workspaceId: string; snapshot: WorkspaceSnapshot }) {
  const metrics = [
    { label: "Views", value: snapshot.views.length.toString(), detail: "Workspace tabs", icon: Gauge },
    { label: "Members", value: snapshot.members.length.toString(), detail: "Across roles", icon: Users },
    { label: "Boards", value: "3", detail: "Active work streams", icon: SquareKanban },
    { label: "Docs", value: "32", detail: "8 updated today", icon: FileText },
  ]

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
              <Link href={`/${workspaceId}?view=table` as never}>Open table</Link>
            </Button>
          </div>
          <div className="grid gap-3 md:grid-cols-3">
            {["Product delivery", "Roadmap planning", "Design QA"].map((board, index) => (
              <div key={board} className="rounded-xl border border-border bg-muted/45 p-4">
                <h3 className="mb-3 text-sm font-semibold text-foreground">{board}</h3>
                <Progress value={[68, 43, 81][index]} />
                <p className="mt-3 text-xs text-muted-foreground">{[18, 9, 14][index]} open tasks · {[6, 2, 11][index]} due this week</p>
              </div>
            ))}
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

function CalendarCard({ card, groupTitle }: { card: Card; groupTitle: string }) {
  return (
    <div className="rounded-xl border border-border bg-muted p-3">
      <p className="line-clamp-2 text-sm font-medium text-foreground">{card.title}</p>
      <div className="mt-3 flex items-center justify-between gap-2">
        <Badge variant="secondary" className="rounded-full">{groupTitle}</Badge>
        <span className="text-xs text-muted-foreground">{card.dueDate?.slice(5, 10)}</span>
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
