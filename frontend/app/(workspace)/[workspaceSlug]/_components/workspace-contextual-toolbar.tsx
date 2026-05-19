"use client"

import {
  ArrowDownUp,
  Bot,
  CalendarDays,
  ChevronDown,
  EyeOff,
  Filter,
  Group,
  ListPlus,
  MoreHorizontal,
  Search,
  Settings2,
  UserRound,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import { DocEditorToolbar } from "@/components/docs/editor"
import { usePageBlocks } from "@/features/docs/hooks"
import type { WorkspaceView, WorkspaceViewType } from "@/features/workspace/types"

export function WorkspaceContextualToolbar({ activeType, activeView }: { activeType: WorkspaceViewType; activeView?: WorkspaceView }) {
  if (activeType === "doc") return <DocToolbar pageId={activeView?.target.pageId ?? "docs-mvp-spec"} />
  if (activeType === "kanban") return <KanbanToolbar />
  if (activeType === "calendar") return <CalendarToolbar />
  if (activeType === "timeline") return <TimelineToolbar />
  if (activeType === "dashboard") return <DashboardToolbar />
  return <TableToolbar />
}

function SearchBox({ placeholder = "Search" }: { placeholder?: string }) {
  return (
    <div className="relative hidden min-w-[180px] sm:block">
      <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
      <Input className="h-9 rounded-full bg-card pl-8" placeholder={placeholder} />
    </div>
  )
}

function TableToolbar() {
  return (
    <ToolbarShell>
      <Button size="sm" className="rounded-full">
        <ListPlus className="size-4" />
        New task
      </Button>
      <SearchBox placeholder="Search tasks" />
      <ToolbarButton icon={UserRound} label="Person" />
      <ToolbarButton icon={Filter} label="Filter" />
      <ToolbarButton icon={ArrowDownUp} label="Sort" />
      <ToolbarButton icon={EyeOff} label="Hide" />
      <ToolbarButton icon={Group} label="Group by" />
      <ToolbarButton icon={MoreHorizontal} label="More" compact />
    </ToolbarShell>
  )
}

function KanbanToolbar() {
  return (
    <ToolbarShell>
      <SearchBox placeholder="Search cards" />
      <ToolbarButton icon={UserRound} label="Person" />
      <ToolbarButton icon={Filter} label="Filter" />
      <ToolbarButton icon={ArrowDownUp} label="Sort" />
      <ToolbarButton icon={Group} label="Group by status" />
      <ToolbarButton icon={Settings2} label="Board settings" />
    </ToolbarShell>
  )
}

function DocToolbar({ pageId }: { pageId: string }) {
  const blocks = usePageBlocks(pageId)
  return <DocEditorToolbar pageId={pageId} blocks={blocks.data ?? []} compact />
}

function CalendarToolbar() {
  return (
    <ToolbarShell>
      <Button size="sm" className="rounded-full">
        <CalendarDays className="size-4" />
        Today
      </Button>
      <ToggleGroup type="single" defaultValue="month" className="hidden sm:flex">
        <ToggleGroupItem value="month" size="sm">Month</ToggleGroupItem>
        <ToggleGroupItem value="week" size="sm">Week</ToggleGroupItem>
        <ToggleGroupItem value="day" size="sm">Day</ToggleGroupItem>
      </ToggleGroup>
      <ToolbarButton icon={Filter} label="Filter" />
      <ToolbarButton icon={ArrowDownUp} label="Sync" />
      <ToolbarButton icon={Settings2} label="Settings" />
    </ToolbarShell>
  )
}

function TimelineToolbar() {
  return (
    <ToolbarShell>
      <Button size="sm" className="rounded-full">
        <CalendarDays className="size-4" />
        Today
      </Button>
      <ToolbarButton icon={UserRound} label="Person" />
      <ToolbarButton icon={Filter} label="Filter" />
      <ToolbarButton icon={Group} label="Group by list" />
      <ToolbarButton icon={Settings2} label="Timeline settings" />
    </ToolbarShell>
  )
}

function DashboardToolbar() {
  return (
    <ToolbarShell>
      <Button size="sm" className="rounded-full">
        <ListPlus className="size-4" />
        Add widget
      </Button>
      <ToolbarButton icon={Filter} label="Filter" />
      <ToolbarButton icon={ArrowDownUp} label="Refresh" />
      <ToolbarButton icon={Bot} label="AI summary" />
    </ToolbarShell>
  )
}

function ToolbarShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-14 flex-wrap items-center gap-2 border-b border-border bg-background px-4 py-2 sm:px-6">
      {children}
    </div>
  )
}

function ToolbarButton({
  icon: Icon,
  label,
  compact,
}: {
  icon: typeof Search
  label: string
  compact?: boolean
}) {
  return (
    <Button variant="ghost" size="sm" className="rounded-full">
      <Icon className="size-4" />
      {!compact ? <span className="hidden sm:inline">{label}</span> : null}
      {!compact ? <ChevronDown className="hidden size-3.5 text-muted-foreground sm:block" /> : null}
      {compact ? <span className="sr-only">{label}</span> : null}
    </Button>
  )
}
