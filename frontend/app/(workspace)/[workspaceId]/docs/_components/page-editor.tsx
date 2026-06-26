"use client"

import { FileText } from "lucide-react"

interface PageEditorProps {
  pageId: string
  workspaceId?: string
}

export function PageEditor({ pageId }: PageEditorProps) {
  return (
    <div className="rounded-xl border border-dashed p-6 text-sm text-muted-foreground">
      <FileText className="mb-2 size-4" />
      Legacy embedded editor placeholder for page {pageId}. Use `app/(workspace)/[workspaceId]/docs/[pageId]` for the full Docs MVP.
    </div>
  )
}
