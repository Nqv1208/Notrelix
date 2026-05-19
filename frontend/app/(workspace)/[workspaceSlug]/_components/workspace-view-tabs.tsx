"use client"

import Link from "next/link"
import { MoreHorizontal } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { WorkspaceView } from "@/features/workspace/types"
import { getViewHref } from "@/features/workspace/utils"
import { cn } from "@/lib/utils"
import { WorkspaceAddViewMenu } from "./workspace-add-view-menu"

export function WorkspaceViewTabs({
  workspaceSlug,
  views,
  activeViewId,
}: {
  workspaceSlug: string
  views: WorkspaceView[]
  activeViewId?: string
}) {
  return (
    <div className="border-b border-border bg-card">
      <div className="flex min-w-0 items-center gap-2 px-4 sm:px-6">
        <div className="min-w-0 flex-1 overflow-x-auto whitespace-nowrap">
          <div role="tablist" aria-label="Workspace views" className="flex h-12 items-center gap-1">
            {views.map((view) => {
              const active = view.id === activeViewId
              return (
                <Link
                  key={view.id}
                  href={getViewHref(workspaceSlug, view) as never}
                  role="tab"
                  aria-selected={active}
                  className={cn(
                    "relative inline-flex h-10 items-center gap-2 rounded-lg px-3 text-sm font-medium text-muted-foreground transition hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                    active && "text-foreground"
                  )}
                >
                  <span className="text-xs">{view.icon}</span>
                  {view.name}
                  {active ? <span className="absolute inset-x-2 -bottom-1 h-0.5 rounded-full bg-primary" /> : null}
                </Link>
              )
            })}
          </div>
        </div>
        <WorkspaceAddViewMenu workspaceSlug={workspaceSlug} />
        <Button variant="ghost" size="icon-sm" aria-label="More view actions">
          <MoreHorizontal className="size-4" />
        </Button>
      </div>
    </div>
  )
}
