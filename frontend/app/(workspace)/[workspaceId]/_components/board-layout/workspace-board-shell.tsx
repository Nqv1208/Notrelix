"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { AlertCircle } from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import {
  useActiveWorkspaceView,
  useWorkspaceSnapshot,
  getWorkspaceBoardHref,
  WorkspaceManagementPanel,
  WorkspaceCompactHeader,
} from "@/features/workspace"
import { useResolvedWorkspaceBoard } from "@/features/work-management"
import { WorkspaceContextualToolbar } from "./workspace-contextual-toolbar"
import { WorkspaceViewContent } from "../dashboard/workspace-view-content"
import { WorkspaceViewTabs } from "./workspace-view-tabs"

export function WorkspaceBoardShell({
  workspaceId,
  requestedView,
  panel,
}: {
  workspaceId: string
  requestedView?: string
  panel?: string
}) {
  const router = useRouter()
  const snapshot = useWorkspaceSnapshot(workspaceId)
  const { views, activeView, isLoading, error } = useActiveWorkspaceView(workspaceId, requestedView)
  const defaultBoard = useResolvedWorkspaceBoard({ workspaceId })
  const shouldRedirectToMainTable = !panel

  useEffect(() => {
    if (!shouldRedirectToMainTable || !defaultBoard.boardId) return
    router.replace(getWorkspaceBoardHref(workspaceId, defaultBoard.boardId) as never)
  }, [defaultBoard.boardId, router, shouldRedirectToMainTable, workspaceId])

  if (snapshot.isLoading || isLoading || (shouldRedirectToMainTable && defaultBoard.isLoading)) return <WorkspaceBoardSkeleton />

  if (shouldRedirectToMainTable && defaultBoard.boardId) return <WorkspaceBoardSkeleton />

  if (snapshot.error || error || (shouldRedirectToMainTable && defaultBoard.error) || !snapshot.data || (!activeView && !panel)) {
    return (
      <main className="p-4 sm:p-6">
        <div className="rounded-2xl border border-border bg-card p-8 text-center">
          <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
          <h1 className="text-lg font-semibold text-foreground">Workspace unavailable</h1>
          <p className="mt-2 text-sm text-muted-foreground">The workspace views could not be loaded.</p>
        </div>
      </main>
    )
  }

  return (
    <main className="flex h-full min-h-0 flex-col bg-card">
      <WorkspaceCompactHeader workspace={snapshot.data.workspace} members={snapshot.data.members} />
      {panel ? (
        <section className="min-h-0 flex-1 overflow-auto p-4 sm:p-6">
          <WorkspaceManagementPanel panel={panel} workspaceId={workspaceId} snapshot={snapshot.data} />
        </section>
      ) : activeView ? (
        <>
          <WorkspaceViewTabs workspaceId={workspaceId} views={views} activeViewId={activeView.id} />
          <WorkspaceContextualToolbar activeType={activeView.type} activeView={activeView} />
          <section className="min-h-0 flex-1 overflow-auto">
            <WorkspaceViewContent workspaceId={workspaceId} view={activeView} snapshot={snapshot.data} />
          </section>
        </>
      ) : null}
    </main>
  )
}

function WorkspaceBoardSkeleton() {
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
