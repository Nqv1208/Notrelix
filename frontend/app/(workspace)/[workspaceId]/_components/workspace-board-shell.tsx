"use client"

import { AlertCircle } from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import { useActiveWorkspaceView, useWorkspaceSnapshot } from "@/features/workspace/hooks"
import { WorkspaceCompactHeader } from "./workspace-compact-header"
import { WorkspaceContextualToolbar } from "./workspace-contextual-toolbar"
import { WorkspaceViewContent } from "./workspace-view-content"
import { WorkspaceViewTabs } from "./workspace-view-tabs"

export function WorkspaceBoardShell({
  workspaceId,
  requestedView,
}: {
  workspaceId: string
  requestedView?: string
}) {
  const snapshot = useWorkspaceSnapshot(workspaceId)
  const { views, activeView, isLoading, error } = useActiveWorkspaceView(workspaceId, requestedView)

  if (snapshot.isLoading || isLoading) return <WorkspaceBoardSkeleton />

  if (snapshot.error || error || !snapshot.data || !activeView) {
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
    <main className="flex h-full min-h-0 flex-col bg-background">
      <WorkspaceCompactHeader workspace={snapshot.data.workspace} members={snapshot.data.members} />
      <WorkspaceViewTabs workspaceId={workspaceId} views={views} activeViewId={activeView.id} />
      <WorkspaceContextualToolbar activeType={activeView.type} activeView={activeView} />
      <section className="min-h-0 flex-1 overflow-auto">
        <WorkspaceViewContent workspaceId={workspaceId} view={activeView} snapshot={snapshot.data} />
      </section>
    </main>
  )
}

function WorkspaceBoardSkeleton() {
  return (
    <main className="flex h-full min-h-0 flex-col bg-background">
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
