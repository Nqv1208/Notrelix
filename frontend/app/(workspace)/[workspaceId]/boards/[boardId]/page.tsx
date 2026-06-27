"use client"

import { BoardScreen } from "@/features/work-management"
import { useWorkspaceTabbedRoute } from "../../_components/shell/workspace-tabbed-shell"

export default function BoardPage() {
  const route = useWorkspaceTabbedRoute()

  if (route.kind !== "board") {
    return (
      <div className="p-4 text-center text-muted-foreground">
        Board route unavailable
      </div>
    )
  }

  return (
    <BoardScreen
      workspaceId={route.workspaceId}
      boardId={route.boardId}
      view={route.activeView}
    />
  )
}
