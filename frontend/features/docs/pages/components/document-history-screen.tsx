"use client"

import { usePage } from "../hooks/queries/use-page"
import { BreadcrumbNav } from "./breadcrumb-nav"
import { HistoryClient } from "./history-client"

interface DocumentHistoryScreenProps {
  pageId: string
  workspaceId: string
}

export function DocumentHistoryScreen({ pageId, workspaceId }: DocumentHistoryScreenProps) {
  const page = usePage(pageId)
  const detail = page.data

  return (
    <div className="min-h-svh bg-card">
      <div className="mx-auto max-w-[1180px] px-4 py-8 sm:px-6 lg:px-8">
        {detail && <BreadcrumbNav breadcrumb={detail.breadcrumb} workspaceId={workspaceId} />}
        <div className="mb-6 rounded-2xl border border-border bg-card p-5">
          <h1 className="text-2xl font-semibold tracking-[-0.015em] text-foreground">Version history</h1>
          <p className="mt-2 text-sm text-muted-foreground">Review page activity and restore points.</p>
        </div>
        <HistoryClient pageId={pageId} />
      </div>
    </div>
  )
}
