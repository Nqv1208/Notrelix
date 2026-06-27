"use client"

import { createContext, useContext, useMemo, type ReactNode } from "react"
import { usePathname, useSearchParams } from "next/navigation"
import { AlertCircle } from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import {
  useWorkspaceSnapshot,
  useWorkspaceViews,
  getWorkspaceTabbedViews,
  resolveWorkspaceTabbedActiveView,
  resolveWorkspaceTabbedRoute,
  type WorkspaceTabbedRoute,
  type WorkspaceSnapshot,
  type WorkspaceView,
} from "@/features/workspace"
import { cn } from "@/lib/utils"
import { WorkspaceContextualToolbar } from "../board-layout/workspace-contextual-toolbar"
import { WorkspaceViewTabs } from "../board-layout/workspace-view-tabs"
import { WorkspaceCompactHeader } from "./workspace-compact-header"

type WorkspaceTabbedRouteContextValue = WorkspaceTabbedRoute & {
  activeView: WorkspaceView
  snapshot: WorkspaceSnapshot
  views: WorkspaceView[]
}

const WorkspaceTabbedRouteContext = createContext<WorkspaceTabbedRouteContextValue | null>(null)

export function useWorkspaceTabbedRoute() {
  const context = useContext(WorkspaceTabbedRouteContext)
  if (!context) {
    throw new Error("useWorkspaceTabbedRoute must be used inside WorkspaceTabbedRouteFrame")
  }
  return context
}

export function WorkspaceTabbedRouteFrame({
  workspaceId,
  children,
}: {
  workspaceId: string
  children: ReactNode
}) {
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const search = searchParams.toString()
  const route = useMemo(
    () => resolveWorkspaceTabbedRoute(pathname, workspaceId, search),
    [pathname, search, workspaceId]
  )

  if (!route) return <div className="h-full overflow-auto">{children}</div>

  return (
    <WorkspaceTabbedRouteFrameContent workspaceId={workspaceId} route={route}>
      {children}
    </WorkspaceTabbedRouteFrameContent>
  )
}

function WorkspaceTabbedRouteFrameContent({
  workspaceId,
  route,
  children,
}: {
  workspaceId: string
  route: WorkspaceTabbedRoute
  children: ReactNode
}) {
  const snapshot = useWorkspaceSnapshot(workspaceId)
  const viewsQuery = useWorkspaceViews(workspaceId)
  const activeView = useMemo(
    () => resolveWorkspaceTabbedActiveView(viewsQuery.data ?? [], route),
    [route, viewsQuery.data]
  )
  const views = useMemo(
    () => getWorkspaceTabbedViews(viewsQuery.data ?? [], activeView),
    [activeView, viewsQuery.data]
  )

  if (snapshot.isLoading || viewsQuery.isLoading) return <WorkspaceTabbedSkeleton />

  if (snapshot.error || viewsQuery.error || !snapshot.data) {
    return <WorkspaceTabbedError />
  }

  const currentBoardId = route.kind === "board" ? route.boardId : undefined
  const contextValue = {
    ...route,
    activeView,
    snapshot: snapshot.data,
    views,
  }

  return (
    <WorkspaceTabbedRouteContext.Provider value={contextValue}>
      <WorkspaceTabbedShell
        workspaceId={workspaceId}
        snapshot={snapshot.data}
        views={views}
        activeView={activeView}
        currentBoardId={currentBoardId}
        contentClassName={route.contentClassName}
        showToolbar={route.showToolbar}
      >
        {children}
      </WorkspaceTabbedShell>
    </WorkspaceTabbedRouteContext.Provider>
  )
}

export function WorkspaceTabbedShell({
  workspaceId,
  snapshot,
  views,
  activeView,
  currentBoardId,
  children,
  contentClassName,
  showToolbar = true,
}: {
  workspaceId: string
  snapshot: WorkspaceSnapshot
  views: WorkspaceView[]
  activeView: WorkspaceView
  currentBoardId?: string
  children: ReactNode
  contentClassName?: string
  showToolbar?: boolean
}) {
  return (
    <main className="flex h-full min-h-0 flex-col bg-card">
      <WorkspaceCompactHeader workspace={snapshot.workspace} members={snapshot.members} />
      <WorkspaceViewTabs
        workspaceId={workspaceId}
        views={views}
        activeViewId={activeView.id}
        currentBoardId={currentBoardId}
      />
      {showToolbar ? <WorkspaceContextualToolbar activeType={activeView.type} activeView={activeView} /> : null}
      <section className={cn("min-h-0 flex-1 overflow-auto", contentClassName)}>
        {children}
      </section>
    </main>
  )
}

export function WorkspaceTabbedSkeleton() {
  return (
    <main className="flex h-full min-h-0 flex-col bg-card">
      <div className="border-b border-border bg-card p-6">
        <Skeleton className="mb-3 h-10 w-72 rounded-xl" />
        <Skeleton className="h-5 w-full max-w-2xl rounded-lg" />
      </div>
      <div className="border-b border-border bg-card px-6 py-3">
        <Skeleton className="h-9 w-full max-w-2xl rounded-xl" />
      </div>
      <div className="border-b border-border px-6 py-3">
        <Skeleton className="h-9 w-full max-w-3xl rounded-xl" />
      </div>
      <div className="p-6">
        <div className="rounded-2xl border border-border bg-card p-4">
          <Skeleton className="mb-4 h-10 rounded-xl" />
          <Skeleton className="mb-2 h-12 rounded-xl" />
          <Skeleton className="mb-2 h-12 rounded-xl" />
          <Skeleton className="h-12 rounded-xl" />
        </div>
      </div>
    </main>
  )
}

export function WorkspaceTabbedError({
  title = "Workspace unavailable",
  detail = "The workspace views could not be loaded.",
}: {
  title?: string
  detail?: string
}) {
  return (
    <main className="p-4 sm:p-6">
      <div className="rounded-2xl border border-border bg-card p-8 text-center">
        <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
        <h1 className="text-lg font-semibold text-foreground">{title}</h1>
        <p className="mt-2 text-sm text-muted-foreground">{detail}</p>
      </div>
    </main>
  )
}
