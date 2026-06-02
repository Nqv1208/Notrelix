"use client"

import Link from "next/link"
import { useMemo, useCallback } from "react"
import { AlertCircle, CalendarDays, Clock3, FileText, Gauge, MessageSquareText, SquareKanban, Users } from "lucide-react"
import { BoardKanbanView } from "@/components/boards/workbench"
import { MondayDocEditor } from "@/components/docs/editor"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Progress } from "@/components/ui/progress"
import { Skeleton } from "@/components/ui/skeleton"
import { useFullBoard, useResolvedWorkspaceBoard, useWorkspaceBoards } from "@/features/boards/hooks"
import { usePageList } from "@/features/docs/hooks/use-page-tree"
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

  // Get current week days (Monday to Friday)
  const currentWeekDays = useMemo(() => {
    const today = new Date()
    const dayOfWeek = today.getDay() // 0 = Sun, 1 = Mon, ...
    const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek
    const monday = new Date(today)
    monday.setDate(today.getDate() + mondayOffset)

    return Array.from({ length: 5 }).map((_, i) => {
      const date = new Date(monday)
      date.setDate(monday.getDate() + i)
      return date
    })
  }, [])

  const cards = useMemo(() => {
    return groups.flatMap((group) => group.cards.map((card) => ({ ...card, groupTitle: group.title })))
  }, [groups])

  // Group cards by day (Mon-Fri)
  const cardsByDay = useMemo(() => {
    const result: Record<number, typeof cards> = { 0: [], 1: [], 2: [], 3: [], 4: [] }
    let fallbackIndex = 0

    cards.forEach((card) => {
      let placed = false
      if (card.dueDate) {
        try {
          const cardDate = new Date(card.dueDate)
          currentWeekDays.forEach((day, index) => {
            if (cardDate.toDateString() === day.toDateString()) {
              result[index].push(card)
              placed = true
            }
          })
        } catch {}
      }
      // Fallback: if card has no date or falls outside this week, distribute it evenly
      if (!placed) {
        result[fallbackIndex % 5].push(card)
        fallbackIndex++
      }
    })
    return result
  }, [cards, currentWeekDays])

  if (resolvedBoard.isLoading || isBoardLoading) return <ViewSkeleton rows={6} />
  if (resolvedBoard.error || boardError) return <ViewError title="Calendar unavailable" />
  if (resolvedBoard.isEmpty || !resolvedBoard.boardId) return <ViewEmptyBoard />

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
          {currentWeekDays.map((day, index) => {
            const dayStr = day.toLocaleDateString("en-US", { weekday: "short", day: "numeric" })
            return (
              <div key={day.toISOString()} className="min-h-[520px] border-r border-border p-3 last:border-r-0">
                <div className="mb-3 flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
                  <CalendarDays className="size-3.5" />
                  {dayStr}
                </div>
                <div className="space-y-2">
                  {cardsByDay[index].map((card) => (
                    <CalendarCard key={card.id} card={card} groupTitle={card.groupTitle} />
                  ))}
                </div>
              </div>
            )
          })}
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

  // 6 week range starting from Monday of current week
  const { timelineStart, timelineEnd, weeks } = useMemo(() => {
    const today = new Date()
    const dayOfWeek = today.getDay()
    const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek
    const timelineStart = new Date(today)
    timelineStart.setDate(today.getDate() + mondayOffset)
    timelineStart.setHours(0, 0, 0, 0)

    const timelineEnd = new Date(timelineStart)
    timelineEnd.setDate(timelineStart.getDate() + 42) // 6 weeks = 42 days

    const weeks = Array.from({ length: 6 }).map((_, i) => `Week ${i + 1}`)
    return { timelineStart, timelineEnd, weeks }
  }, [])

  const cards = useMemo(() => {
    return groups.flatMap((group) => group.cards.map((card) => ({ ...card, groupTitle: group.title }))).slice(0, 10)
  }, [groups])

  const getTimelineBarStyles = useCallback((card: any, index: number) => {
    let start = new Date(card.startDate || new Date())
    let end = new Date(card.dueDate || new Date())

    if (isNaN(start.getTime()) || isNaN(end.getTime()) || start > end) {
      const width = 20 + (index % 4) * 15
      const margin = (index % 3) * 10
      return { width: `${width}%`, marginLeft: `${margin}%` }
    }

    const totalDuration = timelineEnd.getTime() - timelineStart.getTime()
    const cardStartOffset = start.getTime() - timelineStart.getTime()
    const cardDuration = end.getTime() - start.getTime()

    const marginLeftPct = Math.max(0, Math.min(100, (cardStartOffset / totalDuration) * 100))
    const widthPct = Math.max(5, Math.min(100 - marginLeftPct, (cardDuration / totalDuration) * 100))

    return {
      width: `${widthPct}%`,
      marginLeft: `${marginLeftPct}%`,
    }
  }, [timelineStart, timelineEnd])

  if (resolvedBoard.isLoading || isBoardLoading) return <ViewSkeleton rows={7} />
  if (resolvedBoard.error || boardError) return <ViewError title="Timeline unavailable" />
  if (resolvedBoard.isEmpty || !resolvedBoard.boardId) return <ViewEmptyBoard />

  return (
    <div className="p-4 sm:p-6">
      <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
        <div className="grid min-w-[900px] grid-cols-[260px_repeat(6,minmax(96px,1fr))] border-b border-border bg-muted px-4 py-3 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
          <div>Item</div>
          {weeks.map((week) => <div key={week}>{week}</div>)}
        </div>
        <div className="min-w-[900px]">
          {cards.map((card, index) => {
            const barStyle = getTimelineBarStyles(card, index)
            return (
              <div key={card.id} className="grid min-h-14 grid-cols-[260px_repeat(6,minmax(96px,1fr))] items-center border-b border-border px-4 last:border-b-0">
                <div className="min-w-0 pr-4">
                  <p className="truncate text-sm font-medium text-foreground">{card.title}</p>
                  <p className="text-xs text-muted-foreground">{card.groupTitle}</p>
                </div>
                <div className="col-span-6 h-3 rounded-full bg-muted">
                  <div
                    className="h-3 rounded-full bg-primary"
                    style={barStyle}
                  />
                </div>
              </div>
            )
          })}
        </div>
      </section>
    </div>
  )
}

function WorkspaceDashboardView({ workspaceId, snapshot }: { workspaceId: string; snapshot: WorkspaceSnapshot }) {
  const boardsQuery = useWorkspaceBoards(workspaceId)
  const pagesQuery = usePageList(workspaceId)

  const boardsCount = boardsQuery.data?.length ?? 0
  const docsCount = pagesQuery.data?.length ?? 0

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
              <Link href={`/${workspaceId}?view=table` as never}>Open table</Link>
            </Button>
          </div>
          <div className="grid gap-3 md:grid-cols-3">
            {(boardsQuery.data ?? []).slice(0, 3).map((board: any, index: number) => (
              <div key={board.id} className="rounded-xl border border-border bg-muted/45 p-4">
                <h3 className="mb-3 text-sm font-semibold text-foreground">{board.name}</h3>
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
