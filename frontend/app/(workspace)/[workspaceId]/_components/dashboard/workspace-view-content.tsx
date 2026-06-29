"use client"

import {
  useResolvedWorkspaceBoard,
  BoardWorkspaceViewContent
} from "@/features/work-management"
import { DocumentScreen } from "@/features/docs"
import type { WorkspaceSnapshot, WorkspaceView } from "@/features/workspace"
import { LoadingState, EmptyState, ErrorState, NotFoundState } from "@/components/feedback"

import { WorkspaceOverview } from "./workspace-overview"
import { ActiveBoards } from "./active-boards"
import { PinnedDocs } from "./pinned-docs"
import { ActivityFeed } from "./activity-feed"
import { UpcomingDeadlines } from "./upcoming-deadlines"

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

  if (view.type === "kanban") return <WorkspaceBoardView view={view} workspaceId={workspaceId} />
  if (view.type === "doc") return <WorkspaceDocView pageId={view.target.pageId ?? "docs-mvp-spec"} workspaceId={routeWorkspaceId} />
  if (view.type === "calendar") return <WorkspaceCalendarView view={view} workspaceId={workspaceId} />
  if (view.type === "timeline") return <WorkspaceTimelineView view={view} workspaceId={workspaceId} />
  if (view.type === "dashboard") return <WorkspaceDashboardView workspaceId={routeWorkspaceId} snapshot={snapshot} />
  if (view.type === "table") return <WorkspaceBoardView view={view} workspaceId={workspaceId} />
  return <UnsupportedView view={view} />
}

function WorkspaceBoardView({
  view,
  workspaceId,
}: {
  view: WorkspaceView
  workspaceId: string
}) {
  const resolvedBoard = useResolvedWorkspaceBoard({ workspaceId, requestedBoardId: view.target.boardId })

  if (resolvedBoard.isLoading) return <LoadingState className="m-4 sm:m-6" />
  if (resolvedBoard.error) return <ErrorState error={resolvedBoard.error} className="m-4 sm:m-6" />
  if (resolvedBoard.isEmpty || !resolvedBoard.boardId) {
    return (
      <EmptyState
        title="No boards yet"
        description="Create a board in this workspace to populate the table, kanban, calendar, and timeline views."
        className="m-4 sm:m-6"
      />
    )
  }

  return <BoardWorkspaceViewContent workspaceId={workspaceId} boardId={resolvedBoard.boardId} view={view} />
}

function WorkspaceDocView({ pageId, workspaceId }: { pageId: string; workspaceId: string }) {
  return (
    <div className="h-full min-h-[720px] bg-card">
      <DocumentScreen pageId={pageId} workspaceId={workspaceId} embedded showToolbar={false} showOpenFullDoc />
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

  if (resolvedBoard.isLoading) return <LoadingState className="m-4 sm:m-6" />
  if (resolvedBoard.error) return <ErrorState error={resolvedBoard.error} className="m-4 sm:m-6" />
  if (resolvedBoard.isEmpty || !resolvedBoard.boardId) {
    return (
      <EmptyState
        title="No boards yet"
        description="Create a board in this workspace to populate the calendar view."
        className="m-4 sm:m-6"
      />
    )
  }

  return <BoardWorkspaceViewContent workspaceId={workspaceId} boardId={resolvedBoard.boardId} view={view} />
}

function WorkspaceTimelineView({
  view,
  workspaceId,
}: {
  view: WorkspaceView
  workspaceId: string
}) {
  const resolvedBoard = useResolvedWorkspaceBoard({ workspaceId, requestedBoardId: view.target.boardId })

  if (resolvedBoard.isLoading) return <LoadingState className="m-4 sm:m-6" />
  if (resolvedBoard.error) return <ErrorState error={resolvedBoard.error} className="m-4 sm:m-6" />
  if (resolvedBoard.isEmpty || !resolvedBoard.boardId) {
    return (
      <EmptyState
        title="No boards yet"
        description="Create a board in this workspace to populate the timeline view."
        className="m-4 sm:m-6"
      />
    )
  }

  return <BoardWorkspaceViewContent workspaceId={workspaceId} boardId={resolvedBoard.boardId} view={view} />
}

function WorkspaceDashboardView({ workspaceId, snapshot }: { workspaceId: string; snapshot: WorkspaceSnapshot }) {
  return (
    <div className="space-y-6 p-4 sm:p-6 max-w-[1600px] mx-auto">
      {/* 1. Overview and Greeting */}
      <WorkspaceOverview workspaceId={workspaceId} snapshot={snapshot} />

      {/* 2. Main Content Grid */}
      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
        {/* Left Column: Active Boards & Deadlines */}
        <div className="space-y-6">
          <ActiveBoards workspaceId={workspaceId} />
          <UpcomingDeadlines workspaceId={workspaceId} />
        </div>

        {/* Right Column (Sidebar): Pinned Documents & Recent Activity */}
        <div className="space-y-6">
          <PinnedDocs workspaceId={workspaceId} />
          <ActivityFeed workspaceId={workspaceId} snapshot={snapshot} />
        </div>
      </div>
    </div>
  )
}

function UnsupportedView({ view }: { view: WorkspaceView }) {
  return (
    <NotFoundState
      title={`${view.name} is ready for setup`}
      description="This view type is part of the workspace view model and will connect to its API-backed renderer in a later phase."
      className="m-4 sm:m-6"
    />
  )
}
