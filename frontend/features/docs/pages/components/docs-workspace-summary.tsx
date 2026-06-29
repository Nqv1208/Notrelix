"use client"

import { FileText } from "lucide-react"
import { usePageList } from "../../tree/hooks/queries/use-page-tree"

interface DocsWorkspaceSummaryProps {
  workspaceId: string
}

export function DocsWorkspaceSummary({ workspaceId }: DocsWorkspaceSummaryProps) {
  const { data: pages = [] } = usePageList(workspaceId)
  const docsCount = pages.length

  return (
    <div className="rounded-2xl border border-border bg-card p-4 shadow-sm">
      <div className="mb-3 flex items-center gap-2 text-xs font-medium text-muted-foreground">
        <FileText className="size-4 text-primary" />
        Docs
      </div>
      <p className="text-2xl font-semibold tracking-[-0.015em] text-foreground">{docsCount}</p>
      <p className="mt-1 text-xs text-muted-foreground">Workspace pages</p>
    </div>
  )
}
