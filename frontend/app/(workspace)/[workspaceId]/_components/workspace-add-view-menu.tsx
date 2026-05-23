"use client"

import { useRouter } from "next/navigation"
import { Plus } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { workspaceViewTemplates } from "@/features/workspace/mock/mock-data"
import { useCreateWorkspaceView } from "@/features/workspace/hooks"
import type { WorkspaceViewType } from "@/features/workspace/types"

const defaultTargets: Partial<Record<WorkspaceViewType, { boardId?: string; pageId?: string; calendarId?: string; dashboardId?: string }>> = {
  table: { boardId: "board-product" },
  kanban: { boardId: "board-product" },
  timeline: { boardId: "board-product" },
  doc: { pageId: "docs-mvp-spec" },
  calendar: { calendarId: "workspace-calendar" },
  dashboard: { dashboardId: "workspace-health" },
}

export function WorkspaceAddViewMenu({ workspaceId }: { workspaceId: string }) {
  const router = useRouter()
  const createView = useCreateWorkspaceView()

  async function handleCreate(type: WorkspaceViewType, label: string, disabled?: boolean) {
    if (disabled || createView.isPending) return
    const view = await createView.mutateAsync({
      workspaceId,
      name: label,
      type,
      target: defaultTargets[type],
    })
    router.push(`/${workspaceId}?view=${view.id}`)
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="h-9 rounded-full px-2.5">
          <Plus className="size-4" />
          <span className="sr-only sm:not-sr-only">Add view</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-[330px]">
        <DropdownMenuLabel>Add workspace view</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {workspaceViewTemplates.map((template) => (
          <DropdownMenuItem
            key={template.type}
            disabled={Boolean(template.badge)}
            onClick={() => handleCreate(template.type, template.label, Boolean(template.badge))}
            className="items-start gap-3 py-3"
          >
            <span className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted text-sm text-foreground">
              {template.icon}
            </span>
            <span className="min-w-0 flex-1">
              <span className="flex items-center gap-2 text-sm font-medium text-foreground">
                {template.label}
                {template.badge ? <Badge variant="secondary" className="rounded-full">{template.badge}</Badge> : null}
              </span>
              <span className="mt-0.5 block text-xs leading-5 text-muted-foreground">{template.description}</span>
            </span>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
