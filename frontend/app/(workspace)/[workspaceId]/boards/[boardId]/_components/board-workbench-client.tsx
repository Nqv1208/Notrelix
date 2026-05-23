"use client"

import { AlertCircle } from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from "@/components/ui/resizable"
import { BoardKanbanView, BoardTableView } from "@/components/boards/workbench"
import { useBoardDocsPanel, useFullBoard } from "@/features/boards/hooks"
import { useWorkspace } from "@/features/workspace/hooks/use-workspace"
import { BoardToolbar } from "./board-toolbar"
import { BoardViewTabs } from "./board-view-tabs"
import { DocsPanel } from "./docs-panel"
import type { ViewMode } from "@/features/boards/types"

export function BoardWorkbenchClient({
  workspaceId,
  boardId,
  activeView = "table",
}: {
  workspaceId: string
  boardId: string
  activeView?: ViewMode
}) {
  const workspaceQuery = useWorkspace(workspaceId)
  const { board, groups, fieldDefinitions, isLoading: isBoardLoading, error: boardError } = useFullBoard(boardId, workspaceQuery.data?.id)
  const { activeDocId } = useBoardDocsPanel()

  if (workspaceQuery.isLoading || isBoardLoading) return <BoardWorkbenchSkeleton />

  if (workspaceQuery.error || boardError || !workspaceQuery.data || !board) {
    return (
      <main className="mx-auto max-w-[1380px] px-4 py-5 sm:px-6 lg:px-8">
        <div className="rounded-2xl border border-border bg-card p-8 text-center">
          <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
          <h1 className="text-lg font-semibold text-foreground">Board unavailable</h1>
          <p className="mt-2 text-sm text-muted-foreground">The board may have been moved, archived, or you may not have access.</p>
        </div>
      </main>
    )
  }

  return (
    <main className="mx-auto max-w-[1480px] px-4 py-5 sm:px-6 lg:px-8">
      <BoardToolbar board={board} />
      <BoardViewTabs workspaceId={workspaceId} board={board} activeView={activeView} />
      <ResizablePanelGroup direction="horizontal" className="gap-4">
        <ResizablePanel className="min-w-0">
          {activeView === "kanban" ? (
            <BoardKanbanView board={board} groups={groups} />
          ) : (
            <BoardTableView board={board} groups={groups} fieldDefinitions={fieldDefinitions} />
          )}
        </ResizablePanel>
        {activeDocId ? (
          <>
            <ResizableHandle withHandle className="hidden lg:flex" />
            <ResizablePanel className="hidden max-w-[420px] basis-[360px] lg:block">
              <DocsPanel workspaceId={workspaceId} pageId={activeDocId} />
            </ResizablePanel>
            <div className="fixed inset-x-3 bottom-3 top-20 z-50 lg:hidden">
              <DocsPanel workspaceId={workspaceId} pageId={activeDocId} />
            </div>
          </>
        ) : null}
      </ResizablePanelGroup>
    </main>
  )
}

function BoardWorkbenchSkeleton() {
  return (
    <main className="mx-auto max-w-[1480px] space-y-5 px-4 py-5 sm:px-6 lg:px-8">
      <Skeleton className="h-36 rounded-2xl" />
      <Skeleton className="h-14 rounded-2xl" />
      <div className="rounded-2xl border border-border bg-card p-4">
        <Skeleton className="mb-4 h-10 rounded-xl" />
        <Skeleton className="mb-2 h-12 rounded-xl" />
        <Skeleton className="mb-2 h-12 rounded-xl" />
        <Skeleton className="h-12 rounded-xl" />
      </div>
    </main>
  )
}
